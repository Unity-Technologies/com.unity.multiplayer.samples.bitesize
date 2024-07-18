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

    [HideInInspector]
    [SerializeField]
    private string m_EndSessionScene;

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
        if (EndSessionScene != null)
        {
            m_EndSessionScene = EndSessionScene.name;
        }
    }
#endif

    private Dictionary<string, Scene> m_ScenesLoaded = new Dictionary<string, Scene>();


    private void Start()
    {
        NetworkManagerHelper.Instance.OnShuttingDown -= OnShuttingDownNetworkManager;
        NetworkManagerHelper.Instance.OnShuttingDown += OnShuttingDownNetworkManager;
        NetworkManager.Singleton.OnSessionOwnerPromoted -= OnSessionOwnerPromoted;
        NetworkManager.Singleton.OnSessionOwnerPromoted += OnSessionOwnerPromoted;
        if (NetworkManager.Singleton.IsListening)
        {
            AddSceneManagerCallbacks();
            NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
            NetworkManager.Singleton.OnClientStopped += OnClientStopped;
        }
        else
        {
            NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
            NetworkManager.Singleton.OnClientStarted += OnClientStarted;
        }
    }

    private void OnSessionOwnerPromoted(ulong sessionOwnerPromotedId)
    {
        if (NetworkManager.Singleton.LocalClientId == sessionOwnerPromotedId)
        {
            AddSceneManagerCallbacks();
        }
    }

    private void OnShuttingDownNetworkManager()
    {
        RemoveAllCallbacks();
        NetworkManager.Singleton.OnClientStarted += OnClientStarted;
    }

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

    private void OnSynchronizeComplete(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
            NetworkManager.Singleton.OnClientStopped += OnClientStopped;
            NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= OnSynchronizeComplete;
            UpdateSynchronizedScenes();
        }
    }

    private void OnClientStarted()
    {
        NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
        NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;

        AddSceneManagerCallbacks();
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.Load:
                {
                    if (sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId && sceneEvent.LoadSceneMode == LoadSceneMode.Single && !NetworkManager.Singleton.LocalClient.IsSessionOwner)
                    {
                        RemoveAllCallbacks();
                    }
                    break;
                }
            case SceneEventType.LoadComplete:
                {
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

    private void OnClientStopped(bool wasHost)
    {
        NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
        NetworkManager.Singleton.OnClientStarted += OnClientStarted;
    }

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
