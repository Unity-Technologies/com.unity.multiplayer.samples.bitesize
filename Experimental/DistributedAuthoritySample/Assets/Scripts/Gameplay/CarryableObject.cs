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

        // Add health component to object
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
            // Check if Health value has changed in the Inspector
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

            // Disable renderers or the entire game object to visually hide the chest
            ChangeChestVisuals(false);

            var vfxInstance = Instantiate(destructionVFX, transform.position, Quaternion.identity);
            var particleSystem = vfxInstance.GetComponent<ParticleSystem>();

            if (particleSystem != null)
            {
                particleSystem.Play();
                float totalWaitTime = particleSystem.main.duration;

                yield return new WaitForSeconds(totalWaitTime);
            }

            // Inform other clients to play VFX and spawn rubble locally
            NotifyClientsOfDestruction(transform.position);

            Debug.Log("VFX Destroyed");
            Destroy(vfxInstance);

            // set timer to despawn rubble and reenable chest
            yield return new WaitForSeconds(5f);
            ChangeChestVisuals(true);
        }

        private void ChangeChestVisuals(bool enable)
        {
            // Disable all renderers to hide the chest visually
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = enable;
            }

            // Disable all colliders to make the chest non-interactive
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = enable;
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
           // Instantiate(rubblePrefab, position, Quaternion.identity);
        }
    }
}
