using System;
using System.Collections;
using Unity.DedicatedGameServerSample.Runtime.ConnectionManagement;
using Unity.Multiplayer;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.DedicatedGameServerSample.Runtime.ApplicationLifecycle
{
    /// <summary>
    /// This is the application's entry point, where the configuration is read and the application is initialized
    /// accordingly. This also keeps references to systems that must persist throughout the application's lifecycle.
    /// </summary>
    [MultiplayerRoleRestricted]
    public class ApplicationEntryPoint : MonoBehaviour
    {
        const string k_DefaultServerListenAddress = "0.0.0.0";
        const string k_DefaultClientAutoConnectServerAddress = "127.0.0.1";
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
            m_ConnectionManager.EventManager.AddListener<ConnectionEvent>(OnConnectionEvent);
        }

        void OnDestroy()
        {
            m_ConnectionManager.EventManager.RemoveListener<ConnectionEvent>(OnConnectionEvent);
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
            ushort listeningPort = (ushort) commandLineArgumentsParser.Port;
            switch (MultiplayerRolesManager.ActiveMultiplayerRoleMask)
            {
                case MultiplayerRoleFlags.Server:
                    //lock framerate on dedicated servers
                    Application.targetFrameRate = commandLineArgumentsParser.TargetFramerate;
                    QualitySettings.vSyncCount = 0;
                    m_ConnectionManager.StartServerIP(k_DefaultServerListenAddress, listeningPort);
                    NetworkManager.Singleton.SceneManager.LoadScene("GameScene01", LoadSceneMode.Single);
                    break;
                case MultiplayerRoleFlags.Client:
                {
                    SceneManager.LoadScene("MetagameScene");
                    if (AutoConnectOnStartup)
                    {
                        m_ConnectionManager.StartClient(k_DefaultClientAutoConnectServerAddress, listeningPort);
                    }
                    break;
                }
                case MultiplayerRoleFlags.ClientAndServer:
                    throw new ArgumentOutOfRangeException("MultiplayerRole", "ClientAndServer is an invalid multiplayer role in this sample. Please select the Client or Server role.");
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

        void OnConnectionEvent(ConnectionEvent evt)
        {
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask == MultiplayerRoleFlags.Server)
            {
                switch (evt.status)
                {
                    case ConnectStatus.GenericDisconnect:
                    case ConnectStatus.ServerEndedSession:
                    case ConnectStatus.StartServerFailed:
                        // If server ends networked session or fails to start, quit the application
                        Quit();
                        break;
                }
            }
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}