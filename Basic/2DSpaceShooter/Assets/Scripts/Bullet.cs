using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    bool m_Bounce;
    int m_Damage = 5;
    ShipControl m_Owner;
    NetworkObjectPool m_ObjectPool;

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
        GameObject ex = ObjectPool.SharedInstance.GetPooledObject();
        if (ex != null)
        {
            ex.transform.position = transform.position + new Vector3(0, 0, -2);
            ex.transform.rotation = Quaternion.identity;
            ex.SetActive(true);
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
            bulletRb.velocity = velocity;
            SetVelocityClientRpc(velocity);
        }
    }

    [ClientRpc]
    void SetVelocityClientRpc(Vector2 velocity)
    {
        if (!IsHost)
        {
            var bulletRb = GetComponent<Rigidbody2D>();
            bulletRb.velocity = velocity;
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
