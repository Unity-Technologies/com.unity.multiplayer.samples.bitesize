using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;

namespace Game.APIPlayground
{
    /// <summary>
    /// This class serves as the playground of the dynamic prefab loading use-cases. It integrates API from this sample
    /// to use at post-connection time such as: connection approval for syncing late-joining clients, dynamically
    /// loading a collection of network prefabs on the host and all connected clients, synchronously spawning a
    /// dynamically loaded network prefab across connected clients, and spawning a dynamically loaded network prefab as
    /// network-invisible for all clients until they load the prefab locally (in which case it becomes network-visible
    /// to the client).
    /// </summary>
    /// <remarks>
    /// For more details on the API usage, see the in-project readme (which includes links to further resources,
    /// including the project's technical document).
    /// </remarks>
    public sealed class APIPlayground : NetworkBehaviour
    {
        [SerializeField]
        NetworkManager m_NetworkManager;
        
        [SerializeField] List<AssetReferenceGameObject> m_DynamicPrefabReferences;
        
        [SerializeField] InGameUI m_InGameUI;
        
        const int k_MaxConnectPayload = 1024;
        
        //A storage where we keep association between prefab (hash of it's GUID) and the spawned network objects that use it
        Dictionary<int, HashSet<NetworkObject>> m_PrefabHashToNetworkObjectId = new Dictionary<int, HashSet<NetworkObject>>();
        
        float m_SynchronousSpawnTimeoutTimer;
        
        int m_SynchronousSpawnAckCount = 0;

        void Start()
        {
            DynamicPrefabLoadingUtilities.Init(m_NetworkManager);
            
            // In the use-cases where connection approval is implemented, the server can begin to validate a user's
            // connection payload, and either approve or deny connection to the joining client.
            m_NetworkManager.NetworkConfig.ConnectionApproval = true;
            
            // Here, we keep ForceSamePrefabs disabled. This will allow us to dynamically add network prefabs to Netcode
            // for GameObject after establishing a connection.
            m_NetworkManager.NetworkConfig.ForceSamePrefabs = false;
            m_NetworkManager.ConnectionApprovalCallback += ConnectionApprovalCallback;
            
            // hooking up UI callbacks
            m_InGameUI.LoadAllAsyncButtonPressed += OnClickedPreload;
            m_InGameUI.TrySpawnSynchronouslyButtonPressed += OnClickedTrySpawnSynchronously;
            m_InGameUI.SpawnUsingVisibilityButtonPressed += OnClickedTrySpawnInvisible;
        }
        
