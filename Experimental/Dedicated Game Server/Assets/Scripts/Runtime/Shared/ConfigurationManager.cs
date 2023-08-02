using System.IO;
using System.Linq;
using Unity.Template.Multiplayer.NGO.Runtime.SimpleJSON;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// A configuration Manager for easily accessing dynamic configurations that alter the behaviour of the app
    /// </summary>
    public class ConfigurationManager
    {
        #region DeveloperSetupFileData
        public const string k_DevConfigFile = "startupConfiguration.json";
        public static readonly string k_DevConfigFileDefault = Path.Combine(Application.dataPath, Path.Combine("Resources", Path.Combine("DefaultConfigurations", k_DevConfigFile)));

        const string k_PlayerConfigFolder = "Player"; //todo: to be used for player-specific configuration files (I.E: game settings)
        const string k_PlayerConfigFileName = "settings.json";
        static readonly string s_PlayerConfigFilePath = Path.Combine(k_PlayerConfigFolder, k_PlayerConfigFileName);
        public const string k_ModeServer = "StartAsServer";
        public const string k_ModeClient = "StartAsClient";
        public const string k_MaxPlayers = "MaxPlayers";
        public const string k_Port = "Port";
        public const string k_EnableBots = "EnableBots";
        public const string k_ServerIP = "ServerIP";
        public const string k_Autoconnect = "AutoConnect";
        #endregion

        /// <summary>
        /// Meta-configuration file used to automate processes
        /// </summary>
        JSONNode m_Config;
        string m_ConfigFilePath;

        public ConfigurationManager(string configFilePath)
        {
            LoadSetupFromFile(configFilePath, false, false);
        }

        public ConfigurationManager(string configFilePath, bool keepUninitialized)
        {
            this.m_ConfigFilePath = configFilePath;
            if (keepUninitialized)
            {
                return;
            }
            LoadSetupFromFile(configFilePath, false, false);
        }
        public ConfigurationManager(string configFilePath, bool createIfNotExists, bool updateIfOutdated)
        {
            LoadSetupFromFile(configFilePath, createIfNotExists, updateIfOutdated);
        }

        public ConfigurationManager(JSONNode config) { this.m_Config = config; }

        /// <summary>
        /// Loads the lobby setup from the configuration file
        /// </summary>
        void LoadSetupFromFile(string configFilePath, bool createIfNotExists, bool updateIfOutdated)
        {
            this.m_ConfigFilePath = configFilePath;
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

            if (!updateIfOutdated) { return; }

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

        public void Remove(string key) => m_Config.Remove(key);
        public bool Contains(string key) => m_Config.Keys.Any(k => k == key);
        public string Set(string key, object value) => m_Config[key] = value.ToString();
        public string GetString(string key) => m_Config[key].Value;
        public bool GetBool(string key) => m_Config[key].AsBool;
        public int GetInt(string key) => m_Config[key].AsInt;
        public float GetFloat(string key) => m_Config[key].AsFloat;

        public void SaveAsJSON(bool singleLine)
        {
            SaveAsJSON(m_ConfigFilePath, singleLine);
        }

        void SaveAsJSON(string path, bool singleLine)
        {
            JSONUtilities.WriteJSONToFile(path, m_Config, singleLine);
        }

        public void Overwrite(JSONNode newConfiguration)
        {
            m_Config = JSONNode.Parse(newConfiguration.ToString());
        }
    }
}
