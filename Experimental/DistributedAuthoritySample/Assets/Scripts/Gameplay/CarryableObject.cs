using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
public class CarryableObject : NetworkBehaviour
{
    [Header("General Settings")]
    public GameObject LeftHand;
    public GameObject RightHand;
    public int Health = 1;

    [Header("Destruction Settings")]
    public GameObject destructionVFX;
    public GameObject rubblePrefab;

    private int previousHealth;
    private GameObject spawnedRubble;

    public int CurrentHealth
    {
        get => Health;
        set
        {
            Health = value;
            if (Health <= 0)
            {
                DestroyObject();
            }
        }
    }

    private void Start()
    {
        previousHealth = Health;
        if (IsOwner)
        {
            NetworkObject.Spawn();
        }
    }

    private void Update()
    {
        if (previousHealth != Health)
        {
            CurrentHealth = Health;
            previousHealth = Health;
        }
    }

    public override void OnNetworkSpawn()
    {
        // Ensure rubble initialization on network spawn
        if (IsServer || IsOwner)
        {
            InitializeRubble();
        }
    }

    private void InitializeRubble()
    {
        if (rubblePrefab != null)
        {
            spawnedRubble = Instantiate(rubblePrefab, transform.position, Quaternion.identity);
            if (spawnedRubble.TryGetComponent<NetworkObject>(out var networkObject))
            {
                networkObject.Spawn(true);
            }
            ChangeRubbleVisuals(false); // Initially hide the rubble
        }
    }

    protected virtual void DestroyObject()
    {
        Debug.Log("Object Destroyed");
        StartCoroutine(DeferredDespawn());
    }

    protected IEnumerator DeferredDespawn()
    {
        Debug.Log("DeferredDespawn started");

        ChangeObjectVisuals(false);

        if (destructionVFX != null)
        {
            var vfxInstance = Instantiate(destructionVFX, transform.position, Quaternion.identity);
            var particleSystem = vfxInstance.GetComponent<ParticleSystem>();

            if (particleSystem != null)
            {
                particleSystem.Play();
                float totalWaitTime = particleSystem.main.duration;

                yield return new WaitForSeconds(totalWaitTime);

                Destroy(vfxInstance);
            }
        }

        NotifyClientsOfDestruction();

        yield return new WaitForSeconds(5f);

        ChangeObjectVisuals(true);
    }

    private void ChangeObjectVisuals(bool enable)
    {
        // Ensure the object is at ground level when re-enabled
        var objectPosition = transform.position;
        objectPosition.y = 0;
        transform.position = objectPosition;
        transform.rotation = Quaternion.identity; // Ensure the object is upright when re-enabled

        // Disable or enable renderers
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = enable;
        }

        // Disable or enable colliders
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = enable;
        }

        ChangeRubbleVisuals(!enable); // Ensure the rubble is active when the object is inactive and vice-versa
    }

    private void ChangeRubbleVisuals(bool enable)
    {
        if (spawnedRubble != null)
        {
            // Ensure rubble is at ground level
            var transformPosition = transform.position;
            transformPosition.y = 0f;
            spawnedRubble.transform.position = transformPosition;

            // Disable or enable renderers
            Renderer[] renderers = spawnedRubble.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = enable;
            }

            // Disable or enable colliders
            Collider[] colliders = spawnedRubble.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = enable;
            }

            // Disable or enable rigidbody physics
            Rigidbody[] rigidbodies = spawnedRubble.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rigidbody in rigidbodies)
            {
                rigidbody.isKinematic = !enable;
            }
        }
    }

    private void NotifyClientsOfDestruction()
    {
        NotifyClientsOfDestructionClientRpc();
    }

    [ClientRpc]
    private void NotifyClientsOfDestructionClientRpc()
    {
        if (!IsOwner)
        {
            HandleDestructionVisualUpdates();
        }
    }

    private void HandleDestructionVisualUpdates()
    {
        ChangeObjectVisuals(false);
        PlayDestructionVFX(transform.position);
        SpawnRubble(transform.position);
    }

    protected virtual void PlayDestructionVFX(Vector3 position)
    {
        if (destructionVFX != null)
        {
            GameObject vfxInstance = Instantiate(destructionVFX, position, Quaternion.identity);
            var particleSystem = vfxInstance.GetComponent<ParticleSystem>();

            if (particleSystem != null)
            {
                particleSystem.Play();
                float totalWaitTime = particleSystem.main.duration;
                Destroy(vfxInstance, totalWaitTime);
            }
        }
    }

    protected virtual void SpawnRubble(Vector3 position)
    {
        if (rubblePrefab != null && spawnedRubble == null)
        {
            spawnedRubble = Instantiate(rubblePrefab, position, Quaternion.identity);
            if (spawnedRubble.TryGetComponent<NetworkObject>(out var networkObject))
            {
                networkObject.Spawn(true);
            }
            ChangeRubbleVisuals(true);
        }
        else
        {
            ChangeRubbleVisuals(true);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (spawnedRubble != null)
        {
            if (spawnedRubble.TryGetComponent<NetworkObject>(out var networkObject))
            {
                networkObject.Despawn(true);
            }
            else
            {
                Destroy(spawnedRubble);
            }
            spawnedRubble = null;
        }
    }
}

}
