using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class Powerup : NetworkBehaviour
{
    static string s_ObjectPoolTag = "ObjectPool";

    public static int numPowerups = 0;
    
    NetworkObjectPool m_ObjectPool;

    public NetworkVariable<Buff.BuffType> buffType = new NetworkVariable<Buff.BuffType>();

    void Awake()
    {
        m_ObjectPool = GameObject.FindWithTag(s_ObjectPoolTag).GetComponent<NetworkObjectPool>();
        Assert.IsNotNull(m_ObjectPool, $"{nameof(NetworkObjectPool)} not found in scene. Did you apply the {s_ObjectPoolTag} to the GameObject?");
    }
    
    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            OnStartClient();
        }

        if (IsServer)
        {
            OnStartServer();
        }
    }

    void OnStartClient()
    {
        float dir = 170.0f;
        transform.rotation = Quaternion.Euler(0, 180, dir);
        GetComponent<Rigidbody2D>().angularVelocity = dir;

        Color color = Buff.bufColors[(int)buffType.Value];
        GetComponent<Renderer>().material.color = color;

        if (!IsServer)
        {
            numPowerups += 1;
        }
    }

    void OnStartServer()
    {
        numPowerups += 1;
    }

    void OnGUI()
    {
        GUI.color = Buff.bufColors[(int)buffType.Value];
        Vector3 pos = Camera.main.WorldToScreenPoint(transform.position);
        GUI.Label(new Rect(pos.x - 20, Screen.height - pos.y - 30, 100, 30), buffType.Value.ToString());
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer)
        {
            return;
        }

        var otherShipControl = other.gameObject.GetComponent<ShipControl>();
        if (otherShipControl != null)
        {
            otherShipControl.AddBuff(buffType.Value);
            DestroyPowerUp();
        }
    }

    void DestroyPowerUp()
    {
        AudioSource.PlayClipAtPoint(GetComponent<AudioSource>().clip, transform.position);
        numPowerups -= 1;
       
        NetworkObject.Despawn(true);
    }
}
