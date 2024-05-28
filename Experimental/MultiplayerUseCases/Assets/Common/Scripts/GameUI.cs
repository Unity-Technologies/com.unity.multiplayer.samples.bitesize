using TMPro;
using UnityEngine;

namespace Unity.Netcode.Samples.MultiplayerUseCases.Common
{
    /// <summary>
    /// Manages the UI of the "NetworkVariable vs RPCs" scene
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [SerializeField] TMP_Text startupLabel;
        [SerializeField] TMP_Text controlsLabel;

        void Start()
        {
            RefreshLabels(NetworkManager.Singleton && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer));
            NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }

        void OnDestroy()
        {
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            }
        }

        void OnServerStarted()
        {
            RefreshLabels(true);
        }

        void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData connectionEventData)
        {
            if (connectionEventData.EventType == ConnectionEvent.ClientConnected)
            {
                if (NetworkManager.Singleton && NetworkManager.Singleton.IsServer)
                {
                    return; //you don't want to do actions twice when playing as a host
                }
                RefreshLabels(true);
            }

            else if (connectionEventData.EventType == ConnectionEvent.ClientDisconnected)
            {
                    RefreshLabels(false);
            }
        }

        void RefreshLabels(bool isConnected)
        {
            startupLabel.gameObject.SetActive(!isConnected);
            controlsLabel.gameObject.SetActive(isConnected);
        }
    }
}
