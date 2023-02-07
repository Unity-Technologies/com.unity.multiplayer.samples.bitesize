using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game
{
    public class DynamicPrefabLoadingUtilities
    {
        const int k_EmptyDynamicPrefabHash = -1;

        int m_HashOfDynamicPrefabGUIDs = k_EmptyDynamicPrefabHash;

        Dictionary<AddressableGUID, AsyncOperationHandle<GameObject>> m_LoadedDynamicPrefabResourceHandles = new Dictionary<AddressableGUID, AsyncOperationHandle<GameObject>>();
        
        List<AddressableGUID> m_DynamicPrefabGUIDs = new List<AddressableGUID>(); //cached list to avoid GC

        //A storage where we keep the association between the dynamic prefab (hash of it's GUID) and the clients that have it loaded
        Dictionary<int, HashSet<ulong>> m_PrefabHashToClientIds = new Dictionary<int, HashSet<ulong>>();

        public bool HasClientLoadedPrefab(ulong clientId, int prefabHash) => 
            m_PrefabHashToClientIds.TryGetValue(prefabHash, out var clientIds) && clientIds.Contains(clientId);

        public bool IsPrefabLoadedLocally(AddressableGUID assetGuid) => 
            m_LoadedDynamicPrefabResourceHandles.ContainsKey(assetGuid);

        public int LoadedPrefabCount => m_LoadedDynamicPrefabResourceHandles.Count;

        public int ServerPrefabHash => m_HashOfDynamicPrefabGUIDs;

        NetworkManager m_NetworkManager;

        public DynamicPrefabLoadingUtilities(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
        }

        public void RecordThatClientHasLoadedAllPrefabs(ulong clientId)
        {
            foreach (var dynamicPrefabGUID in m_DynamicPrefabGUIDs)
            {
                RecordThatClientHasLoadedAPrefab(dynamicPrefabGUID.GetHashCode(), clientId);
            }
        }
        
        public void RecordThatClientHasLoadedAPrefab(int assetGuidHash, ulong clientId)
        {
            if (m_PrefabHashToClientIds.TryGetValue(assetGuidHash, out var clientIds))
            {
                clientIds.Add(clientId);
            }
            else
            {
                m_PrefabHashToClientIds.Add(assetGuidHash, new HashSet<ulong>() { clientId });
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

        public string GenerateDisconnectionPayload()
        {
            var rejectionPayload = new DisconnectionPayload()
            {
                reason = DisconnectReason.ClientNeedsToPreload,
                guids = m_DynamicPrefabGUIDs.Select(item => item.ToString()).ToList()
            };
    
            return JsonUtility.ToJson(rejectionPayload);
        }
        
        public async Task<GameObject> LoadDynamicPrefab(AddressableGUID guid, int artificialDelayMilliseconds, 
            bool recomputeHash = true)
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
            await Task.Delay(artificialDelayMilliseconds);
#endif

            m_NetworkManager.AddNetworkPrefab(prefab);
            m_LoadedDynamicPrefabResourceHandles.Add(guid, op);
            
            if (recomputeHash)
            {
                CalculateDynamicPrefabArrayHash();
            }

            return prefab;
        }
        
        public async Task<IList<GameObject>> LoadDynamicPrefabs(AddressableGUIDCollection addressableGUIDCollection,
            int artificialDelaySeconds)
        {
            var tasks = new List<Task<GameObject>>();

            foreach (var guid in addressableGUIDCollection.GUIDs)
            {
                tasks.Add( LoadDynamicPrefab(guid, artificialDelaySeconds, recomputeHash:false));
            }
            
            var prefabs = await Task.WhenAll(tasks);
            CalculateDynamicPrefabArrayHash();
            
            return prefabs;
        }

        public void RefreshLoadedPrefabGuids()
        {
            m_DynamicPrefabGUIDs.Clear();
            m_DynamicPrefabGUIDs.AddRange(m_LoadedDynamicPrefabResourceHandles.Keys);
        }
        
        void CalculateDynamicPrefabArrayHash()
        {
            //we need to sort the array so that the hash is consistent across clients
            //it's possible to use an order-independent hashing algorithm for some potential performance gains
            RefreshLoadedPrefabGuids();
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
