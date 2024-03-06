using System.Collections.Generic;
using UnityEngine;

public class ServerPlayerSpawnPoints : MonoBehaviour
{
    [SerializeField]
    List<GameObject> m_SpawnPoints;

    static ServerPlayerSpawnPoints s_Instance;

    public static ServerPlayerSpawnPoints Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = FindObjectOfType<ServerPlayerSpawnPoints>();
            }

            return s_Instance;
        }
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
