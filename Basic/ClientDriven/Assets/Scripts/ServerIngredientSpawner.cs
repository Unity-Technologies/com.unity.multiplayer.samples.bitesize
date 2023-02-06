using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;

public class ServerIngredientSpawner : NetworkBehaviour
{
    [SerializeField]
    List<GameObject> m_SpawnPoints;

    [SerializeField]
    float m_SpawnRatePerSecond;

    [SerializeField]
    GameObject m_IngredientPrefab;

    [SerializeField]
    int m_MaxSpawnWaves;

    int m_SpawnWaves;

    float m_LastSpawnTime;

    Random m_RandomGenerator = new Random();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        m_SpawnWaves = 0;
    }

    public override void OnNetworkDespawn()
    {
        m_SpawnWaves = 0;
    }

    void FixedUpdate()
    {
        if (NetworkManager != null && !IsServer)
        {
            return;
        }

        if (m_SpawnWaves < m_MaxSpawnWaves && Time.time - m_LastSpawnTime > m_SpawnRatePerSecond)
        {
            foreach (var spawnPoint in m_SpawnPoints)
            {
                var newIngredientObject = Instantiate(m_IngredientPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
                newIngredientObject.transform.position = spawnPoint.transform.position +
                    new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), 0, UnityEngine.Random.Range(-0.25f, 0.25f));
                var ingredient = newIngredientObject.GetComponent<ServerIngredient>();
                ingredient.NetworkObject.Spawn();
                ingredient.currentIngredientType.Value = (IngredientType)m_RandomGenerator.Next((int)IngredientType.MAX);
            }
            m_SpawnWaves++;

            m_LastSpawnTime = Time.time;
        }
    }
}
