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
        if (!IsSpawned)
        {
            return;
        }
        m_Owner = owner;
        m_Damage = damage;
        m_Bounce = bounce;
#if NGO_DAMODE

        if (IsOwner)
        {
            // This is bad code don't use invoke.
            Invoke(nameof(DestroyBullet), lifetime);
        }
#else
        if (IsServer)
#endif
        {
            // This is bad code don't use invoke.
            Invoke(nameof(DestroyBullet), lifetime);
        }
    }

    public override void OnNetworkDespawn()
    {
        // This is inefficient, the explosion object could be pooled.
#if NGO_DAMODE
        GameObject ex = Instantiate(explosionParticle, transform.position, Quaternion.identity);
#else
        GameObject ex = Instantiate(explosionParticle, transform.position + new Vector3(0, 0, -2), Quaternion.identity);
#endif
    }

    private void DestroyBullet()
    {
        if (!NetworkObject.IsSpawned)
        {
            return;
        }

        NetworkObject.DeferredDespawnTick = NetworkManager.ServerTime.Tick + 3;
        NetworkObject.Despawn(true);
    }

    public void SetVelocity(Vector2 velocity)
    {
        if (!IsSpawned)
        {
            return;
        }
#if NGO_DAMODE
        if (IsOwner)
#else
        if (IsServer)
#endif

        {
            var bulletRb = GetComponent<Rigidbody2D>();
            bulletRb.velocity = velocity;
            if (NetworkManager.DistributedAuthorityMode)
            {
                OnSetVelocity(velocity);
            }
            else
            {
                SetVelocityClientRpc(velocity);
            }
        }
    }

    private void OnSetVelocity(Vector2 velocity)
    {
        var bulletRb = GetComponent<Rigidbody2D>();
        bulletRb.velocity = velocity;
    }

    [ClientRpc]
    void SetVelocityClientRpc(Vector2 velocity)
    {
        if (!IsHost)
        {
            OnSetVelocity(velocity);
        }
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        var otherObject = other.gameObject;

#if NGO_DAMODE
        if (!NetworkObject.IsSpawned || !IsOwner)
        {
            return;
        }
#else
        if (!NetworkManager.Singleton.IsServer || !NetworkObject.IsSpawned)
        {
            return;
        }
#endif

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
#if NGO_DAMODE
                if (NetworkManager.DistributedAuthorityMode)
                {
                    shipControl.TakeDamageServerRpc(m_Damage);
                }
                else
                {
                    shipControl.TakeDamage(m_Damage);
                }
#else
                shipControl.TakeDamage(m_Damage);
#endif
                DestroyBullet();
            }
        }
    }
}
