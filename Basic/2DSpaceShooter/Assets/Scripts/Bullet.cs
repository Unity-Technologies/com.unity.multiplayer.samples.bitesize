using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    bool m_Bounce;
    int m_Damage = 5;
    ShipControl m_Owner;

    public void Config(ShipControl owner, int damage, bool bounce, float lifetime)
    {
        m_Owner = owner;
        m_Damage = damage;
        m_Bounce = bounce;
        if (IsServer)
        {
            StartCoroutine(BulletDestroyCoroutine(lifetime));
        }
    }

    IEnumerator BulletDestroyCoroutine(float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        DestroyBullet();
    }

    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            ParticleSystem explosionParticles = ExplosionsPool.s_Singleton.Pool.Get();
            explosionParticles.transform.position = transform.position;
            explosionParticles.Play();
        }
    }

    private void DestroyBullet()
    {
        if (!NetworkObject.IsSpawned)
        {
            return;
        }

        NetworkObject.Despawn(true);
    }

    public void SetVelocity(Vector2 velocity)
    {
        if (IsServer)
        {
            var bulletRb = GetComponent<Rigidbody2D>();
            bulletRb.linearVelocity = velocity;
            ClientSetVelocityRpc(velocity);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    void ClientSetVelocityRpc(Vector2 velocity)
    {
        if (!IsHost)
        {
            var bulletRb = GetComponent<Rigidbody2D>();
            bulletRb.linearVelocity = velocity;
        }
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
