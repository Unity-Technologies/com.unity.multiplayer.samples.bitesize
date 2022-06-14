using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    private const float k_YBoundary = 15.0f;
    public PlayerControl owner;

    [Header("Movement Settings")]
    [SerializeField]
    [Tooltip("The constant speed at which the Bullet travels")]
    private float m_TravelSpeed = 4.0f;

    [SerializeField]
    ParticleSystem m_EnemyExplosionParticle;

    private void Update()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        transform.Translate(0, m_TravelSpeed * Time.deltaTime, 0);

        if (transform.position.y > k_YBoundary)
            if (NetworkManager.Singleton.IsServer)
                Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        var hitEnemy = collider.gameObject.GetComponent<EnemyAgent>();
        if (hitEnemy != null && owner != null)
        {
            owner.IncreasePlayerScore(hitEnemy.score);

            Destroy(hitEnemy.gameObject);
            Destroy(gameObject);
            
            // this instantiates at the position of the bullet, there is an offset in the Y axis on the 
            // particle systems in the prefab so it looks like it spawns in the middle of the enemy
            Instantiate(m_EnemyExplosionParticle, transform.position, quaternion.identity);
            return;
        }

        var hitShield = collider.gameObject.GetComponent<Shield>();
        if (hitShield != null)
        {
            Destroy(hitShield.gameObject);
            Destroy(gameObject);
        }
    }
}
