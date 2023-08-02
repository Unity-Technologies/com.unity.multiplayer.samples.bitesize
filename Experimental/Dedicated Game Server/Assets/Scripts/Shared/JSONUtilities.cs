using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Unity.Template.Multiplayer.NGO.Runtime.SimpleJSON;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Utility class for JSON files
    /// </summary>

    public class JSONUtilities : MonoBehaviour
    {

        /// <summary>
        /// Reads the content of a JSON file and returns it
        /// </summary>
        /// <param name="filePath"> path of the JSON file</param>
        /// <returns>the encoded JSON if the reading was successfull, a "Status : failed" JSON otherwise</returns>

        public static JSONNode ReadJSONFromFile(string filePath)
        {
            StreamReader reader = null;
            JSONNode finalJSON = JSONNode.Parse("{ \"Status\" : \"FAILED\" }");
            try
            {
                reader = new StreamReader(filePath);
                finalJSON = JSONNode.Parse(reader.ReadToEnd());
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }
            return finalJSON;
        }

        public static void WriteJSONStringToFile(string filePath, string fileContent, bool append = false)
        {
            StreamWriter writer = null;
            try
            {
                writer = new StreamWriter(filePath, append);
                writer.WriteLine(fileContent);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                    writer.Dispose();
                }
            }
        }


        public static void WriteStringToFile(string filePath, string content)
        {
            EnsureDirectoryExists(filePath);
            StreamWriter writer = null;
            try
            {
                writer = new StreamWriter(filePath, false);
                writer.WriteLine(content);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                    writer.Dispose();
                }
            }
        }

        /// <summary>
        /// Writes the content of a JSON in a file
        /// </summary>
        /// <param name="filePath"> path of the JSON file</param>
        /// <param name="content">content of the JSON that you want to write</param>
        /// <param name="singleLine">Should the JSON be written without spaces?</param>
        /// <param name="append">Should the original file be completeley rewritten? (if exists)</param>

        public static void WriteJSONToFile(string filePath, JSONNode content, bool singleLine = false, bool append = false)
        {
            EnsureDirectoryExists(filePath);
            StreamWriter writer = null;
            try
            {
                writer = new StreamWriter(filePath, append);
                if (singleLine)
                {
                    writer.WriteLine(content.ToString());
                }
                else
                {
                    writer.WriteLine(content.ToString(""));
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                    writer.Dispose();
                }
            }
        }

        static void EnsureDirectoryExists(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            if (!fi.Directory.Exists)
            {
                Directory.CreateDirectory(fi.DirectoryName);
            }
        }

        /// <summary>
        /// Orders a simple JSON of Ints in descending order
        /// </summary>
        /// <param name="jsonToOrder">The JSON to order. It must be a JSON with simple key-IntValue pairs</param>
        /// <returns>A new JSON object representing the ordered JSON</returns>
        public static JSONNode GetOrderedJSONDescByInt(JSONNode jsonToOrder)
        {
            JSONNode jsonCopy = JSONNode.Parse("{}");
            List<KeyValuePair<string, JSONNode>> list = Cast<KeyValuePair<string, JSONNode>>(jsonToOrder.AsObject.GetEnumerator()).ToList();
            foreach (var item in list.OrderByDescending(p => p.Value.AsInt))
            {
                jsonCopy[item.Key] = item.Value;
            }
            return jsonCopy;
        }

        /// <summary>
        /// Orders a simple JSON of Ints in ascending order
        /// </summary>
        /// <param name="jsonToOrder">The JSON to order. It must be a JSON with simple key-IntValue pairs</param>
        /// <returns>A new JSON object representing the ordered JSON</returns>
        public static JSONNode GetOrderedJSONAscByInt(JSONNode jsonToOrder)
        {
            JSONNode jsonCopy = JSONNode.Parse("{}");
            List<KeyValuePair<string, JSONNode>> list = Cast<KeyValuePair<string, JSONNode>>(jsonToOrder.AsObject.GetEnumerator()).ToList();
            foreach (var item in list.OrderBy(p => p.Value.AsInt))
            {
                jsonCopy[item.Key] = item.Value;
            }
            return jsonCopy;
        }

        /// <summary>
        /// Casts and IEnumerator element to an IEnumerable Type
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="iterator"></param>
        /// <returns></returns>
        static IEnumerable<T> Cast<T>(IEnumerator iterator)
        {
            while (iterator.MoveNext())
            {
                yield return (T)iterator.Current;
            }
        }

        /// <summary>
        /// Tries to retrieve a file. 
        /// If it doesn't exist, creates a file in the desired location with the specified content.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="defaultContent"></param>
        /// <returns></returns>
        public static JSONNode CreateOrRetrieveFile(string filePath, string defaultContent)
        {
            if (File.Exists(filePath)) { return ReadJSONFromFile(filePath); }
            JSONNode file = JSONNode.Parse(defaultContent);
            WriteJSONToFile(filePath, file, false);
            return file;
        }

        public delegate JSONNode OnFileNotFoundHandler(string filePath);

        /// <summary>
        /// Tries to retrieve a file. 
        /// If it doesn't exist, a callback is invoked.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="OnFileNotFoundCallback"></param>
        /// <returns></returns>
        public static JSONNode CreateOrRetrieveFile(string filePath, OnFileNotFoundHandler OnFileNotFoundCallback)
        {
            if (File.Exists(filePath)) { return ReadJSONFromFile(filePath); }
            return OnFileNotFoundCallback(filePath);
        }

        /// <summary>
        /// Adds all the elements of the second array to the first array
        /// </summary>
        /// <param name="destinationArray"></param>
        /// <param name="fromArray"></param>
        public static void AddElementsFromArray(JSONArray destinationArray, JSONArray fromArray)
        {
            foreach (JSONNode item in fromArray)
            {
                destinationArray.Add(item);
            }
        }

        public static void PrintChildren(JSONNode json)
        {
            Debug.Log(json.ToString(""));

            foreach (KeyValuePair<string, JSONNode> item in json.ChildsWithKeys)
            {
                Debug.Log(item.Key + " > " + item.Value.ToString(""));
            }
        }
    }
}
