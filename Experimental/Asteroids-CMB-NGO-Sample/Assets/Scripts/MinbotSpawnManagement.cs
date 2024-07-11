using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class MinbotSpawnManagement : NetworkBehaviour
{
    public GameObject MinbotPrefab;
    private ObjectPoolSystem MinbotPoolSystem;

    private NetworkVariable<bool> m_AreMinebotsInitialized = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Range(1, 15)]
    public int InitialCount = 8;

    [Range(50, 500)]
    public float MaxSpawnRadius = 250;


    [Range(10, 50)]
    public float MinSpawnRadius = 30;

    public override void OnNetworkSpawn()
    {
        // If scene management is disabled remove any duplicates automatically
        if (!NetworkManager.NetworkConfig.EnableSceneManagement)
        {
            var duplicateManagers = FindObjectsByType<MinbotSpawnManagement>(FindObjectsSortMode.None).Where((c) => c != this).ToList();
            foreach (var manager in duplicateManagers)
            {
                Destroy(manager.gameObject);
            }
        }

        if (ObjectPoolSystem.ExistingPoolSystems.ContainsKey(MinbotPrefab))
        {
            MinbotPoolSystem = ObjectPoolSystem.ExistingPoolSystems[MinbotPrefab];
        }
        if (IsSessionOwner)
        {
            // Don't try to spawn more minobots if the session is already initialized
            if (!m_AreMinebotsInitialized.Value)
            {
                StartCoroutine(SpawnInitialField());
            }
            else
            {
                StartCoroutine(GetMinebotPool());
            }
        }
        else
        {
            StartCoroutine(GetMinebotPool());
        }
        base.OnNetworkSpawn();
    }

    private IEnumerator GetMinebotPool()
    {
        var waitUntilPoolAvailable = new WaitForSeconds(1.0f);
        var waitCount = 0;
        while (MinbotPoolSystem == null)
        {
            yield return waitUntilPoolAvailable;
            waitCount++;
            if (waitCount > 10)
            {
                Debug.LogWarning("[SpawnInitialField] Could not find the asteroid pool system! Exiting without spawning the initial asteroid field!");
                yield break;
            }
            if (ObjectPoolSystem.ExistingPoolSystems.ContainsKey(MinbotPrefab))
            {
                MinbotPoolSystem = ObjectPoolSystem.ExistingPoolSystems[MinbotPrefab];
            }
        }
    }

    private IEnumerator SpawnInitialField()
    {
        yield return GetMinebotPool();

        for (int i = 0; i < InitialCount; i++)
        {
            var instance = MinbotPoolSystem.GetInstance(true);
            instance.transform.position = new Vector3(GetClampedRange(), 0.5f, GetClampedRange());
            instance.DontDestroyWithOwner = true;
            instance.Spawn();
        }

        m_AreMinebotsInitialized.Value = true;
    }

    private float GetClampedRange()
    {
        var value = Random.Range(MinSpawnRadius, MaxSpawnRadius);
        value *= Random.Range(1, 100) >= 50 ? -1.0f : 1.0f;
        return value;
    }
}
