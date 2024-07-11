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
        if (NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.IsConnectedClient)
            {
                NetworkManager.Singleton.OnClientStopped += OnClientStopped;
                NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            }
            else
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
            }
        }
        else
        {
            NetworkManager.Singleton.OnClientStarted += OnClientStarted;
        }
    }

    private void UpdateSynchronizedScenes()
    {
        var scenesLoaded = NetworkManager.Singleton.SceneManager.GetSynchronizedScenes();
        foreach (var scene in scenesLoaded)
        {
            if (!m_ScenesLoaded.ContainsKey(scene.name))
            {
                m_ScenesLoaded.Add(scene.name, scene);
            }
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
            UpdateSynchronizedScenes();
        }
    }

    private void OnClientStarted()
    {
        NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;

        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;


    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        switch (sceneEvent.SceneEventType)
        {
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
        NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        NetworkManager.Singleton.OnClientStarted += OnClientStarted;

    }

    private Vector2 m_ScrollPosition = Vector2.zero;

    private void OnGUI()
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsConnectedClient) return;

        GUILayout.BeginArea(new Rect(Screen.width - 200, 50, 190, 400));
        var sessionOwner = NetworkManager.Singleton.LocalClient.IsSessionOwner;

        if (sessionOwner)
        {
            if (GUILayout.Button($"[SingleMode] {m_TransitionToScene}"))
            {
                NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
                NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
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

        if (NetworkManager.Singleton.DAHost)
        {
            if (NetworkManager.Singleton.ConnectedClientsIds.Count > 1 || !NetworkManager.Singleton.LocalClient.IsSessionOwner)
            {
                GUILayout.Label("Promote To Session Owner:");
                m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition, GUILayout.Width(140), GUILayout.Height(100));
                foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    if (clientId == NetworkManager.Singleton.CurrentSessionOwner)
                    {
                        continue;
                    }
                }
                GUILayout.EndScrollView();
            }
        }
        GUILayout.EndArea();
    }
}
