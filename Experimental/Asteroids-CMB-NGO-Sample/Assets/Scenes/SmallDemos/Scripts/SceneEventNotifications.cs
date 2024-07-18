using System.Text;
using Unity.Netcode;
using UnityEngine;

public class SceneEventNotifications : MonoBehaviour
{

    private void Start()
    {
        if (NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.IsConnectedClient)
            {
                NetworkManager.Singleton.OnClientStopped += OnClientStopped;
                NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            }
        }
        else
        {
            NetworkManager.Singleton.OnClientStarted += OnClientStarted;
        }
    }

    private void OnClientStarted()
    {
        NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
    }

    private StringBuilder m_SceneEventLog = new StringBuilder();
    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        m_SceneEventLog.Clear();
        m_SceneEventLog.Append($"[Client-{sceneEvent.ClientId}][{sceneEvent.SceneEventType}]");
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.Load:
                {
                    m_SceneEventLog.Append($"[{sceneEvent.SceneName}][{sceneEvent.LoadSceneMode}]");
                    break;
                }
            case SceneEventType.Unload:
                {
                    m_SceneEventLog.Append($"[{sceneEvent.SceneName}]");
                    break;
                }
            case SceneEventType.LoadComplete:
                {
                    m_SceneEventLog.Append($"[{sceneEvent.SceneName}]");
                    break;
                }
            case SceneEventType.UnloadComplete:
                {
                    m_SceneEventLog.Append($"[{sceneEvent.SceneName}]");
                    break;
                }
        }
        NetworkManagerHelper.Instance.LogMessage(m_SceneEventLog.ToString());
    }

    private void OnClientStopped(bool wasHost)
    {
        NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        NetworkManager.Singleton.OnClientStarted += OnClientStarted;
    }
}
