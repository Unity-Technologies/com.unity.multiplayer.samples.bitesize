using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// A custom network manager that implements additional setup logic and rules
    /// </summary>
    [RequireComponent(typeof(NetworkManager))]
    internal class CustomNetworkManager : MonoBehaviour
    {
        const string k_DefaultServerListenAddress = "0.0.0.0";
        public static CustomNetworkManager Singleton { get; private set; }
        public static ConfigurationManager Configuration { get; private set; }
        internal static MultiplayAssignment s_AssignmentForCurrentGame;
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

        internal bool IsClient => m_NetworkManager.IsClient;
        internal bool IsServer => m_NetworkManager.IsServer;
        internal bool IsHost => m_NetworkManager.IsHost;

        internal Action ReturnToMetagame;
        int m_ExpectedPlayers = 2;

        [SerializeField]
        GameApplication m_GameAppPrefab;
        GameApplication m_GameApp;

        HashSet<Player> m_ReadyPlayers;
        NetworkManager m_NetworkManager;

        void Awake()
        {
            if (Singleton == null)
            {
                Singleton = this;
            }
            m_NetworkManager = GetComponent<NetworkManager>();
            m_NetworkManager.OnClientConnectedCallback += OnClientConnected;
            m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
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
        /// <param name="gameMode">The game mode to initialize</param>
        /// <param name="initiatedByUser">Was this setup manually initiated by the user, I.E: when starting a game manually?</param>
        public void PerformActionFromSetup(bool initiatedByUser = false)
        {
            if (IsClient || IsServer)
            {
                m_NetworkManager.Shutdown();
            }

            m_ExpectedPlayers = Configuration.GetInt(ConfigurationManager.k_MaxPlayers);

            var commandLineArgumentsParser = new CommandLineArgumentsParser();
            ushort listeningPort = commandLineArgumentsParser.ServerPort != -1 ? (ushort)commandLineArgumentsParser.ServerPort
                                                                               : (ushort)Configuration.GetInt(ConfigurationManager.k_Port);
            var transport = GetComponent<UnityTransport>();

            if (Configuration.GetBool(ConfigurationManager.k_ModeServer))
            {
                Debug.Log($"Starting server on port {listeningPort}, expecting {m_ExpectedPlayers}");
                Application.targetFrameRate = 60; //lock framerate on dedicated servers
                SetNetworkPortAndAddress(listeningPort, k_DefaultServerListenAddress, k_DefaultServerListenAddress);
                m_NetworkManager.StartServer();
                return;
            }

            if (Configuration.GetBool(ConfigurationManager.k_ModeHost))
            {
                if (AutoConnectOnStartup || initiatedByUser)
                {
                    Debug.Log($"Starting Host on port {listeningPort}, expecting {m_ExpectedPlayers}");
                    SetNetworkPortAndAddress(listeningPort, k_DefaultServerListenAddress, k_DefaultServerListenAddress);
                    m_NetworkManager.StartHost();
                }
                return;
            }

            if (Configuration.GetBool(ConfigurationManager.k_ModeClient))
            {
                if (IsClient)
                {
                    Debug.Log("Already connected!");
                    return;
                }

                if (AutoConnectOnStartup)
                {
                    SetNetworkPortAndAddress(listeningPort, Configuration.GetString(ConfigurationManager.k_ServerIP), k_DefaultServerListenAddress);
                    m_NetworkManager.StartClient();
                    return;
                }

                if (initiatedByUser)
                {
                    SetNetworkPortAndAddress((ushort)s_AssignmentForCurrentGame.Port, s_AssignmentForCurrentGame.Ip, k_DefaultServerListenAddress);
                    Debug.Log($"Attempting to connect to: {transport.ConnectionData.Address}:{transport.ConnectionData.Port}");
                    m_NetworkManager.StartClient();
                    return;
                }
            }
        }

        void SetNetworkPortAndAddress(ushort port, string address, string serverListenAddress)
        {
            var transport = GetComponent<UnityTransport>();
            if (transport == null) //happens during Play Mode Tests
            {
                return;
            }
            transport.SetConnectionData(address, port, serverListenAddress);
        }

        void OnClientDisconnected(ulong ClientId)
        {
            Debug.Log($"Client {ClientId} disconnected");
            if (IsServer)
            {
                m_ReadyPlayers.RemoveWhere(p => p.NetworkObject == m_NetworkManager.ConnectedClients[ClientId].PlayerObject);
            }
        }

        void OnClientConnected(ulong ClientId)
        {
            Debug.Log($"Remote or local client {ClientId} connected");
            if (IsServer && m_NetworkManager.ConnectedClients.Count == m_ExpectedPlayers)
            {
                OnServerPrepareGame();
            }
        }

        internal void OnServerPlayerIsReady(Player player)
        {
            m_ReadyPlayers.Add(player);
            if (m_ReadyPlayers.Count == m_ExpectedPlayers)
            {
                OnServerGameReadyToStart();
            }
        }

        void OnServerPrepareGame()
        {
            m_ReadyPlayers = new HashSet<Player>();
            Debug.Log("[S] Preparing game");
            InstantiateGameApplication();
            foreach (var connectionToClient in m_NetworkManager.ConnectedClients.Values)
            {
                connectionToClient.PlayerObject.GetComponent<Player>().OnClientPrepareGameClientRpc();
            }
        }

        internal void InstantiateGameApplication()
        {
            m_GameApp = Instantiate(m_GameAppPrefab);
        }

        internal void OnServerGameReadyToStart()
        {
            m_GameApp.Broadcast(new StartMatchEvent(true, false));
            foreach (var player in m_ReadyPlayers)
            {
                player.OnClientStartGameClientRpc();
            }
            m_ReadyPlayers.Clear();
        }

        /// <summary>
        /// Performs cleanup operation after a game
        /// </summary>
        internal void OnClientDoPostMatchCleanupAndReturnToMetagame()
        {
            if (IsClient)
            {
                m_NetworkManager.Shutdown();
            }
            Destroy(GameApplication.Instance.gameObject);
            ReturnToMetagame?.Invoke();
        }

        internal void OnEnteredMatchmaker()
        {
            s_AssignmentForCurrentGame = null;
        }
    }
}
