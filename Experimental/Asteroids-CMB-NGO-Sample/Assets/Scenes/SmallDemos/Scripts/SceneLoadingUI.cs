using System.Collections.Generic;
using Unity.Netcode;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneLoadingUI : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    private List<string> m_SceneNames = new List<string>();

    [HideInInspector]
    [SerializeField]
    private string m_TransitionToScene;

#if UNITY_EDITOR
    public SceneAsset EndSessionScene;
    public SceneAsset TransistionToScene;
    public List<SceneAsset> ScenesToLoad;
    private void OnValidate()
    {
        m_SceneNames.Clear();
        foreach (var scene in ScenesToLoad)
        {
            m_SceneNames.Add(scene.name);
        }
        if (TransistionToScene != null)
        {
            m_TransitionToScene = TransistionToScene.name;
        }
    }
#endif

    private Dictionary<string, Scene> m_ScenesLoaded = new Dictionary<string, Scene>();


    private void Start()
    {
        // Assure we don't double register for the following event notifications:
        // OnShuttingDown is invoked just prior to NetworkManager.Shutdown being invoked (i.e. when you hit the "X" button in top left corner)
        NetworkManagerHelper.Instance.OnShuttingDown -= OnShuttingDownNetworkManager;
        NetworkManagerHelper.Instance.OnShuttingDown += OnShuttingDownNetworkManager;
        // We subscribe to the new session owner promotion notification in order to assure the new session owner
        // has updated everything to be used to generate the session owner UI overlay
        NetworkManager.Singleton.OnSessionOwnerPromoted -= OnSessionOwnerPromoted;
        NetworkManager.Singleton.OnSessionOwnerPromoted += OnSessionOwnerPromoted;

        // If we are already listening (i.e. could be a full scene transition/LoadSceneMode.Single)
        if (NetworkManager.Singleton.IsListening)
        {
            // Re-register for all needed NetworkSceneManager callbacks
            AddSceneManagerCallbacks();
            // Re-register for when the client is stopped
            NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
            NetworkManager.Singleton.OnClientStopped += OnClientStopped;
        }
        else // otherwise...
        {
            // Register for when the client is started
            NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
            NetworkManager.Singleton.OnClientStarted += OnClientStarted;
        }
    }

    private void OnSessionOwnerPromoted(ulong sessionOwnerPromotedId)
    {
        // If we are the new session owner, re-register for NetworkSceneManager scene event notifications
        // (this also assures we keep the client synchronization mode in LoadSceneMode.Additive)
        if (NetworkManager.Singleton.LocalClientId == sessionOwnerPromotedId)
        {
            AddSceneManagerCallbacks();
        }
    }

    /// <summary>
    /// Handles clean up just prior to shutting down the NetworkManager
    /// </summary>
    private void OnShuttingDownNetworkManager()
    {
        // Just prior to shutting down the NetworkManager, remove all event notification callback subscriptions
        RemoveAllCallbacks();

        // Subscribe for the client started notification (it is already removed in RemoveAllCallbacks)
        NetworkManager.Singleton.OnClientStarted += OnClientStarted;
    }

    /// <summary>
    /// Handles resynchronizing our local scenes loaded list for the session owner UI overlay
    /// </summary>
    private void UpdateSynchronizedScenes()
    {
        var scenesLoaded = NetworkManager.Singleton.SceneManager.GetSynchronizedScenes();
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
    /// Subscribes to NetworkSceneManager notifications, assures clients will unload any unused scenes after
    /// synchronizing, and if the session owner assures the client synchronization mode is LoadSceneMode.Additive
    /// </summary>
    private void AddSceneManagerCallbacks()
    {
        if (NetworkManager.Singleton.LocalClient.IsSessionOwner)
        {
            NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);
        }
        NetworkManager.Singleton.SceneManager.PostSynchronizationSceneUnloading = true;
        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= OnSynchronizeComplete;
        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += OnSynchronizeComplete;
        NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
    }

    /// <summary>
    /// Once a client is fully synchronized, we want to know when it will be stopped and
    /// we can unsubscribe to the SynchronizeComplete event.
    /// </summary>
    /// <param name="clientId"></param>
    private void OnSynchronizeComplete(ulong clientId)
    {
        // In distributed authority, you need to check if the notification is for the local client before
        // applying any relted notification relative logic
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
            NetworkManager.Singleton.OnClientStopped += OnClientStopped;
            NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= OnSynchronizeComplete;
            UpdateSynchronizedScenes();
        }
    }

    /// <summary>
    /// When the client is started we want to unsubscribe from the OnClientStarted notification,
    /// subscribe to the OnClientStopped notification (also making sure we aren't double subscribing),
    /// and then set up notifications and assure the NetworkSceneManager will unload any unused scenes
    /// that were not used during client synchronization.
    /// </summary>
    private void OnClientStarted()
    {
        NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
        NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;

        AddSceneManagerCallbacks();
    }

    /// <summary>
    /// Notification handler for in-session scene events
    /// </summary>
    /// <param name="sceneEvent"></param>
    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.Load:
                {
                    // If we are doing a full scene transition, then remove the callbacks from this SceneLoadingUI component instance.
                    if (sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId && sceneEvent.LoadSceneMode == LoadSceneMode.Single && !NetworkManager.Singleton.LocalClient.IsSessionOwner)
                    {
                        RemoveAllCallbacks();
                    }
                    break;
                }
            case SceneEventType.LoadComplete:
                {
                    // If this LoadComplete event is for the local client, then add the newly loaded scene
                    // to the m_ScenesLoaded.
                    if (sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId)
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
                    if (sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId)
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
    /// Upon stopping the client, wait for the client started notification
    /// </summary>
    /// <param name="wasHost"></param>
    private void OnClientStopped(bool wasHost)
    {
        NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
        NetworkManager.Singleton.OnClientStarted += OnClientStarted;
    }

    /// <summary>
    /// Removes all callbacks and clears out the scenes loaded list
    /// </summary>
    private void RemoveAllCallbacks()
    {
        m_ScenesLoaded.Clear();
        if (!NetworkManager.Singleton)
        {
            return;
        }
        NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        if (NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
            NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= OnSynchronizeComplete;
        }
        NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
    }

    /// <summary>
    /// Draws the session owner UI overlay
    /// </summary>
    private void OnGUI()
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsConnectedClient) return;

        GUILayout.BeginArea(new Rect(Screen.width - 200, 50, 190, 400));
        var sessionOwner = NetworkManager.Singleton.LocalClient.IsSessionOwner;

        if (sessionOwner)
        {
            if (GUILayout.Button($"[SingleMode] {m_TransitionToScene}"))
            {
                RemoveAllCallbacks();
                NetworkManager.Singleton.SceneManager.LoadScene(m_TransitionToScene, LoadSceneMode.Single);
            }
        }

        foreach (var sceneName in m_SceneNames)
        {
            if (m_ScenesLoaded.ContainsKey(sceneName))
            {
                if (sessionOwner)
                {
                    if (GUILayout.Button($"[Unload] {sceneName}"))
                    {
                        NetworkManager.Singleton.SceneManager.UnloadScene(m_ScenesLoaded[sceneName]);
                    }
                }

                if (NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.scene.name != sceneName)
                {
                    if (GUILayout.Button($"[Player Migrate] {sceneName}"))
                    {
                        SceneManager.MoveGameObjectToScene(NetworkManager.Singleton.LocalClient.PlayerObject.gameObject, m_ScenesLoaded[sceneName]);
                    }
                }
            }
            else if (sessionOwner)
            {
                if (GUILayout.Button($"[Load] {sceneName}"))
                {
                    NetworkManager.Singleton.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
                }
            }
        }
        if (NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            if (SceneManager.GetActiveScene().name != NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.scene.name)
            {
                if (GUILayout.Button($"[Player Migrate] {SceneManager.GetActiveScene().name}"))
                {
                    SceneManager.MoveGameObjectToScene(NetworkManager.Singleton.LocalClient.PlayerObject.gameObject, SceneManager.GetActiveScene());
                }
            }
        }
        GUILayout.EndArea();
    }
}
