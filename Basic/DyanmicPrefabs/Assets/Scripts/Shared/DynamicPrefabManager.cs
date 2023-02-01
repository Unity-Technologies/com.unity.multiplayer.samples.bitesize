using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game
{
    //Assumption: Addressables are loadable, ie when the client tries to load it - it will not fail.

    //todo: improvement ideas:
    // - it's possible to have more advanced logic that would for instance kick players that are consistently failing to load an addressable
    // - addressable guid list could be compressed before being sent
    // - instead of addressable guids the peers could exchange a `short` index that would refer to Addressables in some kind of a list stored in a scriptable object. That would reduce the amount of data that's being exchanged quite drastically.
    
    //todo: if/when there is a sample that shows how to load addressable scenes
    //- we probably should add some logic to NetworkSceneManager that would allow us to use Addressables scene loading
    
    //this sample does not cover the case of addressable usage when the client is loading custom visual prefabs and swapping out the rendering object for essentially non-dynamic prefabs

    public partial class DynamicPrefabManager : NetworkBehaviour
    {
        const int k_MaxConnectPayload = 1024;
        
        const int k_EmptyDynamicPrefabHash = -1;
        
        [SerializeField] float m_SpawnTimeoutInSeconds;
        
        [SerializeField] NetworkManager m_NetworkManager;

        [SerializeField]
        int m_ArtificialDelayMilliseconds = 1000;
        
        int m_SynchronousSpawnAckCount = 0;
        
        float m_SynchronousSpawnTimeoutTimer = 0;
        
        int m_HashOfDynamicPrefabGUIDs = k_EmptyDynamicPrefabHash;
        
        Dictionary<AddressableGUID, AsyncOperationHandle<GameObject>> m_LoadedDynamicPrefabResourceHandles = new Dictionary<AddressableGUID, AsyncOperationHandle<GameObject>>();
        
        List<AddressableGUID> m_DynamicPrefabGUIDs = new List<AddressableGUID>(); //cached list to avoid GC

        //A storage where we keep the association between the dynamic prefab (hash of it's GUID) and the clients that have it loaded
        Dictionary<int, HashSet<ulong>> m_PrefabHashToClientIds = new Dictionary<int, HashSet<ulong>>();
        
        //A storage where we keep association between prefab (hash of it's GUID) and the spawned network objects that use it
        Dictionary<int, HashSet<NetworkObject>> m_PrefabHashToNetworkObjectId = new Dictionary<int, HashSet<NetworkObject>>();
        
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

        public byte[] GenerateRequestPayload()
        {
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                HashOfDynamicPrefabGUIDs = m_HashOfDynamicPrefabGUIDs
            });
            
            return System.Text.Encoding.UTF8.GetBytes(payload);
        }

        public override void OnDestroy()
        {
            UnloadAndReleaseAllDynamicPrefabs();
            base.OnDestroy();
        }
        
        public void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
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
            
            // In order for clients to not just get disconnected with no feedback, the server needs to tell the client
            // why it disconnected it. This could happen after an auth check on a service or because of gameplay
            // reasons (server full, wrong build version, etc).
            // The server can do so via the DisconnectReason in the ConnectionApprovalResponse. The guids of the prefabs
            // the client will need to load will be sent, such that the client loads the needed prefabs, and reconnects.
            
            // A note: DisconnectReason will not be written to if the string is too large in size. This should be used
            // only to tell the client "why" it failed -- the client should instead use services like UGS to fetch the
            // relevant data it needs to fetch & download.
            
            m_DynamicPrefabGUIDs.Clear();
            m_DynamicPrefabGUIDs.AddRange(m_LoadedDynamicPrefabResourceHandles.Keys);
            
            var rejectionPayload = new DisconnectionPayload()
            {
                reason = DisconnectReason.ClientNeedsToPreload,
                guids = m_DynamicPrefabGUIDs.Select(item => item.ToString()).ToList()
            };

            response.Reason = JsonUtility.ToJson(rejectionPayload);
            ImmediateDeny();
            
            // A note: sending large strings through Netcode is not ideal -- you'd usually want to use REST services to
            // accomplish this instead. UGS services like Lobby can be a useful alternative. Another route may be to
            // set ConnectionApprovalResponse's Pending flag to true, and send a CustomMessage containing the array of 
            // GUIDs to a client, which the client would load and reattempt a reconnection.

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
        void AcknowledgeSuccessfulPrefabLoadServerRpc(int prefabHash, ServerRpcParams rpcParams = default)
        {
            m_SynchronousSpawnAckCount++;
            Debug.Log("Client acknowledged successful prefab load with hash: " + prefabHash);
            RecordThatClientHasLoadedAPrefab(prefabHash, rpcParams.Receive.SenderClientId);
           
            //the server has all the objects visible, no need to do anything
            if (rpcParams.Receive.SenderClientId != m_NetworkManager.LocalClientId)
            {
                ShowHiddenObjectsToClient(prefabHash, rpcParams.Receive.SenderClientId);
            }
        }

        void ShowHiddenObjectsToClient(int prefabHash, ulong clientId)
        {
            if(m_PrefabHashToNetworkObjectId.TryGetValue(prefabHash, out var networkObjects))
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

        public async Task<IList<GameObject>> LoadDynamicPrefabs(AddressableGUIDCollection addressableGUIDCollection)
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
            if (m_LoadedDynamicPrefabResourceHandles.ContainsKey(guid))
            {
                Debug.Log($"Prefab has already been loaded, skipping loading this time | {guid}");
                return m_LoadedDynamicPrefabResourceHandles[guid].Result;
            }
            
            Debug.Log($"Loading dynamic prefab {guid.Value}");
            var op = Addressables.LoadAssetAsync<GameObject>(guid.ToString());
            var prefab = await op.Task;

            #if DEBUG
            //this delay here is to make it obvious how different loading strategies differ
            //artificial latency would also highlight the difference
            await Task.Delay(m_ArtificialDelayMilliseconds);
            #endif

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
