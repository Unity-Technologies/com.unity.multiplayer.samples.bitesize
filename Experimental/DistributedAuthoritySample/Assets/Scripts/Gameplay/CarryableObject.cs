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
        public string destructionVFXType; // "Pot" or "Crate"
        public GameObject rubblePrefab;

        int previousHealth;
        GameObject spawnedRubble;
        VFXPoolManager vfxPoolManager;
        Vector3 initialPosition;

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

            initialPosition = transform.position;
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
            base.OnNetworkSpawn();
            if (IsServer || IsOwner)
            {
                InitializeRubble();
            }

            FindVFXPoolManager();
        }

        private void FindVFXPoolManager()
        {
            vfxPoolManager = VFXPoolManager.Instance;
            if (vfxPoolManager == null)
            {
                Debug.LogError("VFXPoolManager not found in the scene.");
            }
        }

        void InitializeRubble()
        {
            Debug.Log("Initializing rubble.");
            if (rubblePrefab != null)
            {
                spawnedRubble = Instantiate(rubblePrefab, transform.position, Quaternion.identity);
                // put the rubble underneath the ground, so it is not visible at the beginning
                spawnedRubble.transform.position = new Vector3(transform.position.x, -10f, transform.position.z);
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

            PlayDestructionVFX(transform.position);

            NotifyClientsOfDestruction();

            yield return new WaitForSeconds(5f);

            ChangeObjectVisuals(true);
        }

        void ChangeObjectVisuals(bool enable)
        {
            // Ensure the object is at ground level when re-enabled
            if (enable)
            {
                var carryableObject = transform;
                carryableObject.position = initialPosition;
                carryableObject.rotation = Quaternion.identity; // Ensure the object is upright when re-enabled
            }

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

        void ChangeRubbleVisuals(bool enable)
        {
            Debug.Log("Changing rubble visuals.");
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
            if (vfxPoolManager != null)
            {
                GameObject vfxInstance = vfxPoolManager.GetVFXInstance(destructionVFXType);
                if (vfxInstance != null)
                {
                    vfxInstance.transform.position = position;
                    var particleSystem = vfxInstance.GetComponent<ParticleSystem>();

                    if (particleSystem != null)
                    {
                        Debug.Log("Playing destruction VFX.");
                        particleSystem.time = 4;
                        particleSystem.Play();
                        StartCoroutine(ReturnVFXInstanceAfterDelay(destructionVFXType, vfxInstance, particleSystem.main.duration-0.02f));
                    }
                }
            }
        }

        private IEnumerator ReturnVFXInstanceAfterDelay(string vfxType, GameObject vfxInstance, float delay)
        {
            Debug.Log("Returning VFX instance after delay.");
            yield return new WaitForSeconds(delay);
            vfxPoolManager?.ReturnVFXInstance(vfxType, vfxInstance);
        }

        protected internal virtual void SpawnRubble(Vector3 position)
        {
            Debug.Log("Spawning rubble 2.");
            if (rubblePrefab != null && spawnedRubble == null)
            {
                spawnedRubble = Instantiate(rubblePrefab, position, Quaternion.identity);
                // put the rubble underneath the ground, so it is not visible at the beginning
                spawnedRubble.transform.position = new Vector3(position.x, -10f, position.z);
                if (spawnedRubble.TryGetComponent<NetworkObject>(out var networkObject))
                {
                    networkObject.Spawn(true);
                }
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

        public void SpawnRubble()
        {
            throw new System.NotImplementedException();
        }
    }
}
