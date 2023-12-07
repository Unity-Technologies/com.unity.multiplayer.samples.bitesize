using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class Asteroid : NetworkBehaviour
{
    static string s_ObjectPoolTag = "ObjectPool";
    
    public static int numAsteroids = 0;

    NetworkObjectPool m_ObjectPool;

    public NetworkVariable<int> Size = new NetworkVariable<int>(4);

    [SerializeField]
    private int m_NumCreates = 3;
    
    public GameObject asteroidPrefab;

    void Awake()
    {
        m_ObjectPool = GameObject.FindWithTag(s_ObjectPoolTag).GetComponent<NetworkObjectPool>();
        Assert.IsNotNull(m_ObjectPool, $"{nameof(NetworkObjectPool)} not found in scene. Did you apply the {s_ObjectPoolTag} to the GameObject?");
    }

    void Start()
    {
        numAsteroids += 1;
    }

    public override void OnNetworkSpawn()
    {
        var size = Size.Value;
        transform.localScale = new Vector3(size, size, size);
    }

    private void OnExplode()
    {
        numAsteroids -= 1;

        var newSize = Size.Value - 1;

        if (newSize > 0)
        {
            int num = Random.Range(1, m_NumCreates + 1);

            for (int i = 0; i < num; i++)
            {
                int dx = Random.Range(0, 4) - 2;
                int dy = Random.Range(0, 4) - 2;
                Vector3 diff = new Vector3(dx * 0.3f, dy * 0.3f, 0);

                var go = m_ObjectPool.GetNetworkObject(asteroidPrefab, transform.position + diff, Quaternion.identity);

                var asteroid = go.GetComponent<Asteroid>();
                asteroid.Size = new NetworkVariable<int>(newSize);
                asteroid.asteroidPrefab = asteroidPrefab;
#if NGO_DAMODE
                // Currently, we always need to spawn with ownership in DA mode (still contemplating how to handle this when the 
                // NetworkObject being spawned has no assigned NetworkManager yet).
                if (NetworkManager.DistributedAuthorityMode)
                {
                    go.GetComponent<NetworkObject>().SpawnWithOwnership(NetworkManager.LocalClientId);
                }
                else
                {
                    go.GetComponent<NetworkObject>().Spawn();
                }

                
#endif
                go.GetComponent<Rigidbody2D>().AddForce(diff * 10, ForceMode2D.Impulse);
            }
        }

        NetworkObject.Despawn(true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnExplodeServerRpc()
    {
        OnExplode();
    }

    public void Explode()
    {
        if (!IsSpawned)
        {
            return;
        }

#if NGO_DAMODE
        if (IsOwner)
        {
            OnExplode();
        }
        else
        {
            OnExplodeServerRpc();
        }
#else
        Assert.IsTrue(NetworkManager.IsServer);
#endif

    }
}
