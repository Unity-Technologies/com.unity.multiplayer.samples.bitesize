/// <summary>
/// Handles aggregation of raw data into heatmap data.
/// </summary>
/// 
/// There are three related fields in Processing that need to be understood:
/// smoothOn, aggregateOn, and groupOn.
/// 
/// smoothOn provides a list of parameters that will get smoothed, and by how much.
/// If, for example, "x" is in the list, and the "x" value is 1, then every x between
/// -.5 and .5 will be "smoothed to 0.
/// smoothOn is always a subset of aggregateOn.
/// 
/// aggregateOn provides a list of parameters to meld into a single point if all the
/// values are the same after smoothing. For example, if aggregateOn contains the list
/// "x", "y", "z" and "t", then every time we encounter a point where (after smoothing)
/// x = 25, y = 15, z = 5, and t = 22, we will consider that to be the exact same point,
/// and aggregation can occur (so the density increases).
/// 
/// groupOn allows us to create lists out of the results. A list will ALWAYS be created
/// out of unique event names, but if groupOn contains "userID" and "platform", then
/// unique lists will also be created from these fields.
/// groupOn is always a subset of aggregateOn.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityAnalytics;

namespace UnityAnalyticsHeatmap
{
    public class HeatmapAggregator
    {
        string[] pointProperties = new string[]{ "x", "y", "z", "rx", "ry", "rz", "dx", "dy", "dz", "t" };
        string[] headerKeys = new string[]{
            "name",
            "submit_time",
            "custom_params",
            "userid",
            "sessionid",
            "platform",
            "debug_device"
        };

        int m_ReportFiles = 0;
        int m_ReportRows = 0;
        int m_ReportLegalPoints = 0;

        Dictionary<Tuplish, HistogramHeatPoint> m_PointDict;
        string m_DataPath = "";

        public delegate void CompletionHandler(string jsonPath);

        CompletionHandler m_CompletionHandler;


        public HeatmapAggregator(string dataPath)
        {
            SetDataPath(dataPath);
        }

        /// <summary>
        /// Sets the data path.
        /// </summary>
        /// <param name="dataPath">The location on the host machine from which to retrieve data.</param>
        public void SetDataPath(string dataPath)
        {
            m_DataPath = dataPath;
        }

