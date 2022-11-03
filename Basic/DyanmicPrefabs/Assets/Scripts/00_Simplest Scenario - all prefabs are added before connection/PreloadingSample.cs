using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace Game
{
    public sealed class PreloadingSample : MonoBehaviour
    {
        [SerializeField] AssetReferenceGameObject m_DynamicPrefabRef;
        [SerializeField] NetworkManager _networkManager;
        
        void Awake()
        {
            //this is the simplest case of a dynamic prefab - we just add it to the list of prefabs before we start the server
            //at this point we can also easily change the PlayerPrefab
            //
            //what's important is that it doesn't really matter where the prefab comes from,
            //it could be a simple prefab or it could be an addressable - it's all the same
            //
            //it's important to note that this isn't limited to PlayerPrefab, despite the method name
            //you can add any prefab to the list of prefabs that will be spawned
            
            PreloadDynamicPlayerPrefab();

        }

        async void PreloadDynamicPlayerPrefab()
        {
            var op =  Addressables.LoadAssetAsync<GameObject>(m_DynamicPrefabRef);
            var prefab = await op.Task;
            Addressables.Release(op);
            
            _networkManager.NetworkConfig.ForceSamePrefabs = true;
            _networkManager.AddNetworkPrefab(prefab);
            _networkManager.NetworkConfig.PlayerPrefab = prefab;
        }
    }
}
