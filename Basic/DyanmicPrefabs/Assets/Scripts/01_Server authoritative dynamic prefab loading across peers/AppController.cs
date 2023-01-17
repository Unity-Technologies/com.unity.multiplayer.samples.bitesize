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
        // placeholder until this is fetched from UI
        string m_ConnectAddress = "127.0.0.1";
        
        // placeholder until this is fetched from UI
        ushort m_Port = 7777;

        [SerializeField] DynamicPrefabManager m_DynamicPrefabManager;
        
        [SerializeField] List<AssetReferenceGameObject> m_DynamicPrefabRefs;
        
        [SerializeField] GameObject m_ConnectionUI;
        
        [SerializeField] GameObject m_SpawnUI;

        [SerializeField]
        ConnectionManager m_ConnectionManager;


        public override void OnDestroy()
        {
            m_DynamicPrefabManager.UnloadAndReleaseAllDynamicPrefabs();
            base.OnDestroy();
        }

        public void StartClient()
        {
            Debug.Log(nameof(StartClient));
            m_ConnectionManager.StartClientIp(m_ConnectAddress, m_Port);
            m_ConnectionUI.SetActive(false);
        }

        public void StartHost()
        {
            Debug.Log(nameof(StartHost));
            m_ConnectionManager.StartHostIp(m_ConnectAddress, m_Port);
            m_ConnectionUI.SetActive(false);
        }

        public override void OnNetworkSpawn()
        {
            m_SpawnUI.SetActive(IsServer);
        }
        
        public override void OnNetworkDespawn()
        {
            m_ConnectionUI.SetActive(true);
            m_SpawnUI.SetActive(true);
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

        // placeholder until this is triggered by UI
        [ContextMenu(nameof(OnClickedShutdown))]
        public void OnClickedShutdown()
        {
            Debug.Log(nameof(OnClickedShutdown));
            m_ConnectionManager.RequestShutdown();
        }
    }
}