        /// <summary>
        /// Process the specified inputFiles, using the other specified parameters.
        /// </summary>
        /// <param name="inputFiles">A list of one or more raw data text files.</param>
        /// <param name="startDate">Any timestamp prior to this ISO 8601 date will be trimmed.</param>
        /// <param name="endDate">Any timestamp after to this ISO 8601 date will be trimmed.</param>
        /// <param name="aggregateOn">A list of properties on which to specify point uniqueness.</param>
        /// <param name="smoothOn">A dictionary of properties that are smoothable, along with their smoothing values. <b>Must be a subset of aggregateOn.</b></param>
        /// <param name="groupOn">A list of properties on which to group resulting lists (supports arbitrary data, plus 'eventName', 'userID', 'sessionID', 'platform', and 'debug').</param>
        /// <param name="remapDensityToField">If not blank, remaps density (aka color) to the value of the field.</param>
        /// <param name="aggregationMethod">Determines the calculation with which multiple points aggregate (default is Increment).</param>
        /// <param name="percentile">For use with the AggregationMethod.Percentile, a value between 0-1 indicating the percentile to use.</param>
        public void Process(CompletionHandler completionHandler,
            List<string> inputFiles, DateTime startDate, DateTime endDate,
            List<string> aggregateOn,
            Dictionary<string, float> smoothOn,
            List<string> groupOn,
            string remapDensityToField,
            AggregationMethod aggregationMethod = AggregationMethod.Increment,
            float percentile = 0)
        {
            m_CompletionHandler = completionHandler;

            string outputFileName = System.IO.Path.GetFileName(inputFiles[0]).Replace(".gz", ".json");

            // Histograms stores all the data
            // Tuplish is the key, holding the combo that makes the point unique(often, x/y/z/t)
            // List maintains the individual lists of points
            // The HistogramHeatPoint stores all the data in the list, compacted right before saving
            var histograms = new Dictionary<Tuplish, List<HistogramHeatPoint>>();

            m_ReportFiles = 0;
            m_ReportLegalPoints = 0;
            m_ReportRows = 0;
            m_PointDict = new Dictionary<Tuplish, HistogramHeatPoint>();

            var headers = GetHeaders();
            if (headers["name"] == -1 || headers["submit_time"] == -1 || headers["custom_params"] == -1)
            {
                Debug.LogWarning ("No headers found. The likeliest cause of this is that you have no custom_headers.gz file. Perhaps you need to download a raw data job?");
            }
            else
            {
                foreach (string file in inputFiles)
                {
                    m_ReportFiles++;
                    LoadStream(histograms, headers, file, startDate, endDate,
                        aggregateOn, smoothOn, groupOn,
                        remapDensityToField);
                }

                // Test if any data was generated
                bool hasData = false;
                List<int> reportList = new List<int>();

                Dictionary<Tuplish, List<Dictionary<string, float>>> outputData = new Dictionary<Tuplish, List<Dictionary<string, float>>>();

                foreach (var generated in histograms)
                {
                    // Convert to output. Perform accretion calcs
                    var list = generated.Value;
                    var outputList = new List<Dictionary<string, float>>();
                    foreach(var pt in list)
                    {
                        var outputPt = new Dictionary<string, float>();
                        outputPt["x"] = (float)pt["x"];
                        outputPt["y"] = (float)pt["y"];
                        outputPt["z"] = (float)pt["z"];
                        outputPt["rx"] = (float)pt["rx"];
                        outputPt["ry"] = (float)pt["ry"];
                        outputPt["rz"] = (float)pt["rz"];
                        outputPt["dx"] = (float)pt["dx"];
                        outputPt["dy"] = (float)pt["dy"];
                        outputPt["dz"] = (float)pt["dz"];
                        outputPt["t"] = (float)pt["t"];
                        outputPt["d"] = Accrete(pt, aggregationMethod, percentile);

                        outputList.Add(outputPt);
                    }
                    outputData.Add(generated.Key, outputList);

                    hasData = generated.Value.Count > 0;
                    reportList.Add(generated.Value.Count);
                    if (!hasData)
                    {
                        break;
                    }
                }

                if (hasData)
                {
                    var reportArray = reportList.Select(x => x.ToString()).ToArray();

                    //Output what happened
                    string report = "Report of " + m_ReportFiles + " files:\n";
                    report += "Total of " + reportList.Count + " groups numbering [" + string.Join(",", reportArray) + "]\n";
                    report += "Total rows: " + m_ReportRows + "\n";
                    report += "Total points analyzed: " + m_ReportLegalPoints;
                    Debug.Log(report);

                    SaveFile(outputFileName, outputData);
                }
                else
                {
                    Debug.LogWarning("The aggregation process yielded no results.");
                }
            }
        }

        internal Dictionary<string, int> GetHeaders()
        {
            var retv = new Dictionary<string, int>();
            string path = System.IO.Path.Combine(m_DataPath, "RawData");
            path = System.IO.Path.Combine(path, "custom_headers.gz");
            string tsv = IonicGZip.DecompressFile(path);
            tsv = tsv.Replace("\n", "");
            List<string> rowData = new List<string>(tsv.Split('\t'));
            for (var a = 0; a < headerKeys.Length; a++)
            {
                retv.Add(headerKeys[a], rowData.IndexOf(headerKeys[a]));
            }
            return retv;
        }

        internal void LoadStream( Dictionary<Tuplish, List<HistogramHeatPoint>> histograms,
            Dictionary<string, int> headers,
            string path, 
            DateTime startDate, DateTime endDate,
            List<string> aggregateOn,
            Dictionary<string, float> smoothOn,
            List<string> groupOn,
            string remapDensityToField)
        {

            bool doRemap = !string.IsNullOrEmpty(remapDensityToField);
            if (doRemap)
            {
                aggregateOn.Add(remapDensityToField);
            }

            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarningFormat("File {0} not found.", path);
                return;
            }

            string tsv = IonicGZip.DecompressFile(path);

            string[] rows = tsv.Split('\n');
            m_ReportRows += rows.Length;

