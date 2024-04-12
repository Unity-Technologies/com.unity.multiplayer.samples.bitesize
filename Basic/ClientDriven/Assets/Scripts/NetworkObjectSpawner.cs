using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

namespace Unity.Multiplayer.Samples.ClientDriven
{
    /// <summary>
    /// Spawn a NetworkObject at this transform's position when NetworkManager's server is started.
    /// </summary>
    /// <remarks>
    /// A NetworkManager is expected to be part of the scene that this NetworkObject is a part of.
    /// </remarks>
    internal class NetworkObjectSpawner : MonoBehaviour
    {
        [SerializeField]
        NetworkObject m_PrefabReference;

        void Start()
        {
            Debug.Assert(NetworkManager.Singleton != null, "The NetworkManager needs to be referenced!");
            if (NetworkManager.Singleton == null)
            {
                return;
            }

            NetworkManager.Singleton.OnServerStarted += SpawnIngredient;
        }

        void OnDestroy()
        {
            if(NetworkManager.Singleton != null)
            { 
                NetworkManager.Singleton.OnServerStarted -= SpawnIngredient;
            }
        }

        void SpawnIngredient()
        {
            var randomGenerator = new Random();
            NetworkObject instantiatedNetworkObject = Instantiate(m_PrefabReference, transform.position, transform.rotation, null);
            var ingredient = instantiatedNetworkObject.GetComponent<ServerIngredient>();
            ingredient.NetworkObject.Spawn();
        }
    }
}