        public override void OnDestroy()
        {
            m_NetworkManager.ConnectionApprovalCallback -= ConnectionApprovalCallback;
            if (m_InGameUI)
            {
                m_InGameUI.LoadAllAsyncButtonPressed -= OnClickedPreload;
                m_InGameUI.TrySpawnSynchronouslyButtonPressed -= OnClickedTrySpawnSynchronously;
                m_InGameUI.SpawnUsingVisibilityButtonPressed -= OnClickedTrySpawnInvisible;
            }
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
            
            // A sample-specific denial on clients when 3 clients have been connected
            if (m_NetworkManager.ConnectedClientsList.Count >= 3)
            {
                ImmediateDeny();
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
        void OnClickedPreload()
        {
            if (!m_NetworkManager.IsServer)
            {
                return;
            }
            
            PreloadPrefabs();
        }
        
        // invoked by UI
        void OnClickedTrySpawnSynchronously()
        {
            if (!m_NetworkManager.IsServer)
            {
                return;
            }
            
            TrySpawnSynchronously();
        }
        
        // invoked by UI
        void OnClickedTrySpawnInvisible()
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
                
                // update UI for each client that is requested to load a certain prefab
                foreach (var client in m_NetworkManager.ConnectedClients.Keys)
                {
                    m_InGameUI.ClientLoadedPrefabStatusChanged(client, assetGuid.GetHashCode(), "Undefined", InGameUI.LoadStatus.Loading);
                }
                
                Debug.Log("Loading dynamic prefab on the clients...");
                LoadAddressableClientRpc(assetGuid);
                
                // server is starting to load a prefab, update UI
                m_InGameUI.ClientLoadedPrefabStatusChanged(NetworkManager.ServerClientId, assetGuid.GetHashCode(), "Undefined", InGameUI.LoadStatus.Loading);

                await DynamicPrefabLoadingUtilities.LoadDynamicPrefab(assetGuid, m_InGameUI.ArtificialDelayMilliseconds);
                
                // server loaded a prefab, update UI with the loaded asset's name
                DynamicPrefabLoadingUtilities.TryGetLoadedGameObjectFromGuid(assetGuid, out var loadedGameObject);
                m_InGameUI.ClientLoadedPrefabStatusChanged(NetworkManager.ServerClientId, assetGuid.GetHashCode(), loadedGameObject.Result.name, InGameUI.LoadStatus.Loaded);
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
                    var obj = Spawn(assetGuid);
                    return (true, obj);
                }
                
                m_SynchronousSpawnAckCount = 0;
                m_SynchronousSpawnTimeoutTimer = 0;
                
                Debug.Log("Loading dynamic prefab on the clients...");
                LoadAddressableClientRpc(assetGuid);
                
                // server is starting to load a prefab, update UI
                m_InGameUI.ClientLoadedPrefabStatusChanged(NetworkManager.ServerClientId, assetGuid.GetHashCode(), "Undefined", InGameUI.LoadStatus.Loading);

                //load the prefab on the server, so that any late-joiner will need to load that prefab also
                await DynamicPrefabLoadingUtilities.LoadDynamicPrefab(assetGuid, m_InGameUI.ArtificialDelayMilliseconds);
                
                // server loaded a prefab, update UI with the loaded asset's name
                DynamicPrefabLoadingUtilities.TryGetLoadedGameObjectFromGuid(assetGuid, out var loadedGameObject);
                m_InGameUI.ClientLoadedPrefabStatusChanged(NetworkManager.ServerClientId, assetGuid.GetHashCode(), loadedGameObject.Result.name, InGameUI.LoadStatus.Loaded);

                var requiredAcknowledgementsCount = IsHost ? m_NetworkManager.ConnectedClients.Count - 1 : 
                    m_NetworkManager.ConnectedClients.Count;
                
                while (m_SynchronousSpawnTimeoutTimer < m_InGameUI.NetworkSpawnTimoutSeconds)
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
                
                // left to the reader: you'll need to be reactive to clients failing to load -- you should either have
                // the offending client try again or disconnect it after a predetermined amount of failed attempts
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

        async void TrySpawnInvisible()
        {
            var randomPrefab = m_DynamicPrefabReferences[Random.Range(0, m_DynamicPrefabReferences.Count)];
            await SpawnImmediatelyAndHideUntilPrefabIsLoadedOnClient(randomPrefab.AssetGUID, Random.insideUnitCircle * 5, Quaternion.identity);
        }
        
        /// <summary>
        /// This call spawns an addressable prefab by it's guid. It does not ensure that all the clients have loaded the
        /// prefab before spawning it. All spawned objects are network-invisible to clients that don't have the prefab
        /// loaded. The server tells the clients that lack the preloaded prefab to load it and acknowledge that they've
        /// loaded it, and then the server makes the object network-visible to that client.
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
                // server is starting to load a prefab, update UI
                m_InGameUI.ClientLoadedPrefabStatusChanged(NetworkManager.ServerClientId, assetGuid.GetHashCode(), "Undefined", InGameUI.LoadStatus.Loading);

                var prefab = await DynamicPrefabLoadingUtilities.LoadDynamicPrefab(assetGuid, 
                    m_InGameUI.ArtificialDelayMilliseconds);
                
                // server loaded a prefab, update UI with the loaded asset's name
                DynamicPrefabLoadingUtilities.TryGetLoadedGameObjectFromGuid(assetGuid, out var loadedGameObject);
                m_InGameUI.ClientLoadedPrefabStatusChanged(NetworkManager.ServerClientId, assetGuid.GetHashCode(), loadedGameObject.Result.name, InGameUI.LoadStatus.Loaded);                
                
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
                    if (clientId == NetworkManager.ServerClientId)
                    {
                        // object is loaded on the server, no need to validate for visibility
                        return true;
                    }
                    
                    //if the client has already loaded the prefab - we can make the object network-visible to them
                    if (DynamicPrefabLoadingUtilities.HasClientLoadedPrefab(clientId, assetGuid.GetHashCode()))
                    {
                        return true;
                    }
                    
                    // client is loading a prefab, update UI
                    m_InGameUI.ClientLoadedPrefabStatusChanged(clientId, assetGuid.GetHashCode(), "Undefined", InGameUI.LoadStatus.Loading);

                    //otherwise the client need to load the prefab, and after they ack - the ShowHiddenObjectsToClient 
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
                // loading prefab as a client, update UI
                m_InGameUI.ClientLoadedPrefabStatusChanged(m_NetworkManager.LocalClientId, assetGuid.GetHashCode(), "Undefined", InGameUI.LoadStatus.Loading);

                Debug.Log("Loading dynamic prefab on the client...");
                await DynamicPrefabLoadingUtilities.LoadDynamicPrefab(assetGuid, m_InGameUI.ArtificialDelayMilliseconds);
                Debug.Log("Client loaded dynamic prefab");
                
                DynamicPrefabLoadingUtilities.TryGetLoadedGameObjectFromGuid(assetGuid, out var loadedGameObject);
                m_InGameUI.ClientLoadedPrefabStatusChanged(m_NetworkManager.LocalClientId, assetGuid.GetHashCode(), loadedGameObject.Result.name, InGameUI.LoadStatus.Loaded);

                AcknowledgeSuccessfulPrefabLoadServerRpc(assetGuid.GetHashCode(), loadedGameObject.Result.name);
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        void AcknowledgeSuccessfulPrefabLoadServerRpc(int prefabHash, string loadedPrefabName, ServerRpcParams rpcParams = default)
        {
            m_SynchronousSpawnAckCount++;
            Debug.Log($"Client acknowledged successful prefab load with hash: {prefabHash}");
            DynamicPrefabLoadingUtilities.RecordThatClientHasLoadedAPrefab(prefabHash, 
                rpcParams.Receive.SenderClientId);
           
            //the server has all the objects network-visible, no need to do anything
            if (rpcParams.Receive.SenderClientId != m_NetworkManager.LocalClientId)
            {
                // Note: there's a potential security risk here if this technique is tied with gameplay that uses
                // a NetworkObject's Show() and Hide() methods. For example, a malicious player could invoke a similar
                // ServerRpc with the guids of enemy players, and it would make those enemies visible (network side
                // and/or visually) to that player, giving them a potential advantage.
                ShowHiddenObjectsToClient(prefabHash, rpcParams.Receive.SenderClientId);
            }
            
            // client has successfully loaded a prefab, update UI
            m_InGameUI.ClientLoadedPrefabStatusChanged(rpcParams.Receive.SenderClientId, prefabHash, loadedPrefabName, InGameUI.LoadStatus.Loaded);
        }
    }
}
