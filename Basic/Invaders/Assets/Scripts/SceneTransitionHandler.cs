using UnityEngine;
using UnityEngine.SceneManagement;
using MLAPI;
using MLAPI.SceneManagement;

public class SceneTransitionHandler : NetworkBehaviour
{
    static public SceneTransitionHandler sceneTransitionHandler { get; internal set; }

    [SerializeField]
    public string DefaultMainMenu = "StartMenu";

    [HideInInspector]
    public delegate void ClientLoadedSceneDelegateHandler(ulong clientId);
    [HideInInspector]
    public event ClientLoadedSceneDelegateHandler OnClientLoadedScene;

    [HideInInspector]
    public delegate void SceneStateChangedDelegateHandler(SceneStates newState);
    [HideInInspector]
    public event SceneStateChangedDelegateHandler OnSceneStateChanged;

    private SceneSwitchProgress m_SceneProgress;

    /// <summary>
    /// Example scene states
    /// </summary>
    public enum SceneStates
    {
        Init,
        Start,
        Lobby,
        Ingame
    }

    private SceneStates m_SceneState;

    /// <summary>
    /// Awake
    /// If another version exists, destroy it and use the current version
    /// Set our scene state to INIT
    /// </summary>
    private void Awake()
    {
        if(sceneTransitionHandler != this && sceneTransitionHandler != null)
        {
            GameObject.Destroy(sceneTransitionHandler.gameObject);
        }
        sceneTransitionHandler = this;
        SetSceneState(SceneStates.Init);
    }

    /// <summary>
    /// SetSceneState
    /// Sets the current scene state to help with transitioning.
    /// </summary>
    /// <param name="sceneState"></param>
    public void SetSceneState(SceneStates sceneState)
    {
        m_SceneState = sceneState;
        if(OnSceneStateChanged != null)
        {
            OnSceneStateChanged.Invoke(m_SceneState);
        }
    }

    /// <summary>
    /// GetCurrentSceneState
    /// Returns the current scene state
    /// </summary>
    /// <returns>current scene state</returns>
    public SceneStates GetCurrentSceneState()
    {
        return m_SceneState;
    }

    /// <summary>
    /// Start
    /// Loads the default main menu when started (this should always be a component added to the networking manager)
    /// </summary>
    private void Start()
    {
        if(m_SceneState == SceneStates.Init)
        {
            SceneManager.LoadScene(DefaultMainMenu);
        }
    }

    /// <summary>
    /// Switches to a new scene
    /// </summary>
    /// <param name="scenename"></param>
    public void SwitchScene(string scenename)
    {
        if(NetworkManager.Singleton.IsListening)
        {
            m_SceneProgress = NetworkSceneManager.SwitchScene(scenename);

            m_SceneProgress.OnClientLoadedScene += SceneProgress_OnClientLoadedScene;
        }
        else
        {
            SceneManager.LoadSceneAsync(scenename);
        }
    }

    public bool AllClientsAreLoaded()
    {
        if(m_SceneProgress != null)
        {
            return m_SceneProgress.IsAllClientsDoneLoading;
        }
        return false;
    }

    /// <summary>
    /// Invoked when a client has finished loading a scene
    /// </summary>
    /// <param name="clientId"></param>
    private void SceneProgress_OnClientLoadedScene(ulong clientId)
    {
        if(OnClientLoadedScene != null)
        {
            OnClientLoadedScene.Invoke(clientId);
        }
    }

    /// <summary>
    /// ExitAndLoadStartMenu
    /// This should be invoked upon a user exiting a multiplayer game session.
    /// </summary>
    public void ExitAndLoadStartMenu()
    {
        if(m_SceneProgress != null)
        {
            m_SceneProgress = null;
        }
        if(OnClientLoadedScene != null)
        {
            OnClientLoadedScene = null;
        }
        SetSceneState(SceneTransitionHandler.SceneStates.Start);
        SceneManager.LoadScene(1);
    }
}