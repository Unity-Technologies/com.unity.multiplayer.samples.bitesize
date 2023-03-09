using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class EnemyBullet : NetworkBehaviour
{
    private const float k_YBoundary = -4.0f;

    // The constant speed at which the Bullet travels
    [Header("Movement Settings")]
    [SerializeField]
    [Tooltip("The constant speed at which the Bullet travels")]
    private float m_TravelSpeed = 3.0f;

    [SerializeField]
    ParticleSystem m_ShieldExplosionParticle;

    void Awake()
    {
        enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        enabled = IsServer;

        if (!IsServer)
        {
            return;
        }

        Assert.IsTrue(InvadersGame.Singleton);

        if (InvadersGame.Singleton)
        {
            InvadersGame.Singleton.isGameOver.OnValueChanged += OnGameOver;
        }
    }

    private void Update()
    {
        transform.Translate(0, -m_TravelSpeed * Time.deltaTime, 0);

        if (transform.position.y < k_YBoundary)
        {
            NetworkObject.Despawn();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (InvadersGame.Singleton)
        {
            InvadersGame.Singleton.isGameOver.OnValueChanged -= OnGameOver;
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        // several OnTriggerEnter2D calls may be invoked in the same frame (for different Colliders), so we check if
        // we're spawned to make sure we don't trigger hits for already despawned bullets
        if (!IsServer || !IsSpawned)
            return;

        var hitPlayer = collider.gameObject.GetComponent<PlayerControl>();
        if (hitPlayer != null)
        {
            NetworkObject.Despawn();
            hitPlayer.HitByBullet();
            return;
        }

        var hitShield = collider.gameObject.GetComponent<Shield>();
        if (hitShield != null)
        {
            SpawnExplosionVFXClientRpc(transform.position, Quaternion.identity);

            Destroy(hitShield.gameObject);
            NetworkObject.Despawn();
        }
    }

    [ClientRpc]
    void SpawnExplosionVFXClientRpc(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        Instantiate(m_ShieldExplosionParticle, spawnPosition, spawnRotation);
    }

    private void OnGameOver(bool oldValue, bool newValue)
    {
        enabled = false;

        // On game over destroy the bullets
        NetworkObject.Despawn();
    }
}
