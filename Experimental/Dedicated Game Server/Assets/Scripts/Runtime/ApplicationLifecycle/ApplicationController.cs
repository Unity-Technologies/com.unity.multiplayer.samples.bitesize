using System;
using Unity.Template.Multiplayer.NGO.Runtime.ConnectionManagement;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime.ApplicationLifecycle
{
    public class ApplicationController : MonoBehaviour
    {
        const string k_DefaultServerListenAddress = "0.0.0.0";
        public static ApplicationController Singleton { get; private set; }
        public static ConfigurationManager Configuration { get; private set; }

        public bool UsingBots => Configuration.GetBool(ConfigurationManager.k_EnableBots);
#if UNITY_EDITOR
        public static bool s_AreTestsRunning = false;
        public bool AreTestsRunning => s_AreTestsRunning;
#endif
        bool AutoConnectOnStartup
        {
            get
            {
                bool startAutomatically = Configuration.GetBool(ConfigurationManager.k_Autoconnect);
#if UNITY_EDITOR
                startAutomatically |= AreTestsRunning;
#endif
                return startAutomatically;
            }
        }

        internal Action ReturnToMetagame;

        [SerializeField]
        ConnectionManager m_ConnectionManager;

        void Awake()
        {
            /*
            m_NetworkManager = GetComponent<NetworkManager>();
            m_NetworkManager.OnClientConnectedCallback += OnClientConnected;
            m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            */
        }

        [RuntimeInitializeOnLoadMethod]
        static void OnApplicationStarted()
        {
            if (!Singleton) //this happens during PlayMode tests
            {
                return;
            }
            Configuration = new ConfigurationManager(ConfigurationManager.k_DevConfigFile);
            if (!Configuration.GetBool(ConfigurationManager.k_ModeServer))
            {
                //todo: enable this once you set up the Player configuration system
                //PlayerConfiguration = new ConfigurationManager(ConfigurationManager.USER_CONFIG_FILE, true, true);
                //MetagameApplication.instance?.view.settingsWindow.ReloadSettings();
            }
            Singleton.PerformActionFromSetup(); //note: this is the entry point for all autoconnected instances (including standalone servers)
        }

        public void SetConfiguration(ConfigurationManager configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Does something according to what is in the configuration file
        /// </summary>
        public void PerformActionFromSetup()
        {

            var commandLineArgumentsParser = new CommandLineArgumentsParser();
            ushort listeningPort = commandLineArgumentsParser.ServerPort != -1 ? (ushort)commandLineArgumentsParser.ServerPort
                                                                               : (ushort)Configuration.GetInt(ConfigurationManager.k_Port);
            if (Configuration.GetBool(ConfigurationManager.k_ModeServer))
            {
                Application.targetFrameRate = 60; //lock framerate on dedicated servers
                m_ConnectionManager.StartServerIP(k_DefaultServerListenAddress, listeningPort);
                return;
            }

            if (Configuration.GetBool(ConfigurationManager.k_ModeClient))
            {
                if (AutoConnectOnStartup)
                {
                    m_ConnectionManager.StartClient(Configuration.GetString(ConfigurationManager.k_ServerIP), listeningPort);
                }
            }
        }

        /// <summary>
        /// Performs cleanup operation after a game
        /// </summary>
        internal void OnClientDoPostMatchCleanupAndReturnToMetagame()
        {
            m_ConnectionManager.RequestShutdown();
            ReturnToMetagame?.Invoke();
        }
    }
}
