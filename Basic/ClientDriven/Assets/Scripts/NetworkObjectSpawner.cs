using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

namespace Unity.Multiplayer.Samples.ClientDriven
{
    public class NetworkObjectSpawner : MonoBehaviour
    {
        [SerializeField] NetworkObject prefabReference;

        void Start()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted += OnServerStartedIngredientSpawn;
            }
        }

        void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= OnServerStartedIngredientSpawn;
            }
        }

        void OnServerStartedIngredientSpawn()
        {
            Random randomGenerator = new Random(); 
            NetworkObject instantiatedNetworkObject = Instantiate(prefabReference, transform.position, transform.rotation, null);
            ServerIngredient ingredient = instantiatedNetworkObject.GetComponent<ServerIngredient>();
            ingredient.NetworkObject.Spawn();
            ingredient.currentIngredientType.Value = (IngredientType)randomGenerator.Next((int)IngredientType.MAX);
        }
    }
}