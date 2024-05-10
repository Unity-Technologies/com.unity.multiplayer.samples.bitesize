using System;
using Game.UI;
using Unity.Netcode;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// A class to bind UI events to invocations from <see cref="OptionalConnectionManager"/>, where client and host
    /// connection requests are initiated. This class also listens for status updates from Netcode for GameObjects to
    /// then display the appropriate UI elements.
    /// </summary>
    public sealed class AppController : MonoBehaviour
    {
        [SerializeField]
        NetworkManager m_NetworkManager;

        [SerializeField]
        OptionalConnectionManager m_ConnectionManager;

        [SerializeField]
        IPMenuUI m_IPMenuUI;

        [SerializeField]
        InGameUI m_InGameUI;

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        void Start()
        {
            m_IPMenuUI.ResetUI();
            m_InGameUI.Hide();

            m_NetworkManager.OnConnectionEvent += OnConnectionEvent;

            m_IPMenuUI.HostButtonPressed += StartHost;
            m_IPMenuUI.ClientButtonPressed += StartClient;
            m_IPMenuUI.DisconnectButtonPressed += Disconnect;
        }

        void OnDestroy()
        {
            m_NetworkManager.OnConnectionEvent -= OnConnectionEvent;

            if (m_IPMenuUI)
            {
                m_IPMenuUI.HostButtonPressed -= StartHost;
                m_IPMenuUI.ClientButtonPressed -= StartClient;
                m_IPMenuUI.DisconnectButtonPressed -= Disconnect;
            }
        }

        void OnConnectionEvent(NetworkManager arg1, ConnectionEventData arg2)
        {
            if (arg2.EventType == ConnectionEvent.ClientConnected)
            {
                m_IPMenuUI.HideIPConnectionMenu();

                // for host
                if (m_NetworkManager.IsHost)
                {
                    if (arg2.ClientId == m_NetworkManager.LocalClientId)
                    {
                        m_IPMenuUI.HostStarted();
                        m_InGameUI.Show(InGameUI.ButtonVisibility.Server);
                        m_InGameUI.AddConnectionUIInstance(arg2.ClientId, new int[] { }, new string[] { });
                    }
                    else
                    {
                        // grab all loaded prefabs and represent that on the newly joined client
                        var loadedPrefabs = GetLoadedPrefabsHashesAndNames();

                        m_InGameUI.AddConnectionUIInstance(arg2.ClientId, loadedPrefabs.Item1, loadedPrefabs.Item2);
                    }
                }
                else if (m_NetworkManager.IsClient)
                {
                    // for clients that are not host
                    if (arg2.ClientId == m_NetworkManager.LocalClientId)
                    {
                        m_IPMenuUI.ClientStarted();
                        m_InGameUI.Show(InGameUI.ButtonVisibility.Client);

                        // grab all locally loaded prefabs and represent that on local client
                        var loadedPrefabs = GetLoadedPrefabsHashesAndNames();

                        m_InGameUI.AddConnectionUIInstance(arg2.ClientId, loadedPrefabs.Item1, loadedPrefabs.Item2);
                    }
                }
            }
            else if (arg2.EventType == ConnectionEvent.ClientDisconnected)
            {
                // when a connected client disconnects, remove their UI
                if (m_NetworkManager.IsServer && arg2.ClientId != NetworkManager.ServerClientId)
                {
                    // if a connected client disconnects on the host
                    m_InGameUI.RemoveConnectionUIInstance(arg2.ClientId);
                }
                else
                {
                    // show connection UI only when the local client disconnects
                    m_IPMenuUI.ResetUI();
                    m_InGameUI.RemoveConnectionUIInstance(m_NetworkManager.LocalClientId);
                    m_InGameUI.Hide();
                }
            }
        }

        static Tuple<int[], string[]> GetLoadedPrefabsHashesAndNames()
        {
            var loadedHashes = new int[DynamicPrefabLoadingUtilities.LoadedDynamicPrefabResourceHandles.Keys.Count];
            var loadedNames = new string[DynamicPrefabLoadingUtilities.LoadedDynamicPrefabResourceHandles.Keys.Count];
            int index = 0;
            foreach (var loadedPrefab in DynamicPrefabLoadingUtilities.LoadedDynamicPrefabResourceHandles)
            {
                loadedHashes[index] = loadedPrefab.Key.GetHashCode();
                loadedNames[index] = loadedPrefab.Value.Result.name;
                index++;
            }

            return Tuple.Create(loadedHashes, loadedNames);
        }

        void StartHost()
        {
            Debug.Log(nameof(StartHost));
            m_ConnectionManager.StartHostIp(m_IPMenuUI.IpAddress, m_IPMenuUI.Port);
        }

        void StartClient()
        {
            Debug.Log(nameof(StartClient));
            m_ConnectionManager.StartClientIp(m_IPMenuUI.IpAddress, m_IPMenuUI.Port);
        }

        public void Disconnect()
        {
            Debug.Log(nameof(Disconnect));
            m_ConnectionManager.RequestShutdown();
            m_InGameUI.DisconnectRequested();
            m_IPMenuUI.DisconnectRequested();
        }

        void OnApplicationQuit()
        {
            Disconnect();
        }
    }
}
