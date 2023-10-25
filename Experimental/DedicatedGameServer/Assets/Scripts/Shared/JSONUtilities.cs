using System.IO;
using System.Threading.Tasks;
using Unity.DedicatedGameServerSample.Runtime.SimpleJSON;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Utility class for JSON files
    /// </summary>
    public class JSONUtilities
    {
        /// <summary>
        /// Reads the content of a JSON file and returns it
        /// </summary>
        /// <param name="filePath"> path of the JSON file</param>
        /// <returns>the encoded JSON if the reading was successfull, a "Status : failed" JSON otherwise</returns>
        public static JSONNode ReadJSONFromFile(string filePath)
        {
            StreamReader reader = null;
            try
            {
                reader = new StreamReader(filePath);
                return JSONNode.Parse(reader.ReadToEnd());
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
            return JSONNode.Parse("{ \"Status\" : \"FAILED\" }");
        }

        /// <summary>
        /// Reads the content of a JSON file and returns it, asynchronously
        /// </summary>
        /// <param name="filePath"> path of the JSON file</param>
        /// <returns>the encoded JSON if the reading was successfull, a "Status : failed" JSON otherwise</returns>
        public static async Task<JSONNode> ReadJSONFromFileAsync(string filePath)
        {
            StreamReader reader = null;
            try
            {
                reader = new StreamReader(filePath);
                string fileContent = await reader.ReadToEndAsync().ConfigureAwait(false);
                return JSONNode.Parse(fileContent);
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
            return JSONNode.Parse("{ \"Status\" : \"FAILED\" }");
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
    }
}
