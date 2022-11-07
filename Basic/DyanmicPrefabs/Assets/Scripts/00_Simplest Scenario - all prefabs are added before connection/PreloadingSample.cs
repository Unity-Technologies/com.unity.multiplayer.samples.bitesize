using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game
{
    public sealed class PreloadingSample : MonoBehaviour
    {
        [SerializeField] AssetReferenceGameObject m_DynamicPrefabRef;
        [SerializeField] NetworkManager _networkManager;
        

         async void Start()
         {
             //this is the simplest case of a dynamic prefab - we just add it to the list of prefabs before we start the server
             //at this point we can also easily change the PlayerPrefab
             //
             //what's important is that it doesn't really matter where the prefab comes from,
             //it could be a simple prefab or it could be an addressable - it's all the same
             //
             //it's important to note that this isn't limited to PlayerPrefab, despite the method name
             //you can add any prefab to the list of prefabs that will be spawned

             await PreloadDynamicPlayerPrefab();

            //after we've waited for the prefabs to load - we can start the host or the client via the NetworkManagerHud component.

         }

        async Task PreloadDynamicPlayerPrefab()
        {
            var op =  Addressables.LoadAssetAsync<GameObject>(m_DynamicPrefabRef);
            var prefab = await op.Task;
            Addressables.Release(op);
            
            _networkManager.NetworkConfig.ForceSamePrefabs = true;
            //it's important to actually add the player prefab to the list of network prefabs - it doesn't happen automatically
            _networkManager.AddNetworkPrefab(prefab);
            _networkManager.NetworkConfig.PlayerPrefab = prefab;
        }
    }
}
