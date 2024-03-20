using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public class EnemyAgent : NetworkBehaviour
{
    private const float k_ShootingRandomThreshold = 0.996f;
    private const float k_ShootTimer = 1.25f;
    [Header("Enemy Settings")]
    public int score = 50;
    public GameObject bulletPrefab;
    public float GraceShootingPeriod = 1.0f; // A period of time in which the enemy will not shoot at the start

    public bool canShoot { get; set; }
    public float column { get; private set; }
    public float row { get; private set; }

    private float m_ShootTimer = 0.0f;
    private float m_FirstShootTimeAfterSpawn = 0.0f;

    public void Awake()
    {
        canShoot = false;
        m_FirstShootTimeAfterSpawn = Single.PositiveInfinity;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            canShoot = false;
            if (score == 100)
                return;

            m_FirstShootTimeAfterSpawn =
                Time.time + Random.Range(GraceShootingPeriod - 0.1f, GraceShootingPeriod + 0.75f);

            Assert.IsNotNull(InvadersGame.Singleton);
            InvadersGame.Singleton.RegisterSpawnableObject(InvadersObjectType.Enemy, gameObject);
            InvadersGame.Singleton.isGameOver.OnValueChanged += OnGameOver;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (!InvadersGame.Singleton) return;

        if (IsServer) InvadersGame.Singleton.UnregisterSpawnableObject(InvadersObjectType.Enemy, gameObject);
        InvadersGame.Singleton.isGameOver.OnValueChanged -= OnGameOver;
    }

    private void Update()
    {
        if (Time.time <= m_FirstShootTimeAfterSpawn)
        {
            // Wait for the grace shooting period to pass
            return;
        }
        
        bool bCanShootThisFrame = false;
        if (IsServer && canShoot)
            if (Random.Range(0, 1.0f) > k_ShootingRandomThreshold)
                bCanShootThisFrame = true;

        if (m_ShootTimer > 0)
            m_ShootTimer -= Time.deltaTime;
        else
        {
            if (!bCanShootThisFrame) return;
            m_ShootTimer = Random.Range(k_ShootTimer - 0.05f, k_ShootTimer + 0.25f);
            SpawnBullet();
            return;
        }
    }

    private void SpawnBullet()
    {
        var myBullet = Instantiate(bulletPrefab, transform.position - Vector3.up, Quaternion.identity);
        myBullet.GetComponent<NetworkObject>().Spawn();
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (!IsServer) return;

        var hitShield = collider.gameObject.GetComponent<Shield>();
        if (hitShield != null) Destroy(hitShield.gameObject);
    }

    private void OnGameOver(bool oldValue, bool newValue)
    {
        // Is there anything we need to add in here?
        enabled = false;
    }

    public void Setup(float column, float row)
    {
        this.column = column;
        this.row = row;
    }
}
