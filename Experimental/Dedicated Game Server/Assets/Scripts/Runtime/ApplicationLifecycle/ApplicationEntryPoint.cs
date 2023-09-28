using System;
using Unity.DedicatedGameServerSample.Runtime.ConnectionManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.DedicatedGameServerSample.Runtime.ApplicationLifecycle
{
    /// <summary>
    /// This is the application's entry point, where the configuration is read and the application is initialized
    /// accordingly. This also keeps references to systems that must persist throughout the application's lifecyle.
    /// </summary>
    public class ApplicationEntryPoint : MonoBehaviour
    {
        const string k_DefaultServerListenAddress = "0.0.0.0";
        public static ApplicationEntryPoint Singleton { get; private set; }
        public static ConfigurationManager Configuration { get; private set; }

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
        
        [SerializeField]
        ConnectionManager m_ConnectionManager;

        public ConnectionManager ConnectionManager => m_ConnectionManager;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            if (Singleton is null)
            {
                Singleton = this;
            }
        }

        [RuntimeInitializeOnLoadMethod]
        static void OnApplicationStarted()
        {
            if (!Singleton) //this happens during PlayMode tests
            {
                return;
            }
            Configuration = new ConfigurationManager(ConfigurationManager.k_DevConfigFile);
            Singleton.InitializeNetworkLogic(); //note: this is the entry point for all autoconnected instances (including standalone servers)
        }

        public void SetConfiguration(ConfigurationManager configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Initializes the application's network-related behaviour according to the configuration. Servers load the main
        /// game scene and automatically start. Clients load the metagame scene and, if autonnect is set to true, attempt
        /// to connect to a server automatically based on the IP and port passed through the configuration or the command
        /// line arguments.
        /// </summary>
        void InitializeNetworkLogic()
        {
            var commandLineArgumentsParser = new CommandLineArgumentsParser();
            ushort listeningPort = commandLineArgumentsParser.ServerPort != -1 ? (ushort)commandLineArgumentsParser.ServerPort
                                                                               : (ushort)Configuration.GetInt(ConfigurationManager.k_Port);
            if (Configuration.GetBool(ConfigurationManager.k_ModeServer)) //todo replace those configs with ContentSelection profiles
            {
                SceneManager.LoadScene("GameScene01");
                Application.targetFrameRate = 60; //lock framerate on dedicated servers
                m_ConnectionManager.StartServerIP(k_DefaultServerListenAddress, listeningPort);
                return;
            }

            if (Configuration.GetBool(ConfigurationManager.k_ModeClient))
            {
                SceneManager.LoadScene("MetagameScene");
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
            SceneManager.LoadScene("MetagameScene");
        }
    }
}
