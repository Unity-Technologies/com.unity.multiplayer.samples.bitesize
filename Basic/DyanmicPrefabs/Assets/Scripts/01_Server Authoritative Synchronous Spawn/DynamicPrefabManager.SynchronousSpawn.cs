using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Game
{
    public partial class DynamicPrefabManager
    {
        /// <summary>
        /// This call attempts to spawn a prefab by it's addressable guid - it ensures that all the clients have loaded the prefab before spawning it,
        /// and if the clients fail to acknowledge that they've loaded a prefab - the spawn will fail.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public async Task<(bool Success, NetworkObject Obj)> TrySpawnDynamicPrefabSynchronously(string guid, Vector3 position, Quaternion rotation)
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
                    var obj = await Spawn(assetGuid);
                    return (true, obj);
                }
                
                m_SynchronousSpawnAckCount = 0;
                m_SynchronousSpawnTimeoutTimer = 0;
                
                Debug.Log("Loading dynamic prefab on the clients...");
                LoadAddressableClientRpc(assetGuid);
                //load the prefab on the server, so that any late-joiner will need to load that prefab also
                await LoadDynamicPrefab(assetGuid);
                int requiredAcknowledgementsCount = IsHost ? m_NetworkManager.ConnectedClients.Count - 1 : m_NetworkManager.ConnectedClients.Count;
                
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
                var prefab = await LoadDynamicPrefab(assetGuid);
                var obj = Instantiate(prefab, position, rotation).GetComponent<NetworkObject>();
                obj.SpawnWithOwnership(m_NetworkManager.LocalClientId);
                Debug.Log("Spawned dynamic prefab");
                return obj;
            }
        }
    }
}
