using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Game
{
    public partial class DynamicPrefabManager
    {
        /// <summary>
        /// This call spawns an addressable prefab by it's guid. It does not ensure that all the clients have loaded the prefab before spawning it.
        /// All spawned objects are invisible to clients that don't have the prefab loaded.
        /// The server tells the clients that lack the preloaded prefab to load it and acknowledge that they've loaded it,
        /// and then the server makes the object visible to that client.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public async Task<NetworkObject> SpawnImmediatelyAndHideUntilPrefabIsLoadedOnClient(string guid, Vector3 position, Quaternion rotation)
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
                var obj = Instantiate(prefab, position, rotation).GetComponent<NetworkObject>();
                
                if(m_PrefabHashToNetworkObjectId.TryGetValue(assetGuid.GetHashCode(), out var networkObjectIds))
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
                    if (HasClientLoadedPrefab(clientId, assetGuid.GetHashCode()))
                    {
                        return true;
                    }
                    //otherwise the clients need to load the prefab, and after they ack - the ShowHiddenObjectsToClient 
                    LoadAddressableClientRpc(assetGuid, new ClientRpcParams(){Send = new ClientRpcSendParams(){TargetClientIds = new ulong[]{clientId}}});
                    return false;
                };
                
                obj.SpawnWithOwnership(m_NetworkManager.LocalClientId);

                return obj;
            }
        }
    }
}
