using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerBullet : NetworkBehaviour
{
    private const float k_YBoundary = 15.0f;
    public PlayerControl owner;

    [Header("Movement Settings")]
    [SerializeField]
    [Tooltip("The constant speed at which the Bullet travels")]
    private float m_TravelSpeed = 4.0f;

    [SerializeField]
    ParticleSystem m_EnemyExplosionParticle;

    void Awake()
    {
        enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        enabled = IsServer;
    }

    private void Update()
    {
        transform.Translate(0, m_TravelSpeed * Time.deltaTime, 0);

        if (transform.position.y > k_YBoundary)
        {
            NetworkObject.Despawn();
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        // several OnTriggerEnter2D calls may be invoked in the same frame (for different Colliders), so we check if
        // we're spawned to make sure we don't trigger hits for already despawned bullets
        if (!IsServer || !IsSpawned)
            return;

        var hitEnemy = collider.gameObject.GetComponent<EnemyAgent>();
        if (hitEnemy != null && owner != null)
        {
            owner.IncreasePlayerScore(hitEnemy.score);

            // Only the server can despawn a NetworkObject
            hitEnemy.NetworkObject.Despawn();
            
            SpawnExplosionVFXClientRPC(transform.position, Quaternion.identity);
            
            NetworkObject.Despawn();
            
            return;
        }

        var hitShield = collider.gameObject.GetComponent<Shield>();
        if (hitShield != null)
        {
            Destroy(hitShield.gameObject);
            NetworkObject.Despawn();
        }
    }

    [ClientRpc]
    void SpawnExplosionVFXClientRPC(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        // this instantiates at the position of the bullet, there is an offset in the Y axis on the 
        // particle systems in the prefab so it looks like it spawns in the middle of the enemy
        Instantiate(m_EnemyExplosionParticle, spawnPosition, spawnRotation);
    }
}
