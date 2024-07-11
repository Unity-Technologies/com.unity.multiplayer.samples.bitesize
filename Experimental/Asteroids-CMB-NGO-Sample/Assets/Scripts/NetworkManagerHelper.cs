using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;

#if MULTIPLAYER_TOOLS
using Unity.Multiplayer.Tools.NetStats;
using Unity.Multiplayer.Tools.NetStatsMonitor;
#endif

using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Random = System.Random;
using Unity.Netcode.Components;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NetworkManagerHelper : MonoBehaviour, IPoolSystemTracker
{
    public static NetworkManagerHelper Instance;

#if MULTIPLAYER_TOOLS
    [Tooltip("Assign an in-scene placed net stats monitor prefab here for it to be available to other components.")]
    public RuntimeNetStatsMonitor NetStatsMonitor;
#endif

    [Range(0.10f, 1.0f)]
    public float ResolutionFactor = 0.5f;
    public bool ConsoleLogVisible;
    public bool CanStartGame;
    public bool ByPassPoolLoading = true;
    [Range(0.1f, 10.0f)]
    public float GravityMultiplier = 3.0f;
    private float m_CurrentGravityMultiplier = 0.0f;

    [SerializeField]
    private bool m_UseAlternateBackground;

    public Color AlternateBackground;


    [Serializable]
    public struct DistributedAuthoritySettings
    {
        [Tooltip("When enabled. distributed authority mode is active. If this is the only setting enabled, starting as a DA-Host will make the host mock the CMB services.")]
        public bool Enabled;
        public bool UseRelay;

        [Tooltip("When enabled, clients will only connect to the CMB Services server. You can only start a NetworkManager as a client in this mode.")]
        public bool UseService;
        [Tooltip("When enabled, clients will only connect to the CMB Services server hosted on GSH. You can only start a NetworkManager as a client in this mode.")]
        public bool UseLiveService;



        public bool UsingService()
        {
            return Enabled && UseService;
        }

        public bool UsingLiveService()
        {
            return UsingService() && UseLiveService;
        }
    }

    [SerializeField]
    private DistributedAuthoritySettings m_DistributedAuthoritySettings;
    private DistributedAuthoritySettings m_OriginalSettings;

    [HideInInspector]
    [SerializeField]
    private NetworkTransport m_Transport;

    [HideInInspector]
    [SerializeField]
    private string ExitGameSceneName;

    [HideInInspector]
    [SerializeField]
    private string m_ClientButtonLabel = "Client";

    [HideInInspector]
    [SerializeField]
    private string m_HostButtonLabel = "Host";

    [HideInInspector]
    [SerializeField]
    private NetworkManager m_NetworkManager;

    public RenderPipelineAsset DefaultRenderPipelineAsset;
    public RenderPipelineAsset MacRenderPipelineAsset;

    public bool IsRunningOSX { get; private set; }

    private string RenderingPath = "Forward+";

#if UNITY_EDITOR
    public SceneAsset ExitGameScene;
    private void OnValidate()
    {
        if (ExitGameScene != null)
        {
            ExitGameSceneName = ExitGameScene.name;
        }
        m_NetworkManager = GetComponent<NetworkManager>();
        m_Transport = m_NetworkManager.NetworkConfig.NetworkTransport;
        UpdateSessionMode();
    }
#endif

    private void UpdateSessionMode()
    {
        m_NetworkManager.NetworkConfig.NetworkTopology = m_DistributedAuthoritySettings.Enabled ? NetworkTopologyTypes.DistributedAuthority : NetworkTopologyTypes.ClientServer;
        m_NetworkManager.NetworkConfig.UseCMBService = m_DistributedAuthoritySettings.Enabled ? m_DistributedAuthoritySettings.UseService : false;
        m_HostButtonLabel = m_DistributedAuthoritySettings.Enabled ? "DA-Host" : "Host";
        m_ClientButtonLabel = m_DistributedAuthoritySettings.Enabled ? "DA-Client" : "Client";
    }

    public ProgressFill ProgressBar;



    private void Awake()
    {
        m_OriginalSettings = m_DistributedAuthoritySettings;
        if (!IsRunningOSX)
        {
            IsRunningOSX = SystemInfo.operatingSystem.Contains("Mac OS X");
        }
        // Switch to the deferred rendering path for MAC to avoid M1 URP issue
        if (IsRunningOSX)
        {
            GraphicsSettings.defaultRenderPipeline = MacRenderPipelineAsset;
            QualitySettings.renderPipeline = MacRenderPipelineAsset;
            RenderingPath = "Deferred";
        }
        else
        {
            GraphicsSettings.defaultRenderPipeline = DefaultRenderPipelineAsset;
            QualitySettings.renderPipeline = DefaultRenderPipelineAsset;
            RenderingPath = "Forward+";
        }

        Screen.SetResolution((int)(Screen.currentResolution.width * ResolutionFactor), (int)(Screen.currentResolution.height * ResolutionFactor), FullScreenMode.Windowed);

        m_NetworkManager.SetSingleton();

        ObjectPoolSystem.PoolSystemTrackerRegistration(this);
    }

    private async void Start()
    {
        Instance = this;
        //var initoptions = new InitializationOptions();
        //initoptions.SetProfile(GetRandomString(5));
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignInFailed += SignInFailed;
            AuthenticationService.Instance.SignedIn += SignedIn;
            AuthenticationService.Instance.SwitchProfile(GetRandomString(5));
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        //else
        //{
        //    // When signing out is no longer broken, we can shift this down to when the NetworkManager
        //    // stops.
        //    AuthenticationService.Instance.SignInFailed -= SignInFailed;
        //    AuthenticationService.Instance.SignedIn -= SignedIn;
        //    AuthenticationService.Instance.SignedOut += SignedOutOfServices;
        //    try
        //    {
        //        AuthenticationService.Instance.SignOut();
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.LogException(ex);
        //    }
        //}

#if MULTIPLAYER_TOOLS
        if (NetStatsMonitor != null)
        {
            NetStatsMonitor.gameObject.SetActive(false);
        }
#endif
    }

    public void ToggleNetStatsMonitor(bool disable = false)
    {
#if MULTIPLAYER_TOOLS
        if (NetStatsMonitor)
        {
            if (!m_NetworkManager.NetworkConfig.NetworkMessageMetrics)
            {
                var checkForBuiltIn = false;
                var defaultMetrics = Enum.GetNames(typeof(Unity.Multiplayer.Tools.MetricTypes.DirectedMetricType));
                foreach (var elementConfig in NetStatsMonitor.Configuration.DisplayElements)
                {
                    foreach (var stats in elementConfig.Stats)
                    {
                        if (defaultMetrics.Contains(stats.ToString()))
                        {
                            checkForBuiltIn = true;
                            break;
                        }
                    }
                    if (checkForBuiltIn)
                    {
                        break;
                    }
                }

                // Log a warning if the messaging metrics is disabled
                if (checkForBuiltIn)
                {
                    Debug.LogWarning($"{nameof(NetworkManager)}'s {nameof(NetworkConfig.NetworkMessageMetrics)} property is not enabled. The default Built-In Metrics use {nameof(NetworkConfig.NetworkMessageMetrics)} and {nameof(NetStatsMonitor)} not be populates with values!");
                }
            }
            if (disable)
            {
                NetStatsMonitor.gameObject.SetActive(false);
            }
            else
            {
                NetStatsMonitor.gameObject.SetActive(!NetStatsMonitor.gameObject.activeInHierarchy);
            }

        }
#else
        Debug.LogWarning($"The multiplayer tools package is not installed!");
#endif
    }

    private void SignedIn()
    {
        AuthenticationService.Instance.SignedIn -= SignedIn;
        Debug.Log("Signed in anonymously");
    }

    private void SignInFailed(RequestFailedException error)
    {
        AuthenticationService.Instance.SignInFailed -= SignInFailed;
        Debug.LogError($"Failed to sign in anonymously: {error}");
    }

    private async void SignedOutOfServices()
    {
        AuthenticationService.Instance.SignedOut -= SignedOutOfServices;
        //AuthenticationService.Instance.SwitchProfile(GetRandomString(5));
        AuthenticationService.Instance.SignInFailed += SignInFailed;
        AuthenticationService.Instance.SignedIn += SignedIn;

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    static string GetRandomString(int length)
    {
        var r = new Random();
        return new String(Enumerable.Range(0, length).Select(_ => (Char)r.Next('a', 'z')).ToArray());
    }

    public enum StartModes
    {
        Client,
        Host,
        DAClient
    }

    public Action<StartModes> OnStarted;

    public Action OnExiting;

    private float[] m_DeltaTimes = new float[60];
    private int[] m_DeltaTicks = new int[60];
    private int m_DeltaTickIndex;
    private int m_DeltaTimeIndex;

    private int m_DeltaTick;
    private float m_DeltaTime;
    private float m_DeltaDivide = 1.0f / 60.0f;

    private string m_SessionName = string.Empty;
    private Task m_SessionTask;

    private bool m_PooledObjectsLoaded;

    private Dictionary<ObjectPoolSystem, float> m_PoolSystemsLoading = new Dictionary<ObjectPoolSystem, float>();
    private int m_CurrentOwnedObjectCount;
    private float m_LastObjectCountUpdate;
    private Vector3 StandardGravity = new Vector3(0.0f, -9.81f, 0.0f);


    private ISession m_LastSession;
    public string GetSessionName()
    {
        return m_SessionName == string.Empty ? "LocalService" : m_SessionName;
    }

    private void AverageDeltaTime()
    {
        m_DeltaTimes[m_DeltaTimeIndex] = Time.deltaTime;
        m_DeltaTime = 0.0f;
        foreach (var deltaTime in m_DeltaTimes)
        {
            m_DeltaTime += deltaTime;
        }
        m_DeltaTime *= m_DeltaDivide;
        m_DeltaTime *= 1000.0f;
        m_DeltaTimeIndex++;
        m_DeltaTimeIndex %= m_DeltaTimes.Length;

        m_DeltaTick = 0;
        m_DeltaTicks[m_DeltaTickIndex] = m_NetworkManager.LocalTime.Tick - m_NetworkManager.ServerTime.Tick;
        foreach (var deltaTick in m_DeltaTicks)
        {
            m_DeltaTick += deltaTick;
        }

        m_DeltaTick = (int)(m_DeltaTick * m_DeltaDivide);
        m_DeltaTickIndex++;
        m_DeltaTickIndex %= m_DeltaTicks.Length;
    }

    /// <summary>
    /// Used by ObjectPoolSystem to visually track the progress of instantiating its object pool
    /// </summary>
    public void TrackPoolSystemLoading(ObjectPoolSystem poolSystem, float progress, bool isLoading = true)
    {
        if (isLoading)
        {
            if (!m_PoolSystemsLoading.ContainsKey(poolSystem))
            {
                m_PoolSystemsLoading.Add(poolSystem, progress);
            }
            else
            {
                m_PoolSystemsLoading[poolSystem] = progress;
            }
        }
        else
        {
            m_PoolSystemsLoading.Remove(poolSystem);
        }
    }

    private void UpdateProgress()
    {
        if (ByPassPoolLoading)
        {
            return;
        }

        var totalProgress = 0.0f;
        foreach (var poolSystem in m_PoolSystemsLoading)
        {
            totalProgress += poolSystem.Value;
        }
        totalProgress = totalProgress / m_PoolSystemsLoading.Count;
        ProgressBar.UpdateProgress(totalProgress);
        if (totalProgress >= 1.0f)
        {
            m_PoolSystemsLoading.Clear();
            m_PooledObjectsLoaded = true;
        }
    }

    private enum SessionTypes
    {
        None,
        DAHost,
        DAHostRelay,
        ServiceLocal,
        ServiceLive,
    }
    private SessionTypes m_SessionType;
    private bool m_SelectedSessionType;
    private Vector2 m_ScrollViewDropDown = Vector2.zero;
    private bool m_ShowDropDownOptions = false;
    private void DrawDropDown()
    {
        var typeNames = Enum.GetNames(typeof(SessionTypes));

        if (!m_ShowDropDownOptions)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Network Topology:", GUILayout.Width(150)))
            {
                m_ShowDropDownOptions = true;
            }
            GUILayout.Label($"<b>{m_SessionType}</b>");
            GUILayout.EndHorizontal();
        }

        if (m_ShowDropDownOptions)
        {
            m_ScrollViewDropDown = GUILayout.BeginScrollView(m_ScrollViewDropDown);
            for (int index = 0; index < typeNames.Length; index++)
            {
                if (GUILayout.Button(typeNames[index]))
                {
                    m_ShowDropDownOptions = false;
                    m_SessionType = (SessionTypes)Enum.GetValues(typeof(SessionTypes)).GetValue(index);
                }
            }
            GUILayout.EndScrollView();
        }
    }

    private void OnGUI()
    {
        if (Camera.main != null)
        {
            if (m_UseAlternateBackground)
            {
                Camera.main.clearFlags = CameraClearFlags.Color;
                Camera.main.backgroundColor = AlternateBackground;
            }
            else
            {
                Camera.main.clearFlags = CameraClearFlags.Skybox;
            }
        }

        if (!CanStartGame || (!ByPassPoolLoading && !m_PooledObjectsLoaded))
        {
            return;
        }

        if (m_NetworkManager == null) { return; }

        if (!m_NetworkManager.IsClient && !m_NetworkManager.IsServer)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 800));

            if (!m_SelectedSessionType)
            {
                DrawDropDown();
                if (m_SessionType != SessionTypes.None && GUILayout.Button("Confirm Selection", GUILayout.Width(160)))
                {
                    m_DistributedAuthoritySettings.Enabled = true;
                    switch (m_SessionType)
                    {
                        case SessionTypes.DAHost:
                            {
                                m_DistributedAuthoritySettings.UseRelay = false;
                                m_DistributedAuthoritySettings.UseService = false;
                                m_DistributedAuthoritySettings.UseLiveService = false;
                                break;
                            }
                        case SessionTypes.DAHostRelay:
                            {
                                m_DistributedAuthoritySettings.UseRelay = true;
                                m_DistributedAuthoritySettings.UseService = false;
                                m_DistributedAuthoritySettings.UseLiveService = false;
                                break;
                            }
                        case SessionTypes.ServiceLocal:
                            {
                                m_DistributedAuthoritySettings.UseRelay = false;
                                m_DistributedAuthoritySettings.UseService = true;
                                m_DistributedAuthoritySettings.UseLiveService = false;
                                break;
                            }
                        case SessionTypes.ServiceLive:
                            {
                                m_DistributedAuthoritySettings.UseRelay = false;
                                m_DistributedAuthoritySettings.UseService = true;
                                m_DistributedAuthoritySettings.UseLiveService = true;
                                break;
                            }
                    }
                    UpdateSessionMode();
                    m_OriginalSettings.UseService = m_DistributedAuthoritySettings.UseService;
                    m_OriginalSettings.Enabled = m_DistributedAuthoritySettings.Enabled;
                    m_SelectedSessionType = true;
                }
            }
            else
            if (m_DistributedAuthoritySettings.UsingService())
            {
                if (m_DistributedAuthoritySettings.UsingLiveService())
                {
                    GUI.enabled = m_SessionTask == null || m_SessionTask.IsCompleted;

                    GUILayout.Label("Session Name", GUILayout.Width(100));
                    m_SessionName = GUILayout.TextField(m_SessionName);
                    if (GUILayout.Button("Connect"))
                    {
                        m_SessionTask = ConnectThroughLiveService(m_SessionName, true);
                    }

                    GUI.enabled = true;
                }
                else
                {
                    var unityTransport = m_NetworkManager.NetworkConfig.NetworkTransport as UnityTransport;
                    GUILayout.Label("IP Address:", GUILayout.Width(100));
                    unityTransport.ConnectionData.Address = GUILayout.TextField(unityTransport.ConnectionData.Address, GUILayout.Width(100));

                    GUILayout.Label("Port:", GUILayout.Width(100));
                    var portString = GUILayout.TextField(unityTransport.ConnectionData.Port.ToString(), GUILayout.Width(100));
                    ushort.TryParse(portString, out unityTransport.ConnectionData.Port);
                    
                    // CMB distributed authority services just "connects" with no host, client, or server option (all are clients)
                    if (GUILayout.Button("Connect"))
                    {
                        m_NetworkManager.StartClient();
                        m_NetworkManager.OnClientStopped += OnNetworkManagerStopped;
                        OnStarted?.Invoke(StartModes.DAClient);
                    }
                }
            }
            else
            {
                if (m_DistributedAuthoritySettings.UseRelay)
                {
                    GUILayout.Label("Join Code:", GUILayout.Width(100));
                    m_RelayJoinCode = GUILayout.TextField(m_RelayJoinCode);
                }

                if (GUILayout.Button(m_HostButtonLabel))
                {
                    m_NetworkManager.OnServerStarted += OnServerStarted;
                    m_NetworkManager.OnClientStarted += OnClientStarted;
                    if (m_DistributedAuthoritySettings.UseRelay)
                    {
                        StartHostWithRelay();
                    }
                    else
                    {
                        m_NetworkManager.StartHost();
                    }
                }

                if (GUILayout.Button(m_ClientButtonLabel))
                {
                    m_NetworkManager.OnClientStarted += OnClientStarted;
                    if (m_DistributedAuthoritySettings.UseRelay)
                    {
                        StartClientWithRelay();
                    }
                    else
                    {
                        m_NetworkManager.StartClient();
                    }
                }
            }

            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(10, Display.main.renderingHeight - 40, Display.main.renderingWidth - 10, 30));
            var scenesPreloaded = new System.Text.StringBuilder();
            scenesPreloaded.Append("Scenes Preloaded: ");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                scenesPreloaded.Append($"[{scene.name}]");
            }
            GUILayout.Label(scenesPreloaded.ToString());
            GUILayout.EndArea();
            // Exit to main menu
            GUILayout.BeginArea(new Rect(Display.main.renderingWidth - 100, 10, 80, 35));
            if (!m_SelectedSessionType && GUILayout.Button("Main Menu"))
            {
                LoadExitScene();
            }
            else if (m_SelectedSessionType && GUILayout.Button("X"))
            {
                m_SelectedSessionType = false;
                m_SessionType = SessionTypes.None;
            }

            GUILayout.EndArea();
        }
        else
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 800));
            var heading = $"NSM:{m_NetworkManager.NetworkConfig.EnableSceneManagement} Mode: {(m_NetworkManager.IsHost ? m_HostButtonLabel : m_ClientButtonLabel)}-{m_NetworkManager.LocalClientId}";
            var sessionOwner = m_NetworkManager.LocalClient.IsSessionOwner ? "[Session Owner]" : "";
            GUILayout.Label($"{heading}{sessionOwner}");
            if (m_DistributedAuthoritySettings.UseRelay)
            {
                GUILayout.Label($"Join Code: {m_RelayJoinCode}");
            }

            if (m_Transport != null)
            {
                GUILayout.Label($"{RenderingPath} | Frame Time: {(int)m_DeltaTime}ms |Latency: {m_Transport.GetCurrentRtt(NetworkManager.ServerClientId)}ms");
            }

            GUILayout.Label($"DeltaTick: {m_DeltaTick} TicksAgo: {NetworkTransform.GetTickLatency()}");

            var networkManager = m_NetworkManager;
            GUILayout.Label($"Spawned: {m_NetworkManager.SpawnManager.SpawnedObjectsList.Count} Owned: {GetOwnedObjectCount(ref networkManager)}");
            //var currentTimeSync = networkManager.NetworkTimeSystem.CurrentTimeSyncInfo;
            //GUILayout.Label($"[{currentTimeSync.TimeSyncs}] LRTT: {currentTimeSync.LastSyncedRttSec} LSST: {currentTimeSync.LastSyncedServerTimeSec} STO:{currentTimeSync.DesiredServerTimeOffset} LTO: {currentTimeSync.DesiredLocalTimeOffset}");

            if (ConsoleLogVisible && m_MessageLogs.Count > 0)
            {
                GUILayout.Label("-----------(Log)-----------");
                // Display any messages logged to screen
                foreach (var messageLog in m_MessageLogs)
                {
                    GUILayout.Label(messageLog.Message);
                }
                GUILayout.Label("---------------------------");
            }
            GUILayout.EndArea();
            // Exit
            GUILayout.BeginArea(new Rect(Display.main.renderingWidth - 40, 10, 30, 30));
            if (GUILayout.Button("X"))
            {
                m_NetworkManager.Shutdown();
                m_SelectedSessionType = false;
                m_SessionType = SessionTypes.None;
            }
            GUILayout.EndArea();
        }
    }

    private string m_RelayJoinCode;
    private Allocation m_Allocation;

    private async void StartHostWithRelay(int maxConnections = 15)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        m_Allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        var unityTransport = m_NetworkManager.NetworkConfig.NetworkTransport as UnityTransport;
        unityTransport.UseEncryption = true;
        var defaultEndPoint = (RelayServerEndpoint)null;
        foreach (var endPoint in m_Allocation.ServerEndpoints)
        {
            if (endPoint.Secure && endPoint.Network == RelayServerEndpoint.NetworkOptions.Udp)
            {
                defaultEndPoint = endPoint;
                break;
            }
        }
        m_RelayJoinCode = await RelayService.Instance.GetJoinCodeAsync(m_Allocation.AllocationId);
        unityTransport.SetRelayServerData(defaultEndPoint.Host, (ushort)defaultEndPoint.Port, m_Allocation.AllocationIdBytes, m_Allocation.Key, m_Allocation.ConnectionData, null, defaultEndPoint.Secure);
        m_NetworkManager.StartHost();
    }

    private async void StartClientWithRelay()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(m_RelayJoinCode);
        var defaultEndPoint = (RelayServerEndpoint)null;
        foreach (var endPoint in joinAllocation.ServerEndpoints)
        {
            if (endPoint.Secure && endPoint.Network == RelayServerEndpoint.NetworkOptions.Udp)
            {
                defaultEndPoint = endPoint;
                break;
            }
        }
        //Populate the joining data
        var unityTransport = m_NetworkManager.NetworkConfig.NetworkTransport as UnityTransport;
        unityTransport.UseEncryption = true;
        unityTransport.SetClientRelayData(defaultEndPoint.Host, (ushort)defaultEndPoint.Port, joinAllocation.AllocationIdBytes, joinAllocation.Key, joinAllocation.ConnectionData, joinAllocation.HostConnectionData, defaultEndPoint.Secure);
        m_NetworkManager.StartClient();
    }

    private void OnServerStarted()
    {
        m_NetworkManager.OnServerStarted -= OnServerStarted;
        m_NetworkManager.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);
        m_NetworkManager.SceneManager.PostSynchronizationSceneUnloading = true;
    }

    private void OnClientStarted()
    {
        m_NetworkManager.OnClientStarted -= OnClientStarted;
        m_NetworkManager.OnClientStopped += OnNetworkManagerStopped;
        OnStarted?.Invoke(m_NetworkManager.IsHost ? StartModes.Host : m_DistributedAuthoritySettings.Enabled ? StartModes.DAClient : StartModes.Client);
        m_NetworkManager.SceneManager.PostSynchronizationSceneUnloading = true;
    }

    private async Task ConnectThroughLiveService(string sessionName, bool distributedAuthority)
    {
        try
        {
            var options = new CreateSessionOptions(100)
            {
                Name = sessionName,
                MaxPlayers = 100,
            };

            if (m_LastSession == null)
            {
                m_LastSession = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options.WithDistributedConnection());
            }
            else
            {
                await MultiplayerService.Instance.JoinSessionByIdAsync(m_LastSession.Id);
            }

            m_NetworkManager.OnClientStopped += OnNetworkManagerStopped;
            OnStarted?.Invoke(distributedAuthority ? StartModes.DAClient : m_NetworkManager.IsHost ? StartModes.Host : StartModes.Client);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async void OnNetworkManagerStopped(bool obj)
    {
        m_NetworkManager.OnClientStopped -= OnNetworkManagerStopped;
        ObjectOwnerColor.Reset();
        PlayerColor.Reset();

        if (m_DistributedAuthoritySettings.UseRelay)
        {
            m_Allocation = null;
            m_RelayJoinCode = string.Empty;
        }
        if (m_LastSession != null)
        {
            await m_LastSession.LeaveAsync();
        }

        ToggleNetStatsMonitor(true);


        // When signing out is no longer broken, we can shift this down to when the NetworkManager
        // stops.
        //AuthenticationService.Instance.SignedOut += SignedOutOfServices;
        //try
        //{
        //    AuthenticationService.Instance.SignOut();
        //}
        //catch (Exception ex)
        //{
        //    Debug.LogException(ex);
        //}

        //
        //m_LastSession = null;
    }

    private void LoadExitScene()
    {
        if (m_LastSession != null)
        {
            m_LastSession = null;
        }
        OnExiting?.Invoke();
        if (!string.IsNullOrEmpty(ExitGameSceneName))
        {
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
            SceneManager.LoadScene(ExitGameSceneName, LoadSceneMode.Single);
        }
    }



    private int GetOwnedObjectCount(ref NetworkManager networkManager)
    {
        if (m_LastObjectCountUpdate < Time.realtimeSinceStartup)
        {
            m_CurrentOwnedObjectCount = networkManager.SpawnManager.GetClientOwnedObjects(networkManager.LocalClientId).Length;
            m_LastObjectCountUpdate = Time.realtimeSinceStartup + 1.0f;
        }

        return m_CurrentOwnedObjectCount;
    }




    /// <summary>
    /// Invoked when a network session is active
    /// </summary>
    private void ConnectedUpdate()
    {
        AverageDeltaTime();

        if (GravityMultiplier != m_CurrentGravityMultiplier)
        {
            m_CurrentGravityMultiplier = GravityMultiplier;
            Physics.gravity = StandardGravity * m_CurrentGravityMultiplier;
        }

        if (m_DistributedAuthoritySettings.Enabled != m_OriginalSettings.Enabled)
        {
            Debug.LogError($"Cannot change the operation mode of {nameof(NetworkManager)} while started!");
            m_DistributedAuthoritySettings.Enabled = m_OriginalSettings.Enabled;
        }
        if (m_DistributedAuthoritySettings.UseService != m_OriginalSettings.UseService)
        {
            Debug.LogError($"Cannot change the services type of {nameof(NetworkManager)} while started!");
            m_DistributedAuthoritySettings.UseService = m_OriginalSettings.UseService;
        }
    }

    /// <summary>
    /// Invoked when no network session is active
    /// </summary>
    private void DisconnectedUpdate()
    {
        if (m_PoolSystemsLoading.Count > 0)
        {
            UpdateProgress();
            return;
        }
    }

    /// <summary>
    /// Standard update
    /// </summary>
    private void Update()
    {
        if (m_NetworkManager && m_NetworkManager.IsListening && m_NetworkManager.IsConnectedClient)
        {
            ConnectedUpdate();
        }
        else
        {
            DisconnectedUpdate();
        }

        CleanMessageLog();
    }

    #region MESSAGE LOGGING
    private List<MessageLog> m_MessageLogs = new List<MessageLog>();

    /// <summary>
    /// Removes message log entries that have expired
    /// </summary>
    private void CleanMessageLog()
    {
        if (m_MessageLogs.Count == 0)
        {
            return;
        }

        for (int i = m_MessageLogs.Count - 1; i >= 0; i--)
        {
            if (m_MessageLogs[i].ExpirationTime < Time.realtimeSinceStartup)
            {
                m_MessageLogs.RemoveAt(i);
            }
        }
    }

    private class MessageLog
    {
        public string Message { get; private set; }
        public float ExpirationTime { get; private set; }

        public MessageLog(string msg, float timeToLive)
        {
            Message = msg;
            ExpirationTime = Time.realtimeSinceStartup + timeToLive;
        }
    }

    public void LogMessage(string msg, float timeToLive = 10.0f, bool forceMessage = false)
    {
        if (m_MessageLogs.Count > 0)
        {
            m_MessageLogs.Insert(0, new MessageLog(msg, timeToLive));
        }
        else
        {
            m_MessageLogs.Add(new MessageLog(msg, timeToLive));
        }
        if (Instance.ConsoleLogVisible || forceMessage)
        {
            Debug.Log(msg);
        }
    }
    #endregion

    private void OnDestroy()
    {
        ObjectPoolSystem.PoolSystemTrackerRegistration(this, false);
    }

    public NetworkManagerHelper()
    {
        Instance = this;
    }
}
