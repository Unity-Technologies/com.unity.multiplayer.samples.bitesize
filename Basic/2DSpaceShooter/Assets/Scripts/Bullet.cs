using System;
using UnityEngine;
using MLAPI;
using MLAPI.Extensions;

public class Bullet : NetworkBehaviour
{
    NetworkObjectPool m_PoolToReturn;
    
    bool m_Bounce = false;
    int m_Damage = 5;
    ShipControl m_Owner;

    public GameObject explosionParticle;
    

    public void Config(ShipControl owner, int damage, bool bounce, float lifetime, NetworkObjectPool poolToReturn)
    {
        m_Owner = owner;
        m_Damage = damage;
        m_Bounce = bounce;
        m_PoolToReturn = poolToReturn;

        if (IsServer)
        {
            // This is bad code don't use invoke.
            Invoke(nameof(DestroyBullet), lifetime);
        }
    }

    private void DestroyBullet()
    {
        if (!NetworkObject.IsSpawned)
        {
            return;
        }
        
        Vector3 pos = transform.position;
        pos.z = -2;
        GameObject ex = Instantiate(explosionParticle, pos, Quaternion.identity);
        Destroy(ex, 0.5f);
        
        NetworkObject.Despawn();
        m_PoolToReturn.ReturnNetworkObject(NetworkObject);
    }
    
    void OnCollisionEnter2D(Collision2D other)
    {
        if (!NetworkManager.Singleton.IsServer || !NetworkObject.IsSpawned)
        {
            return;
        }

        var asteroid = other.gameObject.GetComponent<Asteroid>();
        if (asteroid != null)
        {
            asteroid.Explode();
            DestroyBullet();
        }

        var wall = other.gameObject.GetComponent<Wall>();
        var obstacle = other.gameObject.GetComponent<Obstacle>();
        if (m_Bounce == false && (wall != null || obstacle != null))
        {
            DestroyBullet();
        }

        var shipControl = other.gameObject.GetComponent<ShipControl>();
        if (shipControl != null)
        {
            if (shipControl != m_Owner)
            {
                shipControl.TakeDamage(m_Damage);
                DestroyBullet();
            }
        }
    }
}
