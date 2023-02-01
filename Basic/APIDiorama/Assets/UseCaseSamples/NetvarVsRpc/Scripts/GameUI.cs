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

        void OnEnable()
        {
            RefreshLabels(NetworkManager.Singleton && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer));
            StartCoroutine(SubscribeToNetworkManagerEvents());
        }

        IEnumerator SubscribeToNetworkManagerEvents()
        {
            yield return new WaitUntil(() => NetworkManager.Singleton);
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
                return; //we don't want to do actions twice when playing as a host
            }
            OnNetworkedSessionStarted();
        }

        void OnClientDisconnectCallback(ulong obj)
        {
            OnNetworkedSessionEnded();
        }

        void RefreshLabels(bool isConnected)
        {
            startupLabel.gameObject.SetActive(!isConnected);
            controlsLabel.gameObject.SetActive(isConnected);
        }

        void OnNetworkedSessionStarted()
        {
            RefreshLabels(true);
        }

        void OnNetworkedSessionEnded()
        {
            RefreshLabels(false);
        }
    }
}
