using UnityEngine;
using MLAPI;
using MLAPI.Extensions;
using MLAPI.NetworkVariable;
using UnityEngine.Assertions;

public class Asteroid : NetworkBehaviour
{
    static string s_ObjectPoolTag = "ObjectPool";
    
    public static int numAsteroids = 0;

    NetworkObjectPool m_ObjectPool;

    public NetworkVariableInt Size = new NetworkVariableInt(4);

    [SerializeField]
    private int m_NumCreates = 3;

    void Awake()
    {
        m_ObjectPool = GameObject.FindWithTag(s_ObjectPoolTag).GetComponent<NetworkObjectPool>();
        Assert.IsNotNull(m_ObjectPool, $"{nameof(NetworkObjectPool)} not found in scene. Did you apply the {s_ObjectPoolTag} to the GameObject?");
    }

    // Use this for initialization
    void Start()
    {
        numAsteroids += 1;
    }

    public override void NetworkStart()
    {
        var size = Size.Value;
        transform.localScale = new Vector3(size, size, size);
    }

    public void Explode()
    {
        Assert.IsTrue(NetworkManager.IsServer);
        
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
                
                var go = m_ObjectPool.GetNetworkObject(NetworkObject.PrefabHash, transform.position + diff, Quaternion.identity);
                
                go.GetComponent<Asteroid>().Size.Value = newSize;
                go.GetComponent<NetworkObject>().Spawn();
                go.GetComponent<Rigidbody2D>().AddForce(diff * 10, ForceMode2D.Impulse);
            }
        }
        
        NetworkObject.Despawn();
        m_ObjectPool.ReturnNetworkObject(NetworkObject);
    }
}
