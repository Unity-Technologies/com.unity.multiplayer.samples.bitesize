using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class EnemyBullet : MonoBehaviour
{
    private const float k_YBoundary = -4.0f;

    // The constant speed at which the Bullet travels
    [Header("Movement Settings")]
    [SerializeField]
    [Tooltip("The constant speed at which the Bullet travels")]
    private float m_TravelSpeed = 3.0f;

    private void Start()
    {
        Assert.IsTrue(InvadersGame.Singleton);
        Assert.IsTrue(NetworkManager.Singleton);

        if(InvadersGame.Singleton)
            InvadersGame.Singleton.isGameOver.OnValueChanged += OnGameOver;
    }

    private void Update()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        transform.Translate(0, -m_TravelSpeed * Time.deltaTime, 0);

        if (transform.position.y < k_YBoundary) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (InvadersGame.Singleton) InvadersGame.Singleton.isGameOver.OnValueChanged -= OnGameOver;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        var hitPlayer = collider.gameObject.GetComponent<PlayerControl>();
        if (hitPlayer != null)
        {
            hitPlayer.HitByBullet();
            Destroy(gameObject);
            return;
        }

        var hitShield = collider.gameObject.GetComponent<Shield>();
        if (hitShield != null)
        {
            Destroy(hitShield.gameObject);
            Destroy(gameObject);
        }
    }

    private void OnGameOver(bool oldValue, bool newValue)
    {
        enabled = false;

        // On game over destroy the bullets
        if (NetworkManager.Singleton.IsServer) Destroy(gameObject);
    }
}
