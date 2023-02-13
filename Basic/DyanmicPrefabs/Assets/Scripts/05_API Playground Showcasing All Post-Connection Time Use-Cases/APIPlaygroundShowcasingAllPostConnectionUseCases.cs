using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;

namespace Game.APIPlaygroundShowcasingAllPostConnectionUseCases
{
    /// <summary>
    /// This class serves as the playground of the dynamic prefab loading use-cases. It integrates API from this sample
    /// to use at post-connection time such as: connection approval for syncing late-joining clients, dynamically
    /// loading a collection of network prefabs on the host and all connected clients, synchronously spawning a
    /// dynamically loaded network prefab across connected clients, and spawning a dynamically loaded network prefab as
    /// network-invisible for all clients until they load the prefab locally (in which case it becomes visible to the
    /// client).
    /// </summary>
    /// <remarks>
    /// For more details on the API usage, see the in-project readme (which includes links to further resources,
    /// including the project's technical document).
    /// </remarks>
    public sealed class APIPlaygroundShowcasingAllPostConnectionUseCases : NetworkBehaviour
    {
        [SerializeField]
        NetworkManager m_NetworkManager;
        
        [SerializeField] List<AssetReferenceGameObject> m_DynamicPrefabReferences;
        
        const int k_MaxConnectPayload = 1024;
        
        [SerializeField]
        int m_ArtificialDelayMilliseconds = 1000;
        
        [SerializeField] float m_SpawnTimeoutInSeconds;
        
        //A storage where we keep association between prefab (hash of it's GUID) and the spawned network objects that use it
        Dictionary<int, HashSet<NetworkObject>> m_PrefabHashToNetworkObjectId = new Dictionary<int, HashSet<NetworkObject>>();
        
        float m_SynchronousSpawnTimeoutTimer;
        
        int m_SynchronousSpawnAckCount = 0;

        void Start()
        {
            DynamicPrefabLoadingUtilities.Init(m_NetworkManager);
            m_NetworkManager.ConnectionApprovalCallback += ConnectionApprovalCallback;
        }
        
        public override void OnDestroy()
        {
            m_NetworkManager.ConnectionApprovalCallback -= ConnectionApprovalCallback;
            DynamicPrefabLoadingUtilities.UnloadAndReleaseAllDynamicPrefabs();
            base.OnDestroy();
        }

        void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            Debug.Log("Client is trying to connect " + request.ClientNetworkId);
            var connectionData = request.Payload;
            var clientId = request.ClientNetworkId;
            
            if (clientId == m_NetworkManager.LocalClientId)
            {
                //allow the host to connect
                Approve();
                return;
            }
            
            if (connectionData.Length > k_MaxConnectPayload)
            {
                // If connectionData is too big, deny immediately to avoid wasting time on the server. This is intended as
                // a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage.
                ImmediateDeny();
                return;
            }
    
            if (DynamicPrefabLoadingUtilities.LoadedPrefabCount == 0)
            {
                //immediately approve the connection if we haven't loaded any prefabs yet
                Approve();
                return;
            }
    
            var payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload); // https://docs.unity3d.com/2020.2/Documentation/Manual/JSONSerialization.html
    
            int clientPrefabHash = connectionPayload.hashOfDynamicPrefabGUIDs;
            int serverPrefabHash = DynamicPrefabLoadingUtilities.HashOfDynamicPrefabGUIDs;
            
            //if the client has the same prefabs as the server - approve the connection
            if (clientPrefabHash == serverPrefabHash)
            {
                Approve();

                DynamicPrefabLoadingUtilities.RecordThatClientHasLoadedAllPrefabs(clientId);
                
                return;
            }
            
            // In order for clients to not just get disconnected with no feedback, the server needs to tell the client
            // why it disconnected it. This could happen after an auth check on a service or because of gameplay
            // reasons (server full, wrong build version, etc).
            // The server can do so via the DisconnectReason in the ConnectionApprovalResponse. The guids of the prefabs
            // the client will need to load will be sent, such that the client loads the needed prefabs, and reconnects.
            
            // A note: DisconnectReason will not be written to if the string is too large in size. This should be used
            // only to tell the client "why" it failed -- the client should instead use services like UGS to fetch the
            // relevant data it needs to fetch & download.

            DynamicPrefabLoadingUtilities.RefreshLoadedPrefabGuids();

            response.Reason = DynamicPrefabLoadingUtilities.GenerateDisconnectionPayload();
            
            ImmediateDeny();
            
            // A note: sending large strings through Netcode is not ideal -- you'd usually want to use REST services to
            // accomplish this instead. UGS services like Lobby can be a useful alternative. Another route may be to
            // set ConnectionApprovalResponse's Pending flag to true, and send a CustomMessage containing the array of 
            // GUIDs to a client, which the client would load and reattempt a reconnection.
    
