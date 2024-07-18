using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class AsteroidSpawnManagement : NetworkBehaviour
{
    public List<GameObject> AsteroidPrefabs;

    private NetworkVariable<bool> m_IsAsteroidFieldInitialized = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Range(1, 100000)]
    public int InitialCount = 40;

    [Range(100, 10000)]
    public float MaxSpawnRadius = 250;


    [Range(10, 100)]
    public float MinSpawnRadius = 30;

    [Range(1.0f, 8.0f)]
    public float InitialScaleMax = 3.0f;

    [Range(0.5f, 3.0f)]
    public float InitialScaleMin = 1.0f;

    public override void OnNetworkSpawn()
    {
        // If scene management is disabled remove any duplicates automatically
        if (!NetworkManager.NetworkConfig.EnableSceneManagement)
        {
            var duplicateManagers = FindObjectsByType<AsteroidSpawnManagement>(FindObjectsSortMode.None).Where((c) => c != this).ToList();
            foreach (var manager in duplicateManagers)
            {
                Destroy(manager.gameObject);
            }
        }
        foreach (var asteroid in AsteroidPrefabs)
        {
            if (ObjectPoolSystem.ExistingPoolSystems.ContainsKey(asteroid))
            {
                AsteroidObject.SetAsteroidPoolSystem(ObjectPoolSystem.ExistingPoolSystems[asteroid]);
            }
        }

        if (IsSessionOwner)
        {
            // Don't try to spawn more asteroids if the session is already initialized
            if (!m_IsAsteroidFieldInitialized.Value)
            {
                StartCoroutine(SpawnInitialField());
            }
            else
            {
                StartCoroutine(GetAsteroidPool());
            }
        }
        else
        {
            StartCoroutine(GetAsteroidPool());
        }
        base.OnNetworkSpawn();
    }

    private IEnumerator GetAsteroidPool()
    {
        var waitUntilPoolAvailable = new WaitForSeconds(1.0f);
        var waitCount = 0;
        while (AsteroidObject.AsteroidPoolSystems.Count < AsteroidPrefabs.Count)
        {
            yield return waitUntilPoolAvailable;
            waitCount++;
            if (waitCount > 10)
            {
                Debug.LogWarning("[SpawnInitialField] Could not find the asteroid pool system! Exiting without spawning the initial asteroid field!");
                yield break;
            }
            foreach (var asteroid in AsteroidPrefabs)
            {
                if (ObjectPoolSystem.ExistingPoolSystems.ContainsKey(asteroid))
                {
                    AsteroidObject.SetAsteroidPoolSystem(ObjectPoolSystem.ExistingPoolSystems[asteroid]);
                }
            }
        }
    }

    private IEnumerator SpawnInitialField()
    {
        yield return GetAsteroidPool();

        while (!NetworkManager.IsListening || !NetworkManager.IsConnectedClient)
        {
            yield return null;
        }
        if (AsteroidObject.AsteroidPoolSystems.Count == 0)
        {
            Debug.LogError("Could not find the Asteroid Pool System! Skipping spawning!");
            yield break;
        }
        for (int i = 0; i < InitialCount; i++)
        {
            var instance = AsteroidObject.GetRandomPoolSystem().GetInstance(true);
            var baslinePos = new Vector3(1.0f, 0.0f, 1.0f);
            var position = GetRandomVector3(MinSpawnRadius, MaxSpawnRadius, baslinePos, true);
            position.y = 0.5f;
            instance.transform.position = position;

            instance.transform.localScale = GetRandomVector3(1.0f, 2f, instance.transform.localScale, false);
            instance.DontDestroyWithOwner = true;
            try
            {
                instance.Spawn();
                var instanceObject = instance.GetComponent<AsteroidObject>();
                if (instanceObject != null)
                {
                    instanceObject.InitializeAsteroid();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        m_IsAsteroidFieldInitialized.Value = true;
    }

    protected Vector3 GetRandomVector3(float min, float max, Vector3 baseLine, bool randomlyApplySign = false)
    {
        var retValue = new Vector3(baseLine.x * Random.Range(min, max), baseLine.y * Random.Range(min, max), baseLine.z * Random.Range(min, max));
        if (!randomlyApplySign)
        {
            return retValue;
        }

        retValue.x *= Random.Range(1, 100) >= 50 ? -1 : 1;
        retValue.y *= Random.Range(1, 100) >= 50 ? -1 : 1;
        retValue.z *= Random.Range(1, 100) >= 50 ? -1 : 1;
        return retValue;
    }

    protected Vector3 GetRandomVector3(MinMaxVector2Physics minMax, Vector3 baseLine, bool randomlyApplySign = false)
    {
        return GetRandomVector3(minMax.Min, minMax.Max, baseLine, randomlyApplySign);
    }

    private float GetClampedRange()
    {
        var value = Random.Range(MinSpawnRadius, MaxSpawnRadius);
        value *= Random.Range(1, 100) >= 50 ? -1.0f : 1.0f;
        return value;
    }
}
