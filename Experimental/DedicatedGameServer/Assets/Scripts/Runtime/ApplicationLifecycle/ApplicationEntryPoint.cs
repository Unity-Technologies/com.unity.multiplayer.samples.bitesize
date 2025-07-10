using System;
using Unity.DedicatedGameServerSample.Runtime.ConnectionManagement;
using Unity.Multiplayer;
using Unity.Multiplayer.Playmode;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using ConnectionEvent = Unity.DedicatedGameServerSample.Runtime.ConnectionManagement.ConnectionEvent;

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
        public static ApplicationEntryPoint Instance { get; private set; }

#if UNITY_EDITOR
        public static bool s_AreTestsRunning = false;
        public bool AreTestsRunning => s_AreTestsRunning;
#endif
        public bool AutoConnectOnStartup
        {
            get
            {
                var startAutomatically = false;
                switch (MultiplayerRolesManager.ActiveMultiplayerRoleMask)
                {
                    case MultiplayerRoleFlags.Server:
                        startAutomatically = true;
                        break;
                    case MultiplayerRoleFlags.Client:
                        startAutomatically = ContainsString(CurrentPlayer.ReadOnlyTags(), k_AutoconnectPlayerTag);
                        break;
                }
#if UNITY_EDITOR
                startAutomatically |= AreTestsRunning;
#endif
                return startAutomatically;
            }
        }

        [SerializeField]
        ConnectionManager m_ConnectionManager;

        internal const int k_MinPlayers = 2;
        internal const int k_MaxPlayers = 10;

        // this is the Tag created for a Virtual Player
        const string k_AutoconnectPlayerTag = "Autoconnect";
        const string k_MetagameSceneName = "MetagameScene";
        const string k_GameSceneName = "GameScene01";

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            if (Instance is null)
            {
                Instance = this;
            }
            m_ConnectionManager.EventManager.AddListener<ConnectionEvent>(OnConnectionEvent);
        }

        void OnDestroy()
        {
            Instance = null;
            m_ConnectionManager.EventManager.RemoveListener<ConnectionEvent>(OnConnectionEvent);
        }

        [RuntimeInitializeOnLoadMethod]
        static void OnApplicationStarted()
        {
            if (!Instance) //this happens during PlayMode tests
            {
                return;
            }
            Instance.InitializeNetworkLogic(); //note: this is the entry point for all autoconnected instances (including standalone servers)
        }

        /// <summary>
        /// Initializes the application's network-related behaviour according to the configuration. Servers load the main
        /// game scene and automatically start. Clients load the metagame scene and, if autoconnect is set to true, attempt
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

                    // Multiplayer Play Mode Scenarios are as follows:
                    // * EditorAsClient (one Virtual Player with Server role, Editor and rest of Virtual Players autoconnect)
                    // * EditorAsServer (editor is Server role, rest of Virtual Players with Client role autoconnect)
                    // * DeployToUGS (deploys server to a separate fleet in UGS, Virtual Player clients can connect to the allocated server's IP & port)
                    // * Live (Virtual Players clients can connect to the last deployed server)
#if UNITY_SERVER && !UNITY_EDITOR
                    m_ConnectionManager.StartServerMatchmaker(k_MaxPlayers);
#elif UNITY_EDITOR
                    m_ConnectionManager.StartServerIP(k_DefaultServerListenAddress, listeningPort);
                    #endif
                    break;
                case MultiplayerRoleFlags.Client:
                {
                    if (AutoConnectOnStartup)
                    {
                        m_ConnectionManager.StartClientIP(k_DefaultClientAutoConnectServerAddress, listeningPort);
                    }
                    else
                    {
                        SceneManager.LoadScene(k_MetagameSceneName);
                    }
                    break;
                }
                case MultiplayerRoleFlags.ClientAndServer:
                    throw new ArgumentOutOfRangeException("MultiplayerRole", "ClientAndServer is an invalid multiplayer role in this sample. Please select the Client or Server role.");
            }
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
                    case ConnectStatus.Success:
                        // If server successfully starts, load game scene
                        NetworkManager.Singleton.SceneManager.LoadScene(k_GameSceneName, LoadSceneMode.Single);
                        break;
                }
            }
            else
            {
                switch (evt.status)
                {
                    case ConnectStatus.GenericDisconnect:
                    case ConnectStatus.UserRequestedDisconnect:
                    case ConnectStatus.ServerEndedSession:
                        if (!AutoConnectOnStartup)
                        {
                            // If client is disconnected, return to metagame scene
                            SceneManager.LoadScene(k_MetagameSceneName);
                        }
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

        static bool ContainsString(string[] source, string value, bool ignoreCase = false)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return Array.Exists(source, str =>
                ignoreCase
                    ? string.Equals(str, value, StringComparison.OrdinalIgnoreCase)
                    : str == value
            );
        }
    }
}
