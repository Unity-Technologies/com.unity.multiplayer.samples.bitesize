using System.Collections.Generic;
using UnityEngine;

public class ServerPlayerSpawnPoints : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> m_SpawnPoints;

    private static ServerPlayerSpawnPoints s_Instance;

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

    private void OnDestroy()
    {
        s_Instance = null;
    }

    public GameObject ConsumeNextSpawnPoint()
    {
        var toReturn = m_SpawnPoints[m_SpawnPoints.Count - 1];
        m_SpawnPoints.RemoveAt(m_SpawnPoints.Count - 1);
        return toReturn;
    }
}
