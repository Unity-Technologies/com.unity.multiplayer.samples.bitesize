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
    NetworkObject m_IngredientPrefab;

    [SerializeField]
    int m_MaxSpawnWaves;

    int m_SpawnWaves;

    float m_LastSpawnTime;

    Random m_RandomGenerator = new Random();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        enabled = IsServer;
        if (!IsServer)
        {
            return;
        }

        m_SpawnWaves = 0;
    }

    public override void OnNetworkDespawn()
    {
        m_SpawnWaves = 0;
        enabled = false;
    }

    void FixedUpdate()
    {
        if (NetworkManager == null || !NetworkManager.IsListening || !IsServer)
        {
            return;
        }

        if (m_SpawnWaves < m_MaxSpawnWaves && Time.time - m_LastSpawnTime > m_SpawnRatePerSecond)
        {
            foreach (var spawnPoint in m_SpawnPoints)
            {
                var newIngredientObject = m_IngredientPrefab.InstantiateAndSpawn(NetworkManager,
                    position: spawnPoint.transform.position + new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), 0, UnityEngine.Random.Range(-0.25f, 0.25f)),
                    rotation: spawnPoint.transform.rotation);
                var ingredient = newIngredientObject.GetComponent<ServerIngredient>();
                ingredient.currentIngredientType.Value = (IngredientType)m_RandomGenerator.Next((int)IngredientType.MAX);
            }
            m_SpawnWaves++;

            m_LastSpawnTime = Time.time;
        }
    }
}
