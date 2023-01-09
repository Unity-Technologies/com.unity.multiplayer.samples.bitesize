using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;

namespace Game
{
    public sealed class AppController : NetworkBehaviour
    {
        ushort m_Port = 7777;
        string m_ConnectAddress = "127.0.0.1";

        [SerializeField] DynamicPrefabManager m_DynamicPrefabManager;
        [SerializeField] List<AssetReferenceGameObject> m_DynamicPrefabRefs;
        [SerializeField] GameObject m_ConnectionUI;
        
        [SerializeField] GameObject m_SpawnUI;

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
        
        void OnConnectionStatusReceived(DisconnectReason status)
        {
            switch (status)
            {
                case DisconnectReason.Undefined:
                    m_ConnectionUI.SetActive(true);
                    break;
                case DisconnectReason.ClientNeedsToPreload:
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
            m_SpawnUI.SetActive(IsServer);
        }

        public async void OnClickedSpawnWithVisibility()
        {
            var randomPrefab = m_DynamicPrefabRefs[Random.Range(0, m_DynamicPrefabRefs.Count)];
            var spawnedObject =  await m_DynamicPrefabManager.SpawnImmediatelyAndHideUntilPrefabIsLoadedOnClient(randomPrefab.AssetGUID, Random.insideUnitCircle * 5, Quaternion.identity);
        }

        public async void OnClickedTrySpawnSynchronously()
        {
            var randomPrefab = m_DynamicPrefabRefs[Random.Range(0, m_DynamicPrefabRefs.Count)];
            var spawnedObject =  await m_DynamicPrefabManager.TrySpawnDynamicPrefabSynchronously(randomPrefab.AssetGUID, Random.insideUnitCircle * 5, Quaternion.identity);
        }

        public async void OnClickedPreload()
        {
            var tasks = new List<Task>();
            foreach (var p in m_DynamicPrefabRefs)
            {
                tasks.Add(m_DynamicPrefabManager.PreloadDynamicPrefabOnServerAndStartLoadingOnAllClients(p.AssetGUID));
            }

            await Task.WhenAll(tasks);

        }
    }
}
