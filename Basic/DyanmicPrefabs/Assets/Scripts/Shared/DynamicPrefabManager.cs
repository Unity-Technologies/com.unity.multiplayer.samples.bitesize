using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;

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

    public class DynamicPrefabManager : NetworkBehaviour
    {
        public override void OnDestroy()
        {
            //UnloadAndReleaseAllDynamicPrefabs();
            base.OnDestroy();
        }
        
        /*[ClientRpc]
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
        }*/

        [ServerRpc(RequireOwnership = false)]
        void AcknowledgeSuccessfulPrefabLoadServerRpc(int prefabHash, ServerRpcParams rpcParams = default)
        {
            /*m_SynchronousSpawnAckCount++;
            Debug.Log("Client acknowledged successful prefab load with hash: " + prefabHash);
            RecordThatClientHasLoadedAPrefab(prefabHash, rpcParams.Receive.SenderClientId);
           
            //the server has all the objects visible, no need to do anything
            if (rpcParams.Receive.SenderClientId != NetworkManager.Singleton.LocalClientId)
            {
                ShowHiddenObjectsToClient(prefabHash, rpcParams.Receive.SenderClientId);
            }*/
        }

        /*public async Task<IList<GameObject>> LoadDynamicPrefabs(AddressableGUIDCollection addressableGUIDCollection)
        {
            var tasks = new List<Task<GameObject>>();

            foreach (var guid in addressableGUIDCollection.GUIDs)
            {
                tasks.Add( LoadDynamicPrefab(guid, recomputeHash:false));
            }
            
            var prefabs = await Task.WhenAll(tasks);
            CalculateDynamicPrefabArrayHash();
            
            return prefabs;
        }*/

        // could keep it here
        /*async Task<GameObject> LoadDynamicPrefab(AddressableGUID guid, bool recomputeHash = true)
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

            NetworkManager.Singleton.AddNetworkPrefab(prefab);
            m_LoadedDynamicPrefabResourceHandles.Add(guid, op);
            
            if(recomputeHash)
            {
                CalculateDynamicPrefabArrayHash();
            }

            return prefab;
        }*/

        /*public void UnloadAndReleaseAllDynamicPrefabs()
        {
            m_HashOfDynamicPrefabGUIDs = k_EmptyDynamicPrefabHash;
            
            foreach (var handle in m_LoadedDynamicPrefabResourceHandles.Values)
            {
                NetworkManager.Singleton.RemoveNetworkPrefab(handle.Result);
                Addressables.Release(handle);
            }
            
            m_LoadedDynamicPrefabResourceHandles.Clear();
        }*/
    }
}
