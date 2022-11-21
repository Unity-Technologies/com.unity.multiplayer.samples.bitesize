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
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game
{
    public enum ConnectStatus
    {
        Undefined,
        //Success,                  //client successfully connected. This may also be a successful reconnect.
        ClientNeedsToPreload,     //client needs to preload the dynamic prefabs before connecting
        // ServerFull,               //can't join, server is already at capacity.
        // LoggedInAgain,            //logged in on a separate client, causing this one to be kicked out.
        // UserRequestedDisconnect,  //Intentional Disconnect triggered by the user.
        // GenericDisconnect,        //server disconnected, but no specific reason given.
        // Reconnecting,             //client lost connection and is attempting to reconnect.
        // IncompatibleBuildType,    //client build type is incompatible with server.
        // HostEndedSession,         //host intentionally ended the session.
        // StartHostFailed,          // server failed to bind
        // StartClientFailed         // failed to connect to server and/or invalid network endpoint
    }

    public struct AddressableGUID : INetworkSerializable
    {
        public FixedString128Bytes Value;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public struct AddressableGUIDCollection : INetworkSerializable
    {
        public AddressableGUID[] GUIDs;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // Length
            int length = 0;
            if (!serializer.IsReader)
            {
                length = GUIDs.Length;
            }

            serializer.SerializeValue(ref length);

            // Array
            if (serializer.IsReader)
            {
                GUIDs = new AddressableGUID[length];
            }

            for (int n = 0; n < length; ++n)
            {
                serializer.SerializeValue(ref GUIDs[n]);
            }
        }
        
        public override int GetHashCode()
        {
            int value = 0;
            for (var i = 0;i< this.GUIDs.Length; i++)
            {
                value = HashCode.Combine(this.GUIDs[i],value);
            }

            return value;
        }

        public unsafe int GetSizeInBytes()
        {
            return sizeof(AddressableGUID) * GUIDs.Length;
        }
    }
    
            
    [Serializable]
    public class ConnectionPayload
    {
        public int HashOfDynamicPrefabGUIDs;
    }

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
    public sealed class SparseLoadingSample : NetworkBehaviour
    {
        const int k_MaxConnectPayload = 1024;
        const int k_EmptyDynamicPrefabHash = -1;
        ushort m_Port = 7777;
        string m_ConnectAddress = "127.0.0.1";

        [SerializeField] GameObject m_ConnectionUI;
        [FormerlySerializedAs("m_StartGameButton")] [SerializeField] Button m_SpawnButton;
        [SerializeField] AssetReferenceGameObject m_DynamicPrefabRef;
        [SerializeField] float m_SpawnTimeoutInSeconds;
        [SerializeField] NetworkManager m_NetworkManager;
        
        int m_CountOfClientsThatLoadedThePrefab = 0;
        float m_SpawnTimeoutTimer = 0;
        
        int m_HashOfDynamicPrefabGUIDs = k_EmptyDynamicPrefabHash;
        Dictionary<AddressableGUID, AsyncOperationHandle<GameObject>> m_LoadedDynamicPrefabResourceHandles = new Dictionary<AddressableGUID, AsyncOperationHandle<GameObject>>();
        List<AddressableGUID> m_DynamicPrefabGUIDs = new List<AddressableGUID>(); //cached list to avoid GC
        
        public void StartClient()
        {
            Debug.Log(nameof(StartClient));
            
            m_NetworkManager.NetworkConfig.ForceSamePrefabs = false;
            
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                HashOfDynamicPrefabGUIDs = m_HashOfDynamicPrefabGUIDs
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            m_NetworkManager.NetworkConfig.ConnectionData = payloadBytes;
            m_NetworkManager.StartClient();
            m_NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(nameof(ReceiveServerToClientSetDisconnectReason_CustomMessage), ReceiveServerToClientSetDisconnectReason_CustomMessage);
            m_ConnectionUI.SetActive(false);
        }

        public void StartHost()
        {
            Debug.Log(nameof(StartHost));
            
            m_NetworkManager.NetworkConfig.ForceSamePrefabs = false;
            var transport = m_NetworkManager.GetComponent<UnityTransport>();
            transport.SetConnectionData(m_ConnectAddress, m_Port);
            m_NetworkManager.ConnectionApprovalCallback = ConnectionApprovalCallback;
            m_NetworkManager.StartHost();
            m_ConnectionUI.SetActive(false);
        }

        public override void OnDestroy()
        {
            UnloadAndReleaseAllDynamicPrefabs();
            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                m_SpawnButton.onClick.AddListener(OnClickedSpawnButton);
            }
            else
            {
                m_SpawnButton.gameObject.SetActive(false);
            }
        }

        async void ReceiveServerToClientSetDisconnectReason_CustomMessage(ulong clientID, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ConnectStatus status);

            switch (status)
            {
                case ConnectStatus.Undefined:
                    Debug.Log("Undefined");
                    m_NetworkManager.Shutdown();
                    m_ConnectionUI.SetActive(true);
                    break;
                case ConnectStatus.ClientNeedsToPreload:
                {
                    m_NetworkManager.Shutdown();
                    Debug.Log("Client needs to preload");
                    reader.ReadValueSafe(out AddressableGUIDCollection addressableGUIDCollection);
                    await LoadDynamicPrefabs(addressableGUIDCollection);
                    Debug.Log("Restarting client");
                    StartClient();
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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

        async void OnClickedSpawnButton()
        {
            m_SpawnButton.gameObject.SetActive(false);
                
            bool didManageToSpawn = await TrySpawnDynamicPrefab(m_DynamicPrefabRef.AssetGUID);

            if (!didManageToSpawn)
            {
                m_SpawnButton.gameObject.SetActive(true);
            }
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
            
            if (clientPrefabHash == serverPrefabHash)
            {
                //if the client has the same prefabs as the server - approve the connection
                Approve();
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

        //todo: return the reason for failure, not just a true/false
        //and write up some ideas on how to deal with different kinds of failures (user timeout - to maybe disconnect the client?)
        //
        //we can try and use network visibility to prevent the client that failed to load a prefab from seeing it
        //
        //we probably should disconnect the user that fails to load the prefab - that's the simplest scenario, however a retry is also possible 
        //
        //user can ask for more time to load, or respond with I'm dead, kick me message
        
        //todo: add a sammple that shows stand-ins (a prefab that is loaded somehow (dynamically or otherwise) and then it's internal graphics are replaced with a lioaded addressable - but it's fully functional from the code perspective)
        
        
        //right now if player C if failing to load - all the players are stalled, this is not ideal
        
        //todo: store a dict of native strings to prefabs vs c# strings
        
        async Task<bool> TrySpawnDynamicPrefab(string guid)
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
                AcknowledgeSuccessfulPrefabLoadServerRpc();
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        void AcknowledgeSuccessfulPrefabLoadServerRpc(ServerRpcParams rpcParams = default)
        {
            m_CountOfClientsThatLoadedThePrefab++;
            Debug.Log("Client acknowledged successful prefab load");
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
