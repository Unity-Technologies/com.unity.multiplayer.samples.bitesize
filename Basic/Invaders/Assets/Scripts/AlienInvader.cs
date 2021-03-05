using System;
using MLAPI;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public class AlienInvader : NetworkBehaviour
{
    private const float k_ShootingRandomThreshold = 0.996f;
    private const float k_ShootTimer = 0.75f;
    [Header("Alien Settings")]
    public int score = 50;
    public GameObject bulletPrefab;

    public bool canShoot { get; set; }
    public float column { get; private set; }
    public float row { get; private set; }

    private float m_ShootTimer = 0.0f;

    private void Update()
    {
        bool bCanShootThisFrame = false;
        if (IsServer && canShoot)
            if (Random.Range(0, 1.0f) > k_ShootingRandomThreshold)
                bCanShootThisFrame = true;

        if (m_ShootTimer > 0)
            m_ShootTimer -= Time.deltaTime;
        else
        {
            if (!bCanShootThisFrame) return;
            m_ShootTimer = k_ShootTimer;
            SpawnBullet();
            return;
        }
    }

    private void SpawnBullet()
    {
        var myBullet = Instantiate(bulletPrefab, transform.position - Vector3.up, Quaternion.identity);
        myBullet.GetComponent<NetworkObject>().Spawn();
    }

    protected void OnDestroy()
    {
        if (!InvadersGame.Singleton) return;

        if (IsServer) InvadersGame.Singleton.UnregisterSpawnableObject(InvadersObjectType.Alien, gameObject);
        InvadersGame.Singleton.isGameOver.OnValueChanged -= OnGameOver;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (!IsServer) return;

        var hitShield = collider.gameObject.GetComponent<Shield>();
        if (hitShield != null) Destroy(hitShield.gameObject);
    }

    public override void NetworkStart()
    {
        base.NetworkStart();

        if (IsServer)
        {
            canShoot = false;
            if (score == 100)
                return;

            Assert.IsTrue(InvadersGame.Singleton);
            InvadersGame.Singleton.RegisterSpawnableObject(InvadersObjectType.Alien, gameObject);
            InvadersGame.Singleton.isGameOver.OnValueChanged += OnGameOver;
        }
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
