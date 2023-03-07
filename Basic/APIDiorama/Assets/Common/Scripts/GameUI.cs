using System.Collections;
using TMPro;
using UnityEngine;

namespace Unity.Netcode.Samples.APIDiorama
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
            Refreshlabels(NetworkManager.Singleton && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer));
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
        }

        void OnDestroy()
        {
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            }
        }

        void OnServerStarted()
        {
            OnNetworkedSessionStarted();
        }

        void OnClientConnectedCallback(ulong obj)
        {
            if (NetworkManager.Singleton && NetworkManager.Singleton.IsServer)
            {
                return; //you don't want to do actions twice when playing as a host
            }
            OnNetworkedSessionStarted();
        }

        void OnClientDisconnectCallback(ulong obj)
        {
            OnNetworkedSessionEnded();
        }

        void Refreshlabels(bool isConnected)
        {
            startupLabel.gameObject.SetActive(!isConnected);
            controlsLabel.gameObject.SetActive(isConnected);
        }

        void OnNetworkedSessionStarted()
        {
            Refreshlabels(true);
        }

        void OnNetworkedSessionEnded()
        {
            Refreshlabels(false);
        }
    }
}
