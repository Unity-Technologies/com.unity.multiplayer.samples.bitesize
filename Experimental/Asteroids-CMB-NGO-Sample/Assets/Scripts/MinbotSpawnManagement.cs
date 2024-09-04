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
        NetworkManager.OnSessionOwnerPromoted += OnSessionOwnerPromoted;
        if (ObjectPoolSystem.ExistingPoolSystems.ContainsKey(MinbotPrefab))
        {
            MinbotPoolSystem = ObjectPoolSystem.ExistingPoolSystems[MinbotPrefab];
        }
        if (IsSessionOwner)
        {
#if SESSION_STORE_ENABLED
            // Don't try to spawn more minobots if the session is already initialized
            if (!m_AreMinebotsInitialized.Value)
#endif
            {
                StartCoroutine(SpawnInitialField());
            }
#if SESSION_STORE_ENABLED
            else
            {
                StartCoroutine(GetMinebotPool());
            }
#endif
        }
        else
        {
            StartCoroutine(GetMinebotPool());
        }
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.OnSessionOwnerPromoted -= OnSessionOwnerPromoted;
        base.OnNetworkDespawn();
    }

    private void OnSessionOwnerPromoted(ulong sessionOwnerPromoted)
    {
        if (NetworkManager.LocalClientId == sessionOwnerPromoted && !IsOwner)
        {
            NetworkObject.ChangeOwnership(NetworkManager.LocalClientId);
        }
    }

    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        if (IsSessionOwner && current != NetworkManager.LocalClientId)
        {
            NetworkManagerHelper.Instance.LogMessage($"[{name}] In-Scene placed NetworkObject changed ownership to Client-{current} who is not the session owner! (Reverting)");
            NetworkObject.ChangeOwnership(NetworkManager.LocalClientId);
        }
        base.OnOwnershipChanged(previous, current);
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