            // Define indices
            int nameIndex = headers["name"];
            int submitTimeIndex = headers["submit_time"];
            int paramsIndex = headers["custom_params"];
            int userIdIndex = headers["userid"];
            int sessionIdIndex = headers["sessionid"];
            int platformIndex = headers["platform"];
            int isDebugIndex = headers["debug_device"];

            for (int a = 0; a < rows.Length; a++)
            {
                List<string> rowData = new List<string>(rows[a].Split('\t'));
                if (rowData.Count < 6)
                {
                    // Re-enable this log if you want to see empty lines
                    //Debug.Log ("No data in line...skipping");
                    continue;
                }

                string userId = rowData[userIdIndex];
                string sessionId = rowData[sessionIdIndex];
                string eventName = rowData[nameIndex];
                string paramsData = rowData[paramsIndex];
                double unixTimeStamp = double.Parse(rowData[submitTimeIndex]);
                DateTime rowDate = DateTimeUtils.s_Epoch.AddMilliseconds(unixTimeStamp);


                string platform = rowData[platformIndex];
                bool isDebug = bool.Parse(rowData[isDebugIndex]);

                // Pass on rows outside any date trimming
                if (rowDate < startDate || rowDate > endDate)
                {
                    continue;
                }

                Dictionary<string, object> datum = MiniJSON.Json.Deserialize(paramsData) as Dictionary<string, object>;
                // If no x/y, this isn't a Heatmap Event. Pass.
                if (!datum.ContainsKey("x") || !datum.ContainsKey("y"))
                {
                    // Re-enable this log line if you want to be see events that aren't valid for heatmapping
                    //Debug.Log ("Unable to find x/y in: " + datum.ToString () + ". Skipping...");
                    continue;
                }

                // Passed all checks. Consider as legal point
                m_ReportLegalPoints++;

                // Construct both the list of elements that signify a unique item...
                var pointTupleList = new List<object>{ eventName };
                // ...and a point to contain the data
                HistogramHeatPoint point = new HistogramHeatPoint();
                foreach (var ag in aggregateOn)
                {
                    float floatValue = 0f;
                    object arbitraryValue = 0f;
                    // Special cases for userIDs, sessionIDs, platform, and debug, which aren't in the JSON
                    if (ag == "userID")
                    {
                        arbitraryValue = userId;
                    }
                    else if (ag == "sessionID")
                    {
                        arbitraryValue = sessionId;
                    }
                    else if (ag == "platform")
                    {
                        arbitraryValue = platform;
                    }
                    else if (ag == "debug")
                    {
                        arbitraryValue = isDebug;
                    }
                    else if (datum.ContainsKey(ag))
                    {
                        // parse and divide all in smoothing list
                        float.TryParse((string)datum[ag], out floatValue);
                        if (smoothOn.ContainsKey(ag))
                        {
                            floatValue = Divide(floatValue, smoothOn[ag]);
                        }
                        else
                        {
                            floatValue = 0;
                        }
                        arbitraryValue = floatValue;
                    }

                    pointTupleList.Add(arbitraryValue);
                    // Add values to the point
                    if (pointProperties.Contains(ag))
                    {
                        point[ag] = floatValue;
                    }
                }
                // Turn the pointTupleList into a key
                var pointTuple = new Tuplish(pointTupleList.ToArray());

                float remapValue = 1f;
                if (doRemap && datum.ContainsKey(remapDensityToField)) {
                    float.TryParse( (string)datum[remapDensityToField], out remapValue);
                }

                if (m_PointDict.ContainsKey(pointTuple))
                {
                    // Use existing point if it exists...
                    point = m_PointDict[pointTuple];
                    point.histogram.Add(remapValue);

                    if (rowDate < point.firstDate)
                    {
                        point.first = remapValue;
                        point.firstDate = rowDate;
                    }
                    else if (rowDate > point.lastDate)
                    {
                        point.last = remapValue;
                        point.lastDate = rowDate;
                    }
                }
                else
                {
                    // ...or else use the one we've been constructing
                    point.histogram.Add(remapValue);
                    point.first = remapValue;
                    point.last = remapValue;
                    point.firstDate = rowDate;
                    point.lastDate = rowDate;

                    // CREATE GROUPING LIST
                    var groupTupleList = new List<object>();
                    foreach (var field in groupOn)
                    {
                        // Special case for eventName
                        if (field == "eventName")
                        {
                            groupTupleList.Add(eventName);
                        }
                        // Special cases for... userID
                        else if (field == "userID")
                        {
                            groupTupleList.Add("user: " + userId);
                        }
                        // ... sessionID ...
                        else if (field == "sessionID")
                        {
                            groupTupleList.Add("session: " + sessionId);
                        }
                        // ... debug ...
                        else if (field == "debug")
                        {
                            groupTupleList.Add("debug: " + isDebug);
                        }
                        // ... platform
                        else if (field == "platform")
                        {
                            groupTupleList.Add("platform: " + platform);
                        }
                        // Everything else just added to key
                        else if (datum.ContainsKey(field))
                        {
                            groupTupleList.Add(field + ": " + datum[field]);
                        }
                    }
                    var groupTuple = new Tuplish(groupTupleList.ToArray());

                    // Create the event list if the key doesn't exist
                    if (!histograms.ContainsKey(groupTuple))
                    {
                        histograms.Add(groupTuple, new List<HistogramHeatPoint>());
                    }

                    // FINALLY, ADD THE POINT TO THE CORRECT GROUP...
                    histograms[groupTuple].Add(point);
                    // ...AND THE POINT DICT
                    m_PointDict[pointTuple] = point;
                }
            }
        }

        internal float Accrete(HistogramHeatPoint point, AggregationMethod aggregatioMethod, float percentile = 0f)
        {
            switch(aggregatioMethod)
            {
                case AggregationMethod.Increment:
                    return point.histogram.Count;
                case AggregationMethod.Cumulative:
                    return point.histogram.Sum(x => (float)Convert.ToDecimal(x));
                case AggregationMethod.Min:
                    point.histogram.Sort();
                    return point.histogram[0];
                case AggregationMethod.Max:
                    point.histogram.Sort();
                    return point.histogram[point.histogram.Count-1];
                case AggregationMethod.First:
                    return point.first;
                case AggregationMethod.Last:
                    return point.last;
                case AggregationMethod.Average:
                    return point.histogram.Sum(x => (float)Convert.ToDecimal(x))/point.histogram.Count;
                case AggregationMethod.Percentile:
                    point.histogram.Sort();
                    int percentileIdx = (int)((percentile/100f) * point.histogram.Count);
                    int finalIdx = (percentileIdx >= point.histogram.Count-1) ? point.histogram.Count-1 : percentileIdx;
                    return point.histogram[finalIdx];
            }
            return 0;
        }

        internal void SaveFile(string outputFileName, 
            Dictionary<Tuplish, List<Dictionary<string, float>>> outputData)
        {
            string savePath = System.IO.Path.Combine(m_DataPath, "RawData");
            if (!System.IO.Directory.Exists(savePath))
            {
                System.IO.Directory.CreateDirectory(savePath);
            }

            var json = MiniJSON.Json.Serialize(outputData);
            string jsonPath = savePath + Path.DirectorySeparatorChar + outputFileName;
            System.IO.File.WriteAllText(jsonPath, json);

            m_CompletionHandler(jsonPath);
        }

        protected float Divide(float value, float divisor)
        {
            if (divisor == 0f)
            {
                return value;
            }
            return Mathf.Round(value / divisor) * divisor;
        }
    }

    // Unity doesn't support Tuple, so here's a Tuple-like standin
    internal class Tuplish
    {

        List<object> objects;

        internal Tuplish(params object[] args)
        {
            objects = new List<object>(args);
        }

        public override bool Equals(object other)
        {
            return objects.SequenceEqual((other as Tuplish).objects);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            foreach (object o in objects)
            {
                hash = hash * 23 + (o == null ? 0 : o.GetHashCode());
            }
            return hash;
        }

        public override string ToString()
        {
            string s = "";
            foreach (var o in objects)
            {
                s += o.ToString() + "~";
            }
            if (s.LastIndexOf('~') == s.Length - 1)
            {
                s = s.Substring(0, s.Length - 1);
            }
            return s;
        }
    }

    class HistogramHeatPoint : Dictionary<string, object>
    {
        public List<float> histogram = new List<float>();

        public DateTime firstDate;
        public DateTime lastDate;

        public float first;
        public float last;
    }
}
