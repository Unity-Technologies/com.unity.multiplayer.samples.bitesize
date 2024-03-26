using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;
using System.Collections;

namespace Unity.Multiplayer.Samples.ClientDriven
{
    /// <summary>
    /// Spawn a NetworkObject at this transform's position when NetworkManer's server is started.
    /// </summary>
    /// <remarks>
    /// A NetworkManager is expected to be part of the scene that this NetworkObject is a part of.
    /// </remarks>
    internal class NetworkObjectSpawner : MonoBehaviour
    {
        [SerializeField]
        NetworkObject prefabReference;

        void Start()
        {
            if (NetworkManager.Singleton == null && NetworkManager.Singleton.IsServer)
            {
                StartCoroutine(NetworkManagerCoroutine());
            }

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted += OnServerStartedIngredientSpawn;
            }
        }

        IEnumerator NetworkManagerCoroutine()
        {
            Debug.Log("NetworkManager not here...");
            yield return new WaitUntil(() => NetworkManager.Singleton != null);
            Debug.Log("NetworkManager is here!");
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
            NetworkObject instantiatedNetworkObject = Instantiate(prefabReference, transform.position, transform.rotation, null);
            ServerIngredient ingredient = instantiatedNetworkObject.GetComponent<ServerIngredient>();
            ingredient.NetworkObject.Spawn();
        }
    }
}
