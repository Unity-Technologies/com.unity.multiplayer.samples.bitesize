using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;

public class ServerIngredientSpawner : NetworkBehaviour
{
    [SerializeField]
    private List<GameObject> m_SpawnPoints;

    [SerializeField]
    private float m_SpawnRatePerSecond;

    [SerializeField]
    private GameObject m_IngredientPrefab;

    private float m_LastSpawnTime;
    private Random r = new Random();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            enabled = false;
            return;
        }
    }

    private void FixedUpdate()
    {
        if (NetworkManager != null && !IsServer) return;
        if (Time.time - m_LastSpawnTime > m_SpawnRatePerSecond)
        {
            foreach (var spawnPoint in m_SpawnPoints)
            {
                var newIngredientObject = Instantiate(m_IngredientPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
                newIngredientObject.transform.position = spawnPoint.transform.position;
                var ingredient = newIngredientObject.GetComponent<ServerIngredient>();
                ingredient.CurrentIngredientType.Value = (IngredientType) r.Next((int)IngredientType.max);
                ingredient.NetworkObject.Spawn();
            }

            m_LastSpawnTime = Time.time;
        }
    }
}
