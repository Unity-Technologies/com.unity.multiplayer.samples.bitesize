using System;
using System.Collections.Generic;
using UnityEngine;

public class ServerPlayerSpawnPoints : MonoBehaviour
{
    [SerializeField]
    List<GameObject> m_SpawnPoints;

    static ServerPlayerSpawnPoints s_Instance;

    public static ServerPlayerSpawnPoints Instance => s_Instance;

    void Awake()
    {
        s_Instance = this;
    }

    void OnDestroy()
    {
        s_Instance = null;
    }

    public GameObject ConsumeNextSpawnPoint()
    {
        if (m_SpawnPoints.Count == 0)
        {
            return null;
        }
        
        var toReturn = m_SpawnPoints[m_SpawnPoints.Count - 1];
        m_SpawnPoints.RemoveAt(m_SpawnPoints.Count - 1);
        return toReturn;
    }
}
