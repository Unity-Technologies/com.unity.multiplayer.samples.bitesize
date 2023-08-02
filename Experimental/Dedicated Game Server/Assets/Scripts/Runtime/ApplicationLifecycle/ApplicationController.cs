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

        [SerializeField]
        GameApplication m_GameAppPrefab;
        GameApplication m_GameApp;

        //HashSet<Player> m_ReadyPlayers;
        //NetworkManager m_NetworkManager;

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

        void OnClientDisconnected(ulong ClientId)
        {
            Debug.Log($"Client {ClientId} disconnected");
            /*if (IsServer)
            {
                m_ReadyPlayers.RemoveWhere(p => p.NetworkObject == m_NetworkManager.ConnectedClients[ClientId].PlayerObject);
            }*/
        }

        void OnClientConnected(ulong ClientId)
        {
            Debug.Log($"Remote or local client {ClientId} connected");
            /*if (IsServer && m_NetworkManager.ConnectedClients.Count == m_ExpectedPlayers)
            {
                OnServerPrepareGame();
            }*/
        }

        internal void OnServerPlayerIsReady(Player player)
        {
            /*m_ReadyPlayers.Add(player);
            if (m_ReadyPlayers.Count == m_ExpectedPlayers)
            {
                OnServerGameReadyToStart();
            }*/
        }

        void OnServerPrepareGame()
        {
            /*m_ReadyPlayers = new HashSet<Player>();
            Debug.Log("[S] Preparing game");
            InstantiateGameApplication();
            foreach (var connectionToClient in m_NetworkManager.ConnectedClients.Values)
            {
                connectionToClient.PlayerObject.GetComponent<Player>().OnClientPrepareGameClientRpc();
            }*/
        }

        internal void InstantiateGameApplication()
        {
            m_GameApp = Instantiate(m_GameAppPrefab);
        }

        internal void OnServerGameReadyToStart()
        {
            /*m_GameApp.Broadcast(new StartMatchEvent(true, false));
            foreach (var player in m_ReadyPlayers)
            {
                player.OnClientStartGameClientRpc();
            }
            m_ReadyPlayers.Clear();*/
        }

        /// <summary>
        /// Performs cleanup operation after a game
        /// </summary>
        internal void OnClientDoPostMatchCleanupAndReturnToMetagame()
        {
            m_ConnectionManager.RequestShutdown();
            Destroy(GameApplication.Instance.gameObject);
            ReturnToMetagame?.Invoke();
        }
    }
}
