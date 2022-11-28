using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game
{
    public sealed class DynamicPrefabManager : NetworkBehaviour
    {
        const int k_MaxConnectPayload = 1024;
        const int k_EmptyDynamicPrefabHash = -1;
        [SerializeField] float m_SpawnTimeoutInSeconds;
        [SerializeField] NetworkManager m_NetworkManager;
        
        int m_CountOfClientsThatLoadedThePrefab = 0;
        float m_SpawnTimeoutTimer = 0;
        
        int m_HashOfDynamicPrefabGUIDs = k_EmptyDynamicPrefabHash;
        Dictionary<AddressableGUID, AsyncOperationHandle<GameObject>> m_LoadedDynamicPrefabResourceHandles = new Dictionary<AddressableGUID, AsyncOperationHandle<GameObject>>();
        List<AddressableGUID> m_DynamicPrefabGUIDs = new List<AddressableGUID>(); //cached list to avoid GC

        public event Action<ConnectStatus, FastBufferReader> OnConnectionStatusReceived;

        string m_ConnectAddress;
        ushort m_Port;
        
        //A storage where we keep the association between the dynamic prefab (hash of it's GUID) and the clients that have it loaded
        Dictionary<int, HashSet<ulong>> m_PrefabHashToClientIds = new Dictionary<int, HashSet<ulong>>();
        
        //A storage where we keep association between prefab (hash of it's GUID) and the spawned network objects that use it
        Dictionary<int, HashSet<ulong>> m_PrefabHashToNetworkObjectId = new Dictionary<int, HashSet<ulong>>();
        
        public bool HasClientLoadedPrefab(ulong clientId, int prefabHash) => m_PrefabHashToClientIds.TryGetValue(prefabHash, out var clientIds) && clientIds.Contains(clientId);

        void RecordThatClientHasLoadedAPrefab(int assetGuidHash, ulong clientId)
        {
            if (m_PrefabHashToClientIds.TryGetValue(assetGuidHash, out var clientIds))
            {
                clientIds.Add(clientId);
            }
            else
            {
                m_PrefabHashToClientIds.Add(assetGuidHash, new HashSet<ulong>() {clientId});
            }
        }
        
        public void StartClient(string connectAddress, ushort port)
        {
            Debug.Log(nameof(StartClient));
            m_ConnectAddress = connectAddress;
            m_Port = port;
            
            m_NetworkManager.NetworkConfig.ForceSamePrefabs = false;
            
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                HashOfDynamicPrefabGUIDs = m_HashOfDynamicPrefabGUIDs
            });
            var transport = m_NetworkManager.GetComponent<UnityTransport>();
            transport.SetConnectionData(connectAddress, port);
            
            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            m_NetworkManager.NetworkConfig.ConnectionData = payloadBytes;
            m_NetworkManager.StartClient();
            m_NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(nameof(ReceiveServerToClientSetDisconnectReason_CustomMessage), ReceiveServerToClientSetDisconnectReason_CustomMessage);
        }

        public void StartHost(string connectAddress, ushort port)
        {
            Debug.Log(nameof(StartHost));
            m_ConnectAddress = connectAddress;
            m_Port = port;
            m_NetworkManager.NetworkConfig.ForceSamePrefabs = false;
            var transport = m_NetworkManager.GetComponent<UnityTransport>();
            transport.SetConnectionData(connectAddress, port);
            m_NetworkManager.ConnectionApprovalCallback = ConnectionApprovalCallback;
            m_NetworkManager.StartHost();
        }

        public override void OnDestroy()
        {
            UnloadAndReleaseAllDynamicPrefabs();
            base.OnDestroy();
        }

        async void ReceiveServerToClientSetDisconnectReason_CustomMessage(ulong clientID, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ConnectStatus status);

            switch (status)
            {
                case ConnectStatus.Undefined:
                    Debug.Log("Undefined");
                    m_NetworkManager.Shutdown();
                    break;
                case ConnectStatus.ClientNeedsToPreload:
                {
                    m_NetworkManager.Shutdown();
                    Debug.Log("Client needs to preload");
                    reader.ReadValueSafe(out AddressableGUIDCollection addressableGUIDCollection);
                    await LoadDynamicPrefabs(addressableGUIDCollection);
                    Debug.Log("Restarting client");
                    StartClient(m_ConnectAddress, m_Port);
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            OnConnectionStatusReceived?.Invoke(status, reader);
        }
        
        /// <summary>
        /// Sends a DisconnectReason to the indicated client. This should only be done on the server, prior to disconnecting the client.
        /// </summary>
        /// <param name="clientID"> id of the client to send to </param>
        /// <param name="status"> The reason for the upcoming disconnect.</param>
        /// <param name="addressableGUIDCollection"></param>
        void SendServerToClientSetDisconnectReason(ulong clientID, ConnectStatus status, AddressableGUIDCollection addressableGUIDCollection)
        {
            int guidCollectionSize = addressableGUIDCollection.GetSizeInBytes();
            
            var writer = new FastBufferWriter(sizeof(ConnectStatus) + guidCollectionSize, Allocator.Temp);
            writer.WriteValueSafe(status);
            writer.WriteValueSafe(addressableGUIDCollection);
            
            m_NetworkManager.CustomMessagingManager.SendNamedMessage(nameof(ReceiveServerToClientSetDisconnectReason_CustomMessage), clientID, writer);
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

            if (m_LoadedDynamicPrefabResourceHandles.Count == 0)
            {
                //immediately approve the connection if we haven't loaded any prefabs yet
                Approve();
                return;
            }

            var payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload); // https://docs.unity3d.com/2020.2/Documentation/Manual/JSONSerialization.html

            int clientPrefabHash = connectionPayload.HashOfDynamicPrefabGUIDs;
            int serverPrefabHash = m_HashOfDynamicPrefabGUIDs;
            
            //if the client has the same prefabs as the server - approve the connection
            if (clientPrefabHash == serverPrefabHash)
            {
                Approve();
                
                foreach (var dynamicPrefabGUID in m_DynamicPrefabGUIDs)
                {
                    RecordThatClientHasLoadedAPrefab(dynamicPrefabGUID.GetHashCode(), clientId);
                }
                
                return;
            }

            StartCoroutine(WaitToDenyApproval(clientId, ConnectStatus.ClientNeedsToPreload, m_LoadedDynamicPrefabResourceHandles.Keys));
            
            void Approve()
            {
                response.Approved = true;
                response.CreatePlayerObject = false; //we're not going to spawn a player object for this sample
            }
            
            void ImmediateDeny()
            {
                response.Approved = false;
                response.CreatePlayerObject = false;
            }
            
            // In order for clients to not just get disconnected with no feedback, the server needs to tell the client why it disconnected it.
            // This could happen after an auth check on a service or because of gameplay reasons (server full, wrong build version, etc)
            // Since network objects haven't synced yet (still in the approval process), we need to send a custom message to clients, wait for
            // UTP to update a frame and flush that message, then give our response to NetworkManager's connection approval process, with a denied approval.
            IEnumerator WaitToDenyApproval(ulong clientID, ConnectStatus status, ICollection<AddressableGUID> addressableGUIDs)
            {
                response.Pending = true; // give some time for server to send connection status message to clients
                response.Approved = false;
                
                m_DynamicPrefabGUIDs.Clear();
                m_DynamicPrefabGUIDs.AddRange(m_LoadedDynamicPrefabResourceHandles.Keys);
                SendServerToClientSetDisconnectReason(clientID, status, new AddressableGUIDCollection(){GUIDs = m_DynamicPrefabGUIDs.ToArray()});
                yield return null; // wait a frame so UTP can flush it's messages on next update
                response.Pending = false; // connection approval process can be finished.
            }
        }
        
        /// <summary>
        /// This call attempts to spawn a prefab by it's addressable guid - it ensures that all the clients have loaded the prefab before spawning it,
        /// and if the clients fail to acknowledge that they've loaded a prefab - the spawn will fail.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public async Task<bool> TrySpawnDynamicPrefabSynchronously(string guid)
        {
            if (IsServer)
            {
                var assetGuid = new AddressableGUID()
                {
                    Value = guid
                };
                
                if (m_LoadedDynamicPrefabResourceHandles.ContainsKey(assetGuid))
                {
                    Debug.Log("Prefab is already loaded by all peers, we can spawn it immediately");
                    await Spawn(assetGuid);
                    return true;
                }
                
                m_CountOfClientsThatLoadedThePrefab = 0;
                m_SpawnTimeoutTimer = 0;
                
                Debug.Log("Loading dynamic prefab on the clients...");
                LoadAddressableClientRpc(assetGuid);

                int requiredAcknowledgementsCount = IsHost ? m_NetworkManager.ConnectedClients.Count - 1 : m_NetworkManager.ConnectedClients.Count;
                
                while (m_SpawnTimeoutTimer < m_SpawnTimeoutInSeconds)
                {
                    if (m_CountOfClientsThatLoadedThePrefab >= requiredAcknowledgementsCount)
                    {
                        Debug.Log($"All clients have loaded the prefab in {m_SpawnTimeoutTimer} seconds, spawning the prefab on the server...");
                        await Spawn(assetGuid);
                        return true;
                    }
                    
                    m_SpawnTimeoutTimer += Time.deltaTime;
                    await Task.Yield();
                }
                
                Debug.LogError("Failed to spawn dynamic prefab - timeout");
                return false;
            }

            return false;

            async Task Spawn(AddressableGUID assetGuid)
            {
                var prefab = await LoadDynamicPrefab(assetGuid);
                var obj = Instantiate(prefab).GetComponent<NetworkObject>();
                obj.SpawnWithOwnership(m_NetworkManager.LocalClientId);
                Debug.Log("Spawned dynamic prefab");
            }
        }

        /// <summary>
        /// This call preloads the dynamic prefab on the server and sends a client rpc to all the clients to do the same.
        /// </summary>
        /// <param name="guid"></param>
        public async Task PreloadDynamicPrefabOnServerAndStartLoadingOnAllClients(string guid)
        {
            if (IsServer)
            {
                var assetGuid = new AddressableGUID()
                {
                    Value = guid
                };
                
                if (m_LoadedDynamicPrefabResourceHandles.ContainsKey(assetGuid))
                {
                    Debug.Log("Prefab is already loaded by all peers");
                    return;
                }
                
                Debug.Log("Loading dynamic prefab on the clients...");
                LoadAddressableClientRpc(assetGuid);
                await LoadDynamicPrefab(assetGuid);
            }
        }
        
        /// <summary>
        /// This call spawns an addressable prefab by it's guid. It does not ensure that all the clients have loaded the prefab before spawning it.
        /// All spawned objects are invisible to clients that don't have the prefab loaded.
        /// The server tells the clients that lack the preloaded prefab to load it and acknowledge that they've loaded it,
        /// and then the server makes the object visible to that client.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public async Task<NetworkObject> SpawnWithVisibilitySystem(string guid)
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
                var prefab = await LoadDynamicPrefab(assetGuid);
                var obj = Instantiate(prefab).GetComponent<NetworkObject>();
                obj.SpawnWithOwnership(m_NetworkManager.LocalClientId);
                
                if(m_PrefabHashToNetworkObjectId.TryGetValue(assetGuid.GetHashCode(), out var networkObjectIds))
                {
                    networkObjectIds.Add(obj.NetworkObjectId);
                }
                else
                {
                    m_PrefabHashToNetworkObjectId.Add(assetGuid.GetHashCode(), new HashSet<ulong>() {obj.NetworkObjectId});
                }

                obj.CheckObjectVisibility = (clientId) => 
                {
                    //if the client has already loaded the prefab - we can make the object visible to them
                    if (HasClientLoadedPrefab(clientId, assetGuid.GetHashCode()))
                    {
                        return true;
                    }
                    //otherwise the clients need to load the prefab, and after they ack - the ShowHiddenObjectsToClient 
                    LoadAddressableClientRpc(assetGuid, new ClientRpcParams(){Send = new ClientRpcSendParams(){TargetClientIds = new ulong[]{clientId}}});
                    return false;
                };
                
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
                await LoadDynamicPrefab(assetGuid);
                Debug.Log("Client loaded dynamic prefab");
                AcknowledgeSuccessfulPrefabLoadServerRpc(assetGuid.GetHashCode());
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void AcknowledgeSuccessfulPrefabLoadServerRpc(int perfabHash, ServerRpcParams rpcParams = default)
        {
            m_CountOfClientsThatLoadedThePrefab++;
            Debug.Log("Client acknowledged successful prefab load with hash: " + perfabHash);
            RecordThatClientHasLoadedAPrefab(perfabHash, rpcParams.Receive.SenderClientId);
            ShowHiddenObjectsToClient(perfabHash, rpcParams.Receive.SenderClientId);
        }

        void ShowHiddenObjectsToClient(int prefabHash, ulong clientId)
        {
            if(m_PrefabHashToNetworkObjectId.TryGetValue(prefabHash, out var networkObjectIds))
            {
                foreach (var networkObjectId in networkObjectIds)
                {
                    var obj = m_NetworkManager.SpawnManager.SpawnedObjects[networkObjectId];
                    if (!obj.IsNetworkVisibleTo(clientId))
                    {
                        obj.NetworkShow(clientId);
                    }
                }
            }
        }

        async Task<IList<GameObject>> LoadDynamicPrefabs(AddressableGUIDCollection addressableGUIDCollection)
        {
            var tasks = new List<Task<GameObject>>();

            foreach (var guid in addressableGUIDCollection.GUIDs)
            {
                tasks.Add( LoadDynamicPrefab(guid, recomputeHash:false));
            }
            
            var prefabs = await Task.WhenAll(tasks);
            CalculateDynamicPrefabArrayHash();
            
            return prefabs;
        }

        
        async Task<GameObject> LoadDynamicPrefab(AddressableGUID guid, bool recomputeHash = true)
        {
            if(m_LoadedDynamicPrefabResourceHandles.ContainsKey(guid))
            {
                Debug.Log($"Prefab has already been loaded, skipping loading this time | {guid}");
                return m_LoadedDynamicPrefabResourceHandles[guid].Result;
            }
            
            Debug.Log($"Loading dynamic prefab {guid.Value}");
            var op = Addressables.LoadAssetAsync<GameObject>(guid.ToString());
            var prefab = await op.Task;
            m_NetworkManager.AddNetworkPrefab(prefab);
            m_LoadedDynamicPrefabResourceHandles.Add(guid, op);
            
            if(recomputeHash)
            {
                CalculateDynamicPrefabArrayHash();
            }

            return prefab;
        }


        void CalculateDynamicPrefabArrayHash()
        {
            //we need to sort the array so that the hash is consistent across clients
            //it's possible to use an order-independent hashing algorithm for some potential performance gains
            m_DynamicPrefabGUIDs.Clear();
            m_DynamicPrefabGUIDs.AddRange(m_LoadedDynamicPrefabResourceHandles.Keys);
            m_DynamicPrefabGUIDs.Sort((a, b) => a.Value.CompareTo(b.Value));
            m_HashOfDynamicPrefabGUIDs = k_EmptyDynamicPrefabHash;
            
            //a simple hash combination algorithm suggested by Jon Skeet,
            //found here: https://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
            //we can't use C# HashCode combine because it is unreliable across different processes (by design)
            unchecked
            {
                int hash = 17;
                for (var i = 0; i < m_DynamicPrefabGUIDs.Count; ++i)
                {
                    hash = hash * 31 + m_DynamicPrefabGUIDs[i].GetHashCode();
                }

                m_HashOfDynamicPrefabGUIDs = hash;
            }

            Debug.Log($"Calculated hash of dynamic prefabs: {m_HashOfDynamicPrefabGUIDs}");
        }

        public void UnloadAndReleaseAllDynamicPrefabs()
        {
            m_HashOfDynamicPrefabGUIDs = k_EmptyDynamicPrefabHash;
            
            foreach (var handle in m_LoadedDynamicPrefabResourceHandles.Values)
            {
                m_NetworkManager.RemoveNetworkPrefab(handle.Result);
                Addressables.Release(handle);
            }
            
            m_LoadedDynamicPrefabResourceHandles.Clear();
        }
    }
}
