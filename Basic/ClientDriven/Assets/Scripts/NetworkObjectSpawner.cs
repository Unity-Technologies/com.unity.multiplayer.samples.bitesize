using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.ClientDriven
{
    /// <summary>
    /// Spawn a NetworkObject at this transform's position when NetworkManager's server is started.
    /// </summary>
    /// <remarks>
    /// A NetworkManager is expected to be part of the scene that this NetworkObject is a part of.
    /// </remarks>
    class NetworkObjectSpawner : MonoBehaviour
    {
        [SerializeField]
        NetworkObject m_PrefabReference;

        void Start()
        {
            Debug.Assert(NetworkManager.Singleton != null, "A NetworkManager is likely not a part of this MonoBehaviour's scene.");
            if (NetworkManager.Singleton == null)
            {
                return;
            }

            NetworkManager.Singleton.OnServerStarted += SpawnIngredient;
        }

        void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= SpawnIngredient;
            }
        }

        void SpawnIngredient()
        {
            // Note: this will be converted to NetworkObject.InstantiateAndSpawn(), but a current limitation on Netcode
            // for GameObjects invoking this method on OnServerStarted prevents this API upgrade.
            // Specifically, if you were to spawn a Rigidbody with Rigidbody Interpolation enabled, you would need to
            // update the Rigidbody's position immediately after invoking NetworkObject.InstantitateAndSpawn().
            NetworkObject instantiatedNetworkObject = Instantiate(m_PrefabReference, transform.position, transform.rotation, null);
            instantiatedNetworkObject.Spawn();
        }
    }
}
