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

    Random r = new Random();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            enabled = false;
            return;
        }
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
                newIngredientObject.transform.position = spawnPoint.transform.position;
                var ingredient = newIngredientObject.GetComponent<ServerIngredient>();
                ingredient.CurrentIngredientType.Value = (IngredientType) r.Next((int)IngredientType.max);
                ingredient.NetworkObject.Spawn();
            }
            m_SpawnWaves++;

            m_LastSpawnTime = Time.time;
        }
    }
}