            void Approve()
            {
                Debug.Log($"Client {clientId} approved");
                response.Approved = true;
                response.CreatePlayerObject = false; //we're not going to spawn a player object for this sample
            }
            
            void ImmediateDeny()
            {
                Debug.Log($"Client {clientId} denied connection");
                response.Approved = false;
                response.CreatePlayerObject = false;
            }
        }

        // invoked by UI
        public void OnClickedPreload()
        {
            if (!m_NetworkManager.IsServer)
            {
                return;
            }
            
            PreloadPrefabs();
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
        
        // invoked by UI
        public void OnClickedTrySpawnInvisible()
        {
            if (!m_NetworkManager.IsServer)
            {
                return;
            }
            
            TrySpawnInvisible();
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
                    var obj = await Spawn(assetGuid);
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
                        var obj = await Spawn(assetGuid);
                        return (true, obj);
                    }
                    
                    m_SynchronousSpawnTimeoutTimer += Time.deltaTime;
                    await Task.Yield();
                }
                
                Debug.LogError("Failed to spawn dynamic prefab - timeout");
                return (false, null);
            }

            return (false, null);

            async Task<NetworkObject> Spawn(AddressableGUID assetGuid)
            {
                var prefab = await DynamicPrefabLoadingUtilities.LoadDynamicPrefab(assetGuid,
                    m_ArtificialDelayMilliseconds);
                var obj = Instantiate(prefab, position, rotation).GetComponent<NetworkObject>();
                obj.Spawn();
                Debug.Log("Spawned dynamic prefab");
                return obj;
            }
        }

        async void TrySpawnInvisible()
        {
            var randomPrefab = m_DynamicPrefabReferences[Random.Range(0, m_DynamicPrefabReferences.Count)];
            await SpawnImmediatelyAndHideUntilPrefabIsLoadedOnClient(randomPrefab.AssetGUID, Random.insideUnitCircle * 5, Quaternion.identity);
        }
        
        /// <summary>
        /// This call spawns an addressable prefab by it's guid. It does not ensure that all the clients have loaded the
        /// prefab before spawning it. All spawned objects are invisible to clients that don't have the prefab loaded.
        /// The server tells the clients that lack the preloaded prefab to load it and acknowledge that they've loaded
        /// it, and then the server makes the object visible to that client.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        async Task<NetworkObject> SpawnImmediatelyAndHideUntilPrefabIsLoadedOnClient(string guid, Vector3 position, Quaternion rotation)
        {
            if (IsServer)
            {
                var assetGuid = new AddressableGUID()
                {
                    Value = guid
                };
                
                return await Spawn(assetGuid);
            }

            return null;

            async Task<NetworkObject> Spawn(AddressableGUID assetGuid)
            {
                var prefab = await DynamicPrefabLoadingUtilities.LoadDynamicPrefab(assetGuid, 
                    m_ArtificialDelayMilliseconds);
                var obj = Instantiate(prefab, position, rotation).GetComponent<NetworkObject>();
                
                if (m_PrefabHashToNetworkObjectId.TryGetValue(assetGuid.GetHashCode(), out var networkObjectIds))
                {
                    networkObjectIds.Add(obj);
                }
                else
                {
                    m_PrefabHashToNetworkObjectId.Add(assetGuid.GetHashCode(), new HashSet<NetworkObject>() {obj});
                }

                obj.CheckObjectVisibility = (clientId) => 
                {
                    //if the client has already loaded the prefab - we can make the object visible to them
                    if (DynamicPrefabLoadingUtilities.HasClientLoadedPrefab(clientId, assetGuid.GetHashCode()))
                    {
                        return true;
                    }
                    //otherwise the clients need to load the prefab, and after they ack - the ShowHiddenObjectsToClient 
                    LoadAddressableClientRpc(assetGuid, new ClientRpcParams(){Send = new ClientRpcSendParams(){TargetClientIds = new ulong[]{clientId}}});
                    return false;
                };
                
                obj.Spawn();

                return obj;
            }
        }
        
        void ShowHiddenObjectsToClient(int prefabHash, ulong clientId)
        {
            if (m_PrefabHashToNetworkObjectId.TryGetValue(prefabHash, out var networkObjects))
            {
                foreach (var obj in networkObjects)
                {
                    if (!obj.IsNetworkVisibleTo(clientId))
                    {
                        obj.NetworkShow(clientId);
                    }
                }
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
           
            //the server has all the objects visible, no need to do anything
            if (rpcParams.Receive.SenderClientId != m_NetworkManager.LocalClientId)
            {
                ShowHiddenObjectsToClient(prefabHash, rpcParams.Receive.SenderClientId);
            }
        }
    }
}
