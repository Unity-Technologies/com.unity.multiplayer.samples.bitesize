/// <summary>
/// The suite of classes serving as the Raw Data Export client
/// </summary>
/// 
/// Raw Data Export (RDE) allows near-realtime fetching of raw data from the Analytics server,
/// but the API is more complicated than the 'Takeout' system that it replaces. Here's how it
/// works.
/// 
/// RDE instantiates 'Jobs' that roll up raw data into files based on an app key and a date range.
/// Calling RawDataClient.GetData() with an appropriate date range returns a JobRequest
/// with an appropriate m_JobId that uniquely identifies the Job. But this does NOT mean
/// that you have data. The server asynchronously collates the requested data, a process
/// which may take seconds to minutes to complete.
/// 
/// The RawDataClient polls the server, asking if the Job is complete. Once it is, the
/// manager's CompletionHandler fires, providing the requester with the desired data.
/// 
/// This process is human-slow (as mentioned, it could take minutes). So it is suggested
/// that a human-intervention step be inserted when the data is "baked", rather than
/// automatically pushing the data into an end-use service like heatmaps. In other words,
/// once baked, the kitchen timer should go "ding!"
/// 
/// RAW DATA API
/// Documentation for the Raw Data API currently can be found here:
/// http://docs.unity3d.com/540/Documentation/Manual/UnityAnalyticsRawDataExport.html


