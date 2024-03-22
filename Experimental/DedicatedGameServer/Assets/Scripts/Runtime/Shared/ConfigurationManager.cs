using System.IO;
using System.Linq;
using Unity.DedicatedGameServerSample.Runtime.SimpleJSON;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// A configuration Manager for easily accessing dynamic configurations that alter the behaviour of the app
    /// </summary>
    public class ConfigurationManager
    {
        #region DeveloperSetupFileData
        /// <summary>
        /// Name of the configuration file
        /// </summary>
        public const string k_DevConfigFile = "startupConfiguration.json";

        /// <summary>
        /// Where the default configuration file is stored.
        /// </summary>
        public static readonly string k_DevConfigFileDefault = Path.Combine(Application.dataPath, Path.Combine("Resources", Path.Combine("DefaultConfigurations", k_DevConfigFile)));
        /// <summary>
        /// Players the server expects in a match before allowing it to start
        /// </summary>
        public const string k_MinPlayers = "MinPlayers";
        /// <summary>
        /// Maximum number of players the server expects in a match
        /// </summary>
        public const string k_MaxPlayers = "MaxPlayers";
        /// <summary>
        /// Will the game startup behaviour change according to the settings?
        /// </summary>
        public const string k_Autoconnect = "AutoConnect";
        #endregion

        /// <summary>
        /// Meta-configuration file used to automate processes
        /// </summary>
        JSONNode m_Config;
        string m_ConfigFilePath;

        /// <summary>
        /// Initializes the ConfigurationManager
        /// </summary>
        /// <param name="configFilePath">path of the configuration file</param>
        public ConfigurationManager(string configFilePath)
        {
            LoadConfigurationFromFile(configFilePath, true, false);
        }

        /// <summary>
        /// Initializes the ConfigurationManager
        /// </summary>
        /// <param name="configFilePath">path of the configuration file</param>
        /// <param name="keepUninitialized">if true, the initialization is not performed</param>
        public ConfigurationManager(string configFilePath, bool keepUninitialized)
        {
            m_ConfigFilePath = configFilePath;
            if (keepUninitialized)
            {
                return;
            }
            LoadConfigurationFromFile(configFilePath, true, false);
        }

        /// <summary>
        /// Loads the configuration file
        /// </summary>
        /// <param name="configFilePath">Path of the configuration file.</param>
        /// <param name="createIfNotExists">If true, creates the configuration file when it doesn't exist.</param>
        /// <param name="updateIfOutdated">If true, new default settings will be integrated in the existing configuration.</param>
        void LoadConfigurationFromFile(string configFilePath, bool createIfNotExists, bool updateIfOutdated)
        {
            m_ConfigFilePath = configFilePath;
            string templatePath = Path.Combine("DefaultConfigurations", configFilePath.Split('.')[0]);
            if (!File.Exists(configFilePath))
            {
                if (!createIfNotExists)
                {
                    throw new FileNotFoundException($"{configFilePath} not found");
                }

                m_Config = JSONNode.Parse(Resources.Load<TextAsset>(templatePath).text);
                JSONUtilities.WriteJSONToFile(configFilePath, m_Config, false);
                return;
            }

            m_Config = JSONUtilities.ReadJSONFromFile(configFilePath);

            if (!updateIfOutdated)
            {
                return;
            }

            /*
             * Since user settings may change between versions, we need to be sure that we update them
             * when new ones come up.
             */
            JSONNode template = JSONNode.Parse(Resources.Load<TextAsset>(templatePath).text);
            var newSettings = template.Keys.Except(m_Config.Keys);
            foreach (var item in newSettings)
            {
                m_Config[item] = template[item].Value;
            }
            JSONUtilities.WriteJSONToFile(configFilePath, m_Config, false);
        }

        /// <summary>
        /// Removes a key from the configuration
        /// </summary>
        /// <param name="key">The configuration key.</param>
        public void Remove(string key) => m_Config.Remove(key);
        /// <summary>
        /// Checks if a key exists in the configuration
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>True if the key exists, false otherwise</returns>
        public bool Contains(string key) => m_Config.Keys.Any(k => k == key);

        /// <summary>
        /// Sets the value of a key in the configuration
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <param name="value">The value</param>
        /// <remarks>value must implement ToString()</remarks>
        public void Set(string key, object value) => m_Config[key] = value.ToString();
        /// <summary>
        /// Gets the value of a key in the configuration
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>the value of the string in the configuration, as string</returns>
        public string GetString(string key) => m_Config[key].Value;
        /// <summary>
        /// Gets the value of a key in the configuration
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>the value of the string in the configuration, as bool</returns>
        public bool GetBool(string key) => m_Config[key].AsBool;
        /// <summary>
        /// Gets the value of a key in the configuration
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>the value of the string in the configuration, as int</returns>
        public int GetInt(string key) => m_Config[key].AsInt;
        /// <summary>
        /// Gets the value of a key in the configuration
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>the value of the string in the configuration, as float</returns>
        public float GetFloat(string key) => m_Config[key].AsFloat;
        /// <summary>
        /// Saves the confiuration as a JSON file
        /// </summary>
        /// <param name="singleLine">If true, the JSON is saved as a one-liner</param>
        public void SaveAsJSON(bool singleLine)
        {
            SaveAsJSON(m_ConfigFilePath, singleLine);
        }

        void SaveAsJSON(string path, bool singleLine)
        {
            JSONUtilities.WriteJSONToFile(path, m_Config, singleLine);
        }

        /// <summary>
        /// Overwrites the existing configuration with a new one
        /// </summary>
        /// <param name="newConfiguration">The new configuration to use</param>
        public void Overwrite(JSONNode newConfiguration)
        {
            m_Config = JSONNode.Parse(newConfiguration.ToString());
        }
    }
}
