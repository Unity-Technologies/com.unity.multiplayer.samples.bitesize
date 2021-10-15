using System;
using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    bool m_Bounce;
    int m_Damage = 5;
    ShipControl m_Owner;

    public GameObject explosionParticle;

    public void Config(ShipControl owner, int damage, bool bounce, float lifetime)
    {
        m_Owner = owner;
        m_Damage = damage;
        m_Bounce = bounce;
        
        if (IsServer)
        {
            // This is bad code don't use invoke.
            Invoke(nameof(DestroyBullet), lifetime);
        }
    }

    public override void OnNetworkDespawn()
    {
        // This is inefficient, the explosion object could be pooled.
        GameObject ex = Instantiate(explosionParticle, transform.position + new Vector3(0, 0, -2), Quaternion.identity);
        Destroy(ex, 0.5f);
    }

    private void DestroyBullet()
    {
        if (!NetworkObject.IsSpawned)
        {
            return;
        }

        NetworkObject.Despawn(true);
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        var otherObject = other.gameObject;

        if (!NetworkManager.Singleton.IsServer || !NetworkObject.IsSpawned)
        {
            return;
        }

        if (otherObject.TryGetComponent<Asteroid>(out var asteroid))
        {
            asteroid.Explode();
            DestroyBullet();
            return;
        }

        if (m_Bounce == false && (otherObject.CompareTag("Wall") || otherObject.CompareTag("Obstacle")))
        {
            DestroyBullet();
        }

        if (otherObject.TryGetComponent<ShipControl>(out var shipControl))
        {
            if (shipControl != m_Owner)
            {
                shipControl.TakeDamage(m_Damage);
                DestroyBullet();
            }
        }
    }
}