using System;
using System.Collections.Generic;
using System.Net;
using System.Collections.Specialized;
using System.Text;
using UnityEngine;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace UnityAnalytics
{

    public class RawDataClient
    {
        private const string BasePath = "https://analytics.cloud.unity3d.com/";
        private const string APIPath = BasePath + "api/v2/projects/";

        public const string CreateJobPath = APIPath + "{0}/rawdataexports";
        public const string JobStatusPath = APIPath + "{0}/rawdataexports/{1}";
        public const string GetJobsPath = APIPath + "{0}/rawdataexports";

        private static RawDataClient _instance;

        public static RawDataClient GetInstance()
        {
            if (_instance == null)
            {
                _instance = new RawDataClient();
            }
            return _instance;
        }

        public string DashboardPath
        {
            get
            {
                return BasePath + "raw_data/" + m_AppId;
            }
        }

        public string ConfigPath
        {
            get
            {
                return BasePath + "projects/" + m_AppId + "/edit";
            }
        }

        private const string k_ManifestFileName = "manifest.json";

        private List<RawDataReport> m_ReportList;

        protected string _appId;
        public string m_AppId
        {
            get
            {
                return _appId;
            }
            set
            {
                _appId = value;
            }
        }
        protected string _key;
        public string m_SecretKey
        {
            get
            {
                return _key;
            }
            set
            {
                _key = value;
            }
        }
        protected string _dataPath;
        public string m_DataPath
        {
            get
            {
                return _dataPath;
            }
            set
            {
                _dataPath = value;
            }
        }

        public delegate void JobsListCompletionHandler(bool success, List<RawDataReport> list, string reason = "");
        JobsListCompletionHandler m_GetJobsCompletionHandler;

        public RawDataClient()
        {
        }

        public RawDataReport ContinueFromJob(RawDataReport priorReport)
        {
            return ContinueFromJob(priorReport, DateTime.UtcNow);
        }

        public RawDataReport ContinueFromJob(RawDataReport priorReport, DateTime endDate)
        {
            RawDataReport report = null;
            using(WebClient client = new WebClient())
            {
                client.Encoding = System.Text.Encoding.UTF8;
                Authorization(client);
                string url = string.Format(CreateJobPath, m_AppId);
                string end = endDate.ToString("yyyy-MM-dd");
                string data = "\"continueFrom\":\"{0}\",\"endDate\":\"{1}\",\"format\":\"{2}\",\"dataset\":\"{3}\"";
                data = "{" + string.Format(data, priorReport.id, end, "tsv", priorReport.request.dataset) + "}";
                string result = client.UploadString(new Uri(url), "POST", data);
                var dict = MiniJSON.Json.Deserialize(result) as Dictionary<string, object>;
                report = new RawDataReport(dict);
                // fill in the startDate for reporting purposes
                report.request.startDate = priorReport.request.endDate;
            }

            return report;
        }

        public RawDataReport CreateJob(string type, DateTime startDate)
        {
            return CreateJob(type, startDate, DateTime.UtcNow);
        }

        public RawDataReport CreateJob(string type, DateTime startDate, DateTime endDate)
        {
            var request = new RawDataRequest(type, startDate, endDate);
            var report = SubmitRequest(request);
            return report;
        }

        private RawDataReport SubmitRequest(RawDataRequest request)
        {
            RawDataReport report = null;
            using(WebClient client = new WebClient())
            {
                client.Encoding = System.Text.Encoding.UTF8;
                Authorization(client);
                string url = string.Format(CreateJobPath, m_AppId);
                string start = request.startDate.ToString("yyyy-MM-dd");
                string end = request.endDate.ToString("yyyy-MM-dd");
                string data = "\"startDate\":\"{0}\",\"endDate\":\"{1}\",\"format\":\"{2}\",\"dataset\":\"{3}\"";
                data = "{" + string.Format(data, start, end, "tsv", request.dataset) + "}";
                string result = client.UploadString(new Uri(url), "POST", data);
                var dict = MiniJSON.Json.Deserialize(result) as Dictionary<string, object>;
                report = new RawDataReport(dict);
            }

            return report;
        }

        public List<RawDataReport> GetManifest()
        {
            List<RawDataReport> list = null;
            string path = PathFromFileName(k_ManifestFileName);
            if (File.Exists(path))
            {
                using(var stream = new StreamReader(path))
                {
                    string text = stream.ReadToEnd();
                    list = GenerateList(text);
                    TestFilesAreLocal(list);
                    if (m_GetJobsCompletionHandler != null)
                    {
                        m_GetJobsCompletionHandler(true, list);
                    }
                }
            }
            return list;
        }

        public List<string> GetFiles(UnityAnalyticsEventType[]eventTypes, DateTime start, DateTime end)
        {
            var eventList = new List<UnityAnalyticsEventType>(eventTypes);
            var resultsAsDict = new Dictionary<DateTime, KeyValuePair<DateTime, string>>();
            var resultList = new List<string>();
            var manifest = GetManifest();
            if (manifest != null)
            {
                for (var a = 0; a < manifest.Count; a++)
                {
                    var report = manifest[a];
                    var evt = (UnityAnalyticsEventType)Enum.Parse(typeof(UnityAnalyticsEventType), report.request.dataset);
                    if (eventList.Contains(evt) && report.status == RawDataReport.Completed && report.result.size > 0 && FilesHaveLoaded(report))
                    {
                        DateTime reportDate = report.createdAt;
                        var fileList = report.result.fileList;
                        for (var b = 0; b < fileList.Count; b++)
                        {
                            if (fileList[b].name.Contains("headers.gz"))
                            {
                                continue;
                            }
                            DateTime fileDate = DateTime.Parse(fileList[b].date).ToUniversalTime();
                            if (fileDate >= start && fileDate <= end)
                            {
                                var path = PathFromFileName(fileList[b].name);
                                KeyValuePair<DateTime, string> kv = new KeyValuePair<DateTime, string>(reportDate, path);
                                // Look for any matching date, but take the one from the more recent report
                                if (resultsAsDict.ContainsKey(fileDate))
                                {
                                    var existingKV = resultsAsDict[fileDate];
                                    kv = (existingKV.Key > reportDate) ? existingKV : kv;
                                    resultsAsDict.Remove(fileDate);
                                }
                                resultsAsDict.Add(fileDate, kv);
                            }
                        }
                    }
                }
                foreach(var kv in resultsAsDict)
                {
                    resultList.Add(kv.Value.Value);
                }
            }
            else
            {
                Debug.LogWarning("No manifest found");
            }
            return resultList;
        }

        public void GetJobs(JobsListCompletionHandler getJobsCompletionHandler)
        {
            m_GetJobsCompletionHandler = getJobsCompletionHandler;
            if (string.IsNullOrEmpty(m_DataPath))
            {
                return;
            }

            // Get and return local version
            GetManifest();

            if (string.IsNullOrEmpty(m_AppId) || string.IsNullOrEmpty(m_SecretKey))
            {
                return;
            }

            // Then get and return the remote version
            using(WebClient client = new WebClient())
            {
                client.Encoding = System.Text.Encoding.UTF8;
                Authorization(client);
                string url = string.Format(GetJobsPath, m_AppId);
                try
                {
                    string responsebody =  client.DownloadString(url);
                    SaveFile(k_ManifestFileName, responsebody);
                    var list = GenerateList(responsebody);
                    TestFilesAreLocal(list);
                    if (m_GetJobsCompletionHandler != null)
                    {
                        m_GetJobsCompletionHandler(true, list);
                    }
                }
                catch(WebException ex)
                {
                    if (m_GetJobsCompletionHandler != null)
                    {
                        m_GetJobsCompletionHandler(false, new List<RawDataReport>(), ex.Status.ToString());
                    }
                }
            }
        }

        public string GenerateManifest(List<RawDataReport> list)
        {
            var str = "[";
            for(var a = 0; a < list.Count; a++)
            {
                var report = list[a];
                str += report.ToString();
                if (a < list.Count-1)
                    str += ",";
            }
            str += "]";
            return str;
        }

        private List<RawDataReport> GenerateList(string text)
        {
            var reportRecordList = new List<RawDataReport>();
            var list = MiniJSON.Json.Deserialize(text) as List<object>;
            for (var a = 0; a < list.Count; a++)
            {
                var report = list[a] as Dictionary<string, object>;
                reportRecordList.Add(new RawDataReport(report));
            }
            return reportRecordList;
        }

        public void TestFilesAreLocal(List<RawDataReport> list)
        {
            if (list == null)
            {
                return;
            }
            for (var a = 0; a < list.Count; a++)
            {
                list[a].isLocal = FilesHaveLoaded(list[a]);
            }
        }

        public bool FilesHaveLoaded(RawDataReport report)
        {
            if (report.result == null || report.result.fileList.Count == 0)
            {
                return false;
            }
            for (var a = 0; a < report.result.fileList.Count; a++)
            {
                string fileName = report.result.fileList[a].name;
                if (fileName.Contains("headers.gz"))
                {
                    fileName = report.request.dataset + "_" + report.result.fileList[a].name;
                }
                if (!FileHasLoaded(fileName))
                {
                    return false;
                }
            }
            return true;
        }

        public bool FileHasLoaded(string name)
        {
            string path = PathFromFileName(name);
            return File.Exists(path);
        }

        private string PathFromFileName(string fileName)
        {
            string savePath = System.IO.Path.Combine(m_DataPath, "RawData");
            if (!System.IO.Directory.Exists(savePath))
            {
                System.IO.Directory.CreateDirectory(savePath);
            }
            return savePath + Path.DirectorySeparatorChar + fileName;
        }

        private void SaveFile(string outputFileName, string outputData)
        {
            File.WriteAllText(PathFromFileName(outputFileName), outputData);
        }

        public void DownloadAll(List<RawDataReport> jobs)
        {
            for (int a = 0; a < jobs.Count; a++)
            {
                var job = jobs[a];
                if (!FilesHaveLoaded(job))
                {
                    Download(jobs[a]);
                }
            }
        }

        public void Download(RawDataReport report)
        {
            for(int a = 0; a < report.result.fileList.Count; a++)
            {
                string file = report.result.fileList[a].url;
                using(WebClient client = new WebClient())
                {
                    string fileName = report.result.fileList[a].name;
                    if (fileName.Contains("headers.gz"))
                    {
                        fileName = report.request.dataset + "_" + report.result.fileList[a].name;
                    }
                    string savePath = PathFromFileName(fileName);
                    client.DownloadFile(file, savePath);
                }
            }
        }

        protected void Authorization(WebClient client)
        {
            #if UNITY_EDITOR_WIN
            // Bypassing SSL security in Windows to work around a CURL bug.
            // This is insecure and should be fixed when the Engine supports SSL.
            ServicePointManager.ServerCertificateValidationCallback = delegate(System.Object obj, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) {
            return true;
            };
            #endif
            string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(m_AppId + ":" + m_SecretKey));
            client.Headers.Add("Content-Type", "application/json");
            client.Headers.Add(HttpRequestHeader.Authorization, string.Format("Basic {0}", credentials));
        }
    }

    public class RawDataReport
    {
        public const string Completed = "completed";
        public const string Failed = "failed";
        public const string Running = "running";


        public string id;
        public string upid;
        public string status;
        public DateTime createdAt;
        public int duration;
        public RawDataRequest request;
        public RawDataResult result;
        private bool _isLocal = false;

        public bool isLocal
        {
            get
            {
                return _isLocal;
            }
            set
            {
                _isLocal = value;
            }
        }

        public RawDataReport(Dictionary<string, object> dict)
        {
            id = (string)dict["id"];
            upid = (string)dict["upid"];
            status = (string)dict["status"];
            createdAt = DateTime.Parse((string)dict["createdAt"]);
            duration = Convert.ToInt32(dict["duration"]);

            request = new RawDataRequest(dict["request"] as Dictionary<string, object>);
            result = (dict.ContainsKey("result") && dict["result"] != null) ? new RawDataResult(dict["result"] as Dictionary<string, object>) : null;
        }

        public RawDataReport(RawDataRequest request)
        {
            this.request = request;
        }

        internal static string NewProp(string key, string value, bool stripComma = false)
        {
            var comma = stripComma ? "" : ",";
            return "\"" + key + "\":\"" + value + "\"" + comma;
        }

        internal static string NewProp(string key, int value, bool stripComma = false)
        {
            var comma = stripComma ? "" : ",";
            return "\"" + key + "\":" + value + comma;
        }

        internal static string ToDateStr(DateTime date)
        {
            return date.ToString("o");
        }

        public override string ToString()
        {
            var str = "{";
            str += NewProp("id", id);
            str += NewProp("upid", upid);
            str += NewProp("status", status);
            str += NewProp("createdAt", ToDateStr(createdAt));
            str += NewProp("duration", duration);
            str += "\"request\":";
            str += request.ToString() + ",";
            str += "\"result\":";
            if (result != null)
            {
                str += result.ToString();
            }
            else
            {
                str += "\"null\"";
            }
            str += "}";
            return str;
        }
    }

    public class RawDataResult
    {
        public int size;
        public int eventCount;
        public List<RawDataFile> fileList;   //contains name, url, size, date
        public bool intraDay;

        public RawDataResult(Dictionary<string, object> dict)
        {
            size = (dict.ContainsKey("size")) ? Convert.ToInt32(dict["size"]) : 0;
            eventCount = (dict.ContainsKey("eventCount")) ? Convert.ToInt32(dict["eventCount"]) : 0;
            intraDay = (dict.ContainsKey("intraDay")) ? Convert.ToBoolean(dict["intraDay"]) : false;
            fileList = new List<RawDataFile>();

            if (dict.ContainsKey("fileList"))
            {
                var fileListFromDict = (dict["fileList"] == null) ? new List<object>() : dict["fileList"] as List<object>;

                foreach(var file in fileListFromDict)
                {
                    fileList.Add(new RawDataFile(file as Dictionary<string,object>));
                }
            }
        }

        public RawDataResult(int size, int eventCount, List<RawDataFile> list, bool intraDay)
        {
            this.size = size;
            this.eventCount = eventCount;
            this.intraDay = intraDay;
            fileList = list;
        }

        public override string ToString()
        {
            var str = "{";
            str += RawDataReport.NewProp("size", size);
            str += RawDataReport.NewProp("eventCount", eventCount);
            str += "\"fileList\":[";
            for (var a = 0; a < fileList.Count; a ++)
            {
                str += fileList[a].ToString();
                if (a < fileList.Count - 1)
                {
                    str += ",";
                }
            }
            str += "]}";
            return str;
        }
    }

    public class RawDataRequest
    {
        public DateTime startDate;
        public DateTime endDate;
        public string format;
        public string dataset;
        public string continueFrom;

        public RawDataRequest(Dictionary<string, object> dict)
        {
            if (dict.ContainsKey("startDate"))
                startDate = DateTime.Parse((string)dict["startDate"]).ToUniversalTime();
            if (dict.ContainsKey("continueFrom"))
                continueFrom = (string)dict["continueFrom"];
            endDate = DateTime.Parse((string)dict["endDate"]).ToUniversalTime();
            format = (string)dict["format"];
            dataset = (string)dict["dataset"];
        }

        public RawDataRequest(string type, DateTime start, DateTime end)
        {
            dataset = type;
            startDate = start;
            endDate = end;
            format = "tsv";
        }

        public RawDataRequest(RawDataRequest priorRequest, DateTime endDate)
        {
            startDate = priorRequest.endDate;
            this.endDate = endDate;
            format = priorRequest.format;
            dataset = priorRequest.dataset;
        }

        public override string ToString()
        {
            var str = "{";
            str += RawDataReport.NewProp("startDate", RawDataReport.ToDateStr(startDate));
            str += RawDataReport.NewProp("endDate", RawDataReport.ToDateStr(endDate));
            str += RawDataReport.NewProp("format", format);
            str += RawDataReport.NewProp("dataset", dataset, true);
            str += "}";
            return str;
        }
    }

    public class RawDataFile
    {
        public string name;
        public string url;
        public int size;
        public string date;

        public RawDataFile (Dictionary<string, object> dict)
        {
            name = (string)dict["name"];
            url = (string)dict["url"];
            size = Convert.ToInt32(dict["size"]);
            date = dict.ContainsKey("date") ? (string)dict["date"] : null;
        }

        public RawDataFile (string name, string url, int size, string date)
        {
            this.name = name;
            this.url = url;
            this.size = size;
            this.date = date;
        }

        public override string ToString()
        {
            var str = "{";
            str += RawDataReport.NewProp("name", name);
            str += RawDataReport.NewProp("url", url);
            str += RawDataReport.NewProp("size", size);
            str += RawDataReport.NewProp("date", date, true);
            str += "}";
            return str;
        }
    }

    /// <summary>
    /// JobRequest status.
    /// </summary>
    /// Mirrors the statuses returned from the server, adding 'Local'
    /// which indicates that the data was found to be local on the user's drive.
    public enum JobRequestStatus
    {
        created,
        submitted,
        finished,
        error,
        local
    }
}