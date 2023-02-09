using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.LoadAllPrefabs
{
    /// <summary>
    /// A simple use case where the server notifies all clients to preload a collection of network prefabs. The server
    /// will not invoke a spawn in this use case, and will incrementally load each dynamic prefab, one prefab at a time.
    /// </summary>
    public sealed class LoadAllPrefabs : NetworkBehaviour
    {
        [SerializeField]
        NetworkManager m_NetworkManager;
        
        [SerializeField] List<AssetReferenceGameObject> m_DynamicPrefabReferences;
        
        [SerializeField]
        int m_ArtificialDelayMilliseconds = 1000;

        void Start()
        {
            DynamicPrefabLoadingUtilities.Init(m_NetworkManager);
        }

        public override void OnDestroy()
        {
            DynamicPrefabLoadingUtilities.UnloadAndReleaseAllDynamicPrefabs();
            base.OnDestroy();
        }
        
        // invoked by UI
        public void OnClickedPreload()
        {
            PreloadPrefabs();
        }

        async void PreloadPrefabs()
        {
            var tasks = new List<Task>();
            foreach (var p in m_DynamicPrefabReferences)
            {
                tasks.Add(PreloadDynamicPrefabOnServerAndStartLoadingOnAllClients(p.AssetGUID));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// This call preloads the dynamic prefab on the server and sends a client rpc to all the clients to do the same.
        /// </summary>
        /// <param name="guid"></param>
        async Task PreloadDynamicPrefabOnServerAndStartLoadingOnAllClients(string guid)
        {
            if (m_NetworkManager.IsServer)
            {
                var assetGuid = new AddressableGUID()
                {
                    Value = guid
                };
                
                if (DynamicPrefabLoadingUtilities.IsPrefabLoadedOnAllClients(assetGuid))
                {
                    Debug.Log("Prefab is already loaded by all peers");
                    return;
                }
                
                Debug.Log("Loading dynamic prefab on the clients...");
                LoadAddressableClientRpc(assetGuid);
                await DynamicPrefabLoadingUtilities.LoadDynamicPrefab(assetGuid, m_ArtificialDelayMilliseconds);
            }
        }
        
        [ClientRpc]
        void LoadAddressableClientRpc(AddressableGUID guid, ClientRpcParams rpcParams = default)
        {
            if (!IsHost)
            {
                Load(guid);
            }

            async void Load(AddressableGUID assetGuid)
            {
                Debug.Log("Loading dynamic prefab on the client...");
                await DynamicPrefabLoadingUtilities.LoadDynamicPrefab(assetGuid, m_ArtificialDelayMilliseconds);
                Debug.Log("Client loaded dynamic prefab");
                AcknowledgeSuccessfulPrefabLoadServerRpc(assetGuid.GetHashCode());
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        void AcknowledgeSuccessfulPrefabLoadServerRpc(int prefabHash, ServerRpcParams rpcParams = default)
        {
            Debug.Log($"Client acknowledged successful prefab load with hash: {prefabHash}");
            DynamicPrefabLoadingUtilities.RecordThatClientHasLoadedAPrefab(prefabHash, rpcParams.Receive.SenderClientId);
        }
    }
}
