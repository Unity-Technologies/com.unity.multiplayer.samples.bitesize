using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;

namespace Game.ServerAuthoritativeSynchronousDynamicPrefabSpawn
{
    /// <summary>
    /// A dynamic prefab loading use-case where the server instructs all clients to load a single network prefab, and
    /// will only invoke a spawn once all clients have successfully completed their respective loads of said prefab. The
    /// server will initially send a ClientRpc to all clients, begin loading the prefab on the server, will await
    /// acknowledgement of a load via ServerRpcs from each client, and will only spawn the prefab over the network once
    /// it has received an acknowledgement from every client, within m_SynchronousSpawnTimeoutTimer seconds.
    /// </summary>
    /// <remarks>
    /// This use-case is recommended for scenarios where you'd want to guarantee the same world version across all
    /// connected clients. Since the server will wait until all clients have loaded the same dynamic prefab, the spawn
    /// of said dynamic prefab will be synchronous. Thus, we recommend using this technique for spawning game-changing
    /// gameplay elements, assuming you'd want all clients to be able to interact with said gameplay elements from the
    /// same point forward. For example, you wouldn't want to have an enemy only be visible (network side and/or
    /// visually) to some clients and not others -- you'd want to delay the enemy's spawn until all clients have
    /// dynamically loaded it and are able to see it before spawning it server side.
    /// </remarks>
    public sealed class ServerAuthoritativeSynchronousDynamicPrefabSpawn : NetworkBehaviour
    {
        [SerializeField]
        NetworkManager m_NetworkManager;
        
        [SerializeField] List<AssetReferenceGameObject> m_DynamicPrefabReferences;
        
        [SerializeField]
        int m_ArtificialDelayMilliseconds = 1000;
        
        [SerializeField] float m_SpawnTimeoutInSeconds;
        
        float m_SynchronousSpawnTimeoutTimer;
        
        int m_SynchronousSpawnAckCount = 0;
        
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
        public void OnClickedTrySpawnSynchronously()
        {
            if (!m_NetworkManager.IsServer)
            {
                return;
            }
            
            TrySpawnSynchronously();
        }
        
        async void TrySpawnSynchronously()
        {
            var randomPrefab = m_DynamicPrefabReferences[Random.Range(0, m_DynamicPrefabReferences.Count)];
            await TrySpawnDynamicPrefabSynchronously(randomPrefab.AssetGUID, Random.insideUnitCircle * 5, Quaternion.identity);
        }

        /// <summary>
        /// This call attempts to spawn a prefab by it's addressable guid - it ensures that all the clients have loaded the prefab before spawning it,
        /// and if the clients fail to acknowledge that they've loaded a prefab - the spawn will fail.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        async Task<(bool Success, NetworkObject Obj)> TrySpawnDynamicPrefabSynchronously(string guid, Vector3 position, Quaternion rotation)
        {
            if (IsServer)
            {
                var assetGuid = new AddressableGUID()
                {
                    Value = guid
                };
                
                if (DynamicPrefabLoadingUtilities.IsPrefabLoadedOnAllClients(assetGuid))
                {
                    Debug.Log("Prefab is already loaded by all peers, we can spawn it immediately");
                    var obj = Spawn(assetGuid);
                    return (true, obj);
                }
                
                m_SynchronousSpawnAckCount = 0;
                m_SynchronousSpawnTimeoutTimer = 0;
                
                Debug.Log("Loading dynamic prefab on the clients...");
                LoadAddressableClientRpc(assetGuid);
                //load the prefab on the server, so that any late-joiner will need to load that prefab also
                await DynamicPrefabLoadingUtilities.LoadDynamicPrefab(assetGuid, m_ArtificialDelayMilliseconds);
                var requiredAcknowledgementsCount = IsHost ? m_NetworkManager.ConnectedClients.Count - 1 : 
                    m_NetworkManager.ConnectedClients.Count;
                
                while (m_SynchronousSpawnTimeoutTimer < m_SpawnTimeoutInSeconds)
                {
                    if (m_SynchronousSpawnAckCount >= requiredAcknowledgementsCount)
                    {
                        Debug.Log($"All clients have loaded the prefab in {m_SynchronousSpawnTimeoutTimer} seconds, spawning the prefab on the server...");
                        var obj = Spawn(assetGuid);
                        return (true, obj);
                    }
                    
                    m_SynchronousSpawnTimeoutTimer += Time.deltaTime;
                    await Task.Yield();
                }
                
                // left to the reader: this you'll need to be reactive to -- you should either have the offending client
                // try again or disconnect it after a predetermined amount of failed attempts
                Debug.LogError("Failed to spawn dynamic prefab - timeout");
                return (false, null);
            }

            return (false, null);

            NetworkObject Spawn(AddressableGUID assetGuid)
            {
                if (!DynamicPrefabLoadingUtilities.TryGetLoadedGameObjectFromGuid(assetGuid, out var prefab))
                {
                    Debug.LogWarning($"GUID {assetGuid} is not a GUID of a previously loaded prefab. Failed to spawn a prefab.");
                    return null;
                }
                var obj = Instantiate(prefab.Result, position, rotation).GetComponent<NetworkObject>();
                obj.Spawn();
                Debug.Log("Spawned dynamic prefab");
                return obj;
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
            m_SynchronousSpawnAckCount++;
            Debug.Log($"Client acknowledged successful prefab load with hash: {prefabHash}");
            DynamicPrefabLoadingUtilities.RecordThatClientHasLoadedAPrefab(prefabHash, 
                rpcParams.Receive.SenderClientId);
        }
    }
}
