using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Preloading
{
    /// <summary>
    /// This is the simplest case of a dynamic prefab - we just add it to the list of prefabs before we start the server
    /// at this point we can also easily change the PlayerPrefab. What's important is that it doesn't really matter
    /// where the prefab comes from, it could be a simple prefab or it could be an Addressable - it's all the same. 
    /// </summary>
    public sealed class PreloadingSample : MonoBehaviour
    {
        [SerializeField] AssetReferenceGameObject m_DynamicPrefabReference;
        
        [SerializeField] NetworkManager m_NetworkManager;
        
        async void Start()
        {
            await PreloadDynamicPlayerPrefab();
            //after we've waited for the prefabs to load - we can start the host or the client
        }

        // It's important to note that this isn't limited to PlayerPrefab, despite the method name you can add any
        // prefab to the list of prefabs that will be spawned.
        async Task PreloadDynamicPlayerPrefab()
        {
            Debug.Log($"Started to load addressable with GUID: {m_DynamicPrefabReference.AssetGUID}");
            var op =  Addressables.LoadAssetAsync<GameObject>(m_DynamicPrefabReference);
            var prefab = await op.Task;
            Addressables.Release(op);
            
            m_NetworkManager.NetworkConfig.ForceSamePrefabs = true;
            //it's important to actually add the player prefab to the list of network prefabs - it doesn't happen
            //automatically
            m_NetworkManager.AddNetworkPrefab(prefab);
            m_NetworkManager.NetworkConfig.PlayerPrefab = prefab;
            Debug.Log($"Loaded prefab has been assigned to NetworkManager's PlayerPrefab");
        }
    }
}
