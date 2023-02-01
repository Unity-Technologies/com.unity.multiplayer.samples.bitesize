using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Game
{
    public partial class DynamicPrefabManager
    {
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
    }
}
