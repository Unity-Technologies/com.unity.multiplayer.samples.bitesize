using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(NetworkManager))]
public class RandomPositionPlayerSpawner: MonoBehaviour
{
    NetworkManager m_NetworkManager;

    int m_RoundRobinIndex = 0;
    
    [SerializeField]
    SpawnMethod m_SpawnMethod;
    
    [SerializeField]
    List<Vector3> m_SpawnPositions = new List<Vector3>() { Vector3.zero };

    /// <summary>
    /// Get a spawn position for a spawned object based on the spawn method.
    /// </summary>
    /// <returns>?The spawn position.</returns>
    /// <exception cref="NotImplementedException"></exception>
    public Vector3 GetNextSpawnPosition()
    {
        switch (m_SpawnMethod)
        {
            case SpawnMethod.Random:
                var index = Random.Range(0, m_SpawnPositions.Count);
                return m_SpawnPositions[index];
            case SpawnMethod.RoundRobin:
                m_RoundRobinIndex = (m_RoundRobinIndex + 1) % m_SpawnPositions.Count;
                return m_SpawnPositions[m_RoundRobinIndex];
            default:
                throw new NotImplementedException();
        }
    }
    
    private void Awake()
    {
        var networkManager = gameObject.GetComponent<NetworkManager>();
        networkManager.ConnectionApprovalCallback += ConnectionApprovalWithRandomSpawnPos;
    }

    void ConnectionApprovalWithRandomSpawnPos(byte[] payload, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
    {
        callback(true, null, true, GetNextSpawnPosition(), Quaternion.identity);
    }
}

enum SpawnMethod
{
    Random = 0,
    RoundRobin = 1,
}
