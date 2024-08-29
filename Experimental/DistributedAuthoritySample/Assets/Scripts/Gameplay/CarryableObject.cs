using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
public class CarryableObject : NetworkBehaviour
{
    public GameObject LeftHand;
    public GameObject RightHand;
    public int Health = 1;
    public GameObject destructionVFX;

    private int previousHealth;

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
        if (IsServer && IsOwner)
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

    protected virtual void DestroyObject()
    {
        Debug.Log("Object Destroyed");
        StartCoroutine(DeferredDespawn());
    }

    protected IEnumerator DeferredDespawn()
    {
        Debug.Log("DeferredDespawn started");

        ChangeChestVisuals(false);
        var vfxInstance = Instantiate(destructionVFX, transform.position, Quaternion.identity);
        var particleSystem = vfxInstance.GetComponent<ParticleSystem>();

        if (particleSystem != null)
        {
            particleSystem.Play();
            float totalWaitTime = particleSystem.main.duration;

            yield return new WaitForSeconds(totalWaitTime);
        }

        NotifyClientsOfDestruction(transform.position);
        Debug.Log("VFX Destroyed");
        Destroy(vfxInstance);

        yield return new WaitForSeconds(5f);
        ChangeChestVisuals(true);
    }

    protected virtual void ChangeChestVisuals(bool enable)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = enable;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = enable;
        }

        if (this is Chest chest)
        {
            chest.ChangeRubbleVisuals(!enable, Vector3.zero); // Ensure the rubble stays active when chest is inactive and vice-versa
        }
    }

    private void NotifyClientsOfDestruction(Vector3 position)
    {
        if (IsOwner)
        {
            InformOtherClientsOfDestructionClientRpc(position);
        }
    }

    [ClientRpc]
    private void InformOtherClientsOfDestructionClientRpc(Vector3 position)
    {
        if (!IsOwner)
        {
            PlayDestructionVFX(position);
            SpawnRubble(position);
        }
    }

    private void PlayDestructionVFX(Vector3 position)
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

    protected virtual void SpawnRubble(Vector3 position)
    {
        // Intended to be overridden by derived classes.
    }
}
}
