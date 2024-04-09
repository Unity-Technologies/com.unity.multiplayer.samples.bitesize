using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

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
        private NetworkObject prefabReference;
        [SerializeField]
        private NetworkManager m_NetworkManager;

        void Start()
        {
            Debug.Assert(m_NetworkManager != null, "The NetworkManager needs to be referenced!");
            if (m_NetworkManager == null)
            {
                return;
            }

            m_NetworkManager.OnServerStarted += OnServerStartedIngredientSpawn;
        }

        void OnDestroy()
        {
            m_NetworkManager.OnServerStarted -= OnServerStartedIngredientSpawn;
        }

        void OnServerStartedIngredientSpawn()
        {
            Random randomGenerator = new Random();
            NetworkObject instantiatedNetworkObject = Instantiate(prefabReference, transform.position, transform.rotation, null);
            ServerIngredient ingredient = instantiatedNetworkObject.GetComponent<ServerIngredient>();
            ingredient.NetworkObject.Spawn();
        }
    }
}
