using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Game
{
    //idea for visualization:
    // - server has a list of prefabs to pick from 
    // - it dynamically spawns prefabs from time to time
    
    /// <summary>
    /// In this scenario we are spawning prefabs that aren't known to the clients beforehand.
    ///
    /// NGO requires us to load the prefab before we spawn it.
    ///
    /// The inbuilt delay that is accessible through NetworkManager.NetworkConfig.SpawnTimeout is NOT MEANT to serve
    /// as a buffering time during which the clients attempt to catch up with the server's spawn command by loading the prefab and hoping that this load won't take more than the timeout.
    /// Such approach would inevitably lead to desyncs in production.
    /// 
    /// To be safe and to respect the NGO requirement of loading the prefab before spawning it, we:
    ///  - ensure that the clients acknowledge that they have loaded the prefab
    ///  - the server waits for a specified amount of time for the clients to acknowledge the load, and if all the clients are successful - it spawns the prefab
    ///  - otherwise the server runs out of time and the spawn is cancelled
    /// </summary>
    public sealed class AppController : NetworkBehaviour
    {
        ushort m_Port = 7777;
        string m_ConnectAddress = "127.0.0.1";

        [SerializeField] DynamicPrefabManager m_DynamicPrefabManager;
        [SerializeField] List<AssetReferenceGameObject> m_DynamicPrefabRefs;
        [SerializeField] GameObject m_ConnectionUI;
        [SerializeField] Button m_SpawnButton;

        void Awake()
        {
            m_DynamicPrefabManager.OnConnectionStatusReceived += OnConnectionStatusReceived;
        }
        
        public override void OnDestroy()
        {
            m_DynamicPrefabManager.UnloadAndReleaseAllDynamicPrefabs();
            m_DynamicPrefabManager.OnConnectionStatusReceived -= OnConnectionStatusReceived;
            base.OnDestroy();
        }
        
        void OnConnectionStatusReceived(ConnectStatus status, FastBufferReader reader)
        {
            switch (status)
            {
                case ConnectStatus.Undefined:
                    m_ConnectionUI.SetActive(true);
                    break;
                case ConnectStatus.ClientNeedsToPreload:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        public void StartClient()
        {
            Debug.Log(nameof(StartClient));
            m_DynamicPrefabManager.StartClient(m_ConnectAddress, m_Port);
            m_ConnectionUI.SetActive(false);
        }

        public void StartHost()
        {
            Debug.Log(nameof(StartHost));
            m_DynamicPrefabManager.StartHost(m_ConnectAddress, m_Port);
            m_ConnectionUI.SetActive(false);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                m_SpawnButton.onClick.AddListener(OnClickedSpawnButton);
                //todo: start a coroutine that spawns random prefabs every once in a while
            }
            else
            {
                m_SpawnButton.gameObject.SetActive(false);
            }
        }
        
        async void OnClickedSpawnButton()
        {
            var randomPrefab = m_DynamicPrefabRefs[Random.Range(0, m_DynamicPrefabRefs.Count)];
            var spawnedObject =  await m_DynamicPrefabManager.SpawnWithVisibilitySystem(randomPrefab.AssetGUID);
            spawnedObject.transform.position = Random.insideUnitCircle * 5;
            return;
            m_SpawnButton.gameObject.SetActive(false);
                
            
            bool didManageToSpawn = await m_DynamicPrefabManager.TrySpawnDynamicPrefabSynchronously(randomPrefab.AssetGUID);

            if (!didManageToSpawn)
            {
                m_SpawnButton.gameObject.SetActive(true);
            }
        }
    }
}
