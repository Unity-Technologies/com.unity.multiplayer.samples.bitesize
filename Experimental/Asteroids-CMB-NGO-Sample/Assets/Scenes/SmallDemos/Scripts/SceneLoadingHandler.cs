using System;
using System.Collections.Generic;
using Unity.Netcode;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneLoadingHandler : MonoBehaviour
{
    public List<SceneGroup> SceneGroups;

#if UNITY_EDITOR
    private void OnValidate()
    {
        foreach (var sceneGroup in SceneGroups)
        {
            sceneGroup.OnValidateProcess();
        }
    }
#endif

    private Dictionary<string, Scene> m_ScenesLoaded = new Dictionary<string, Scene>();
    private NetworkManager m_NetworkManager;
    private NetworkManagerHelper m_NetworkManagerHelper;
    private int m_CurrentSceneGroup;

    private void Awake()
    {
        m_NetworkManager = GetComponent<NetworkManager>();
        m_NetworkManagerHelper = GetComponent<NetworkManagerHelper>();
    }

    private void Start()
    {
        // OnShuttingDown is invoked just prior to NetworkManager.Shutdown being invoked (i.e. when you hit the "X" button in top left corner)
        m_NetworkManagerHelper.OnShuttingDown += OnShuttingDownNetworkManager;
        // Register for when the client is started
        m_NetworkManager.OnClientStarted += OnClientStarted;
        m_NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;

        
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (clientId == m_NetworkManager.LocalClientId)
        {
            SetSceneGroupIndex();
            UpdateSynchronizedScenes();
        }
    }

    /// <summary>
    /// Handles clean up just prior to shutting down the NetworkManager
    /// </summary>
    private void OnShuttingDownNetworkManager()
    {
        SceneManager.activeSceneChanged -= ActiveSceneChanged;
        m_NetworkManager.SceneManager.OnSynchronizeComplete -= OnSynchronizeComplete;
        m_NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
        m_NetworkManager.OnClientStopped += OnClientStopped;
    }

    private void OnClientStopped(bool obj)
    {
        m_NetworkManager.OnClientStopped -= OnClientStopped;
        var sceneGroup = GetSceneGroup(0);
        if (sceneGroup != null) 
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != sceneGroup.ActiveScene)
            {
                Camera.main.transform.SetParent(m_NetworkManager.transform, false);
                m_CurrentSceneGroup = 0;
                SceneManager.LoadScene(sceneGroup.ActiveScene, LoadSceneMode.Single);
            }
        }
    }

    /// <summary>
    /// Checks for a change in the current scene group
    /// </summary>
    private void ActiveSceneChanged(Scene arg0, Scene arg1)
    {
        // When the active scene changes, we check to see if we transitioned 
        // to a new scene group
        SetSceneGroupIndex();
    }

    /// <summary>
    /// When the client is started we subscribe to the NetworkSceneManager events and configure
    /// the client synchronization mode settings.
    /// </summary>
    private void OnClientStarted()
    {
        SceneManager.activeSceneChanged += ActiveSceneChanged;
        m_NetworkManager.SceneManager.OnSynchronizeComplete += OnSynchronizeComplete;
        m_NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
        m_NetworkManager.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);
        m_NetworkManager.SceneManager.PostSynchronizationSceneUnloading = true;
    }

    /// <summary>
    /// Once a client is fully synchronized, we want to know when it will be stopped and
    /// we can unsubscribe to the SynchronizeComplete event.
    /// </summary>
    /// <param name="clientId"></param>
    private void OnSynchronizeComplete(ulong clientId)
    {
        // In distributed authority, you need to check if the notification is for the local client before
        // applying any relted notification relative logic since notifications are broadcasted
        if (m_NetworkManager.LocalClientId == clientId)
        {
            UpdateSynchronizedScenes();
        }
    }

    /// <summary>
    /// Handles resynchronizing our local scenes loaded list for the session owner UI overlay
    /// </summary>
    private void UpdateSynchronizedScenes()
    {
        var scenesLoaded = m_NetworkManager.SceneManager.GetSynchronizedScenes();
        m_ScenesLoaded.Clear();
        foreach (var scene in scenesLoaded)
        {
            if (!m_ScenesLoaded.ContainsKey(scene.name))
            {
                m_ScenesLoaded.Add(scene.name, scene);
            }
        }
    }

    /// <summary>
    /// Gets the scene group based on the index passed in. It will roll over 
    /// if the index is beyond the maximum number of entries
    /// </summary>
    private SceneGroup GetSceneGroup(int sceneGroupIndex)
    {
        return SceneGroups[sceneGroupIndex % SceneGroups.Count];
    }

    /// <summary>
    /// Sets the current scene group index
    /// </summary>
    private void SetSceneGroupIndex()
    {
        var activeScene = SceneManager.GetActiveScene();
        for(int i = 0; i < SceneGroups.Count; i++)
        {
            var sceneGroup = SceneGroups[i];
            if (sceneGroup.ActiveScene == activeScene.name)
            {
                if (m_CurrentSceneGroup != i)
                {
                    UpdateSynchronizedScenes();
                }
                m_CurrentSceneGroup = i;
                break;
            }
        }
    }

    /// <summary>
    /// Notification handler for in-session scene events
    /// </summary>
    /// <param name="sceneEvent"></param>
    private void OnSceneEvent(SceneEvent sceneEvent)
    {
#if UNITY_EDITOR
        var sceneName = string.Empty;
        if (sceneEvent.SceneEventType != SceneEventType.Synchronize && sceneEvent.SceneEventType != SceneEventType.SynchronizeComplete)
        {
            sceneName = sceneEvent.SceneName;
        }
        Debug.Log($"[{name}][{sceneEvent.SceneEventType}][Target Client-{sceneEvent.ClientId}][{sceneName}]");
#endif
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.LoadComplete:
                {
                    // If this LoadComplete event is for the local client, then add the newly loaded scene
                    // to the m_ScenesLoaded.
                    if (sceneEvent.ClientId == m_NetworkManager.LocalClientId)
                    {
                        if (!m_ScenesLoaded.ContainsKey(sceneEvent.SceneName))
                        {
                            m_ScenesLoaded.Add(sceneEvent.SceneName, sceneEvent.Scene);
                        }
                    }
                    break;
                }
            case SceneEventType.UnloadComplete:
                {
                    // If this LoadComplete event is for the local client, then remove the unloaded scene
                    // from the m_ScenesLoaded.
                    if (sceneEvent.ClientId == m_NetworkManager.LocalClientId)
                    {
                        if (m_ScenesLoaded.ContainsKey(sceneEvent.SceneName))
                        {
                            m_ScenesLoaded.Remove(sceneEvent.SceneName);
                        }
                    }
                    break;
                }
        }
    }

    /// <summary>
    /// Draws the session owner UI overlay
    /// </summary>
    private void OnGUI()
    {
        if (!m_NetworkManager || !m_NetworkManager.IsConnectedClient) return;

        GUILayout.BeginArea(new Rect(Screen.width - 200, 50, 190, 400));
        var sessionOwner = m_NetworkManager.LocalClient.IsSessionOwner;
        var currentSceneGroup = GetSceneGroup(m_CurrentSceneGroup);

        if (sessionOwner)
        {
            var nextSceneGroup = GetSceneGroup(m_CurrentSceneGroup + 1);
            if (GUILayout.Button($"[SingleMode] {nextSceneGroup.ActiveScene}"))
            {
                var status = m_NetworkManager.SceneManager.LoadScene(nextSceneGroup.ActiveScene, LoadSceneMode.Single);
                if (status != SceneEventProgressStatus.Started)
                {
                    m_NetworkManagerHelper.LogMessage($"Failed to load scene {nextSceneGroup.ActiveScene} due to: {status}");
                }
            }
        }

        foreach (var sceneName in currentSceneGroup.AdditiveScenes)
        {
            if (m_ScenesLoaded.ContainsKey(sceneName))
            {
                if (sessionOwner)
                {
                    if (GUILayout.Button($"[Unload] {sceneName}"))
                    {
                        m_NetworkManager.SceneManager.UnloadScene(m_ScenesLoaded[sceneName]);
                    }
                }

                if (m_NetworkManager.LocalClient.PlayerObject.gameObject.scene.name != sceneName)
                {
                    if (GUILayout.Button($"[Player Migrate] {sceneName}"))
                    {
                        SceneManager.MoveGameObjectToScene(m_NetworkManager.LocalClient.PlayerObject.gameObject, m_ScenesLoaded[sceneName]);
                    }
                }
            }
            else if (sessionOwner)
            {
                if (GUILayout.Button($"[Load] {sceneName}"))
                {
                    m_NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                }
            }
        }
        if (m_NetworkManager.LocalClient.PlayerObject != null)
        {
            if (SceneManager.GetActiveScene().name != m_NetworkManager.LocalClient.PlayerObject.gameObject.scene.name)
            {
                if (GUILayout.Button($"[Player Migrate] {SceneManager.GetActiveScene().name}"))
                {
                    SceneManager.MoveGameObjectToScene(m_NetworkManager.LocalClient.PlayerObject.gameObject, SceneManager.GetActiveScene());
                }
            }
        }
        GUILayout.EndArea();
    }
}

[Serializable]
public class SceneGroup
{
    [HideInInspector]
    public string ActiveScene;

    [HideInInspector]
    public List<string> AdditiveScenes = new List<string>();

#if UNITY_EDITOR    
    public SceneAsset ActiveSceneToLoad;
    public List<SceneAsset> ScenesToLoad;
    public void OnValidateProcess()
    {
        if (ActiveSceneToLoad != null)
        {
            ActiveScene = ActiveSceneToLoad.name;
        }
        AdditiveScenes?.Clear();
        foreach (var scene in ScenesToLoad)
        {
            if (scene == null) continue;
            AdditiveScenes.Add(scene.name);
        }
    }
#endif
}