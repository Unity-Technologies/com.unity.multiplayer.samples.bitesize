using System;
using System.Collections;
using Unity.Multiplayer.Samples.SocialHub.Physics;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    class DestructibleObject : PhysicsObjectMotion
    {
        [SerializeField]
        float m_StartingHealth = 100f;

        [SerializeField]
        float m_IntangibleDurationAfterDamage;

        float m_LastDamageTime;

        [SerializeField]
        GameObject m_DestructionFX;

        [SerializeField]
        GameObject rubblePrefab;

        [SerializeField]
        string destructionVFXType;

        [SerializeField]
        int m_HitPoints = 100;

        VFXPoolManager vfxPoolManager;
        GameObject spawnedRubble;

        NetworkVariable<bool> m_Initialized = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        NetworkVariable<float> m_Health = new NetworkVariable<float>(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        bool m_HealthAtNull = false;

        Vector3 m_OriginalPosition;
        Quaternion m_OriginalRotation;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            InitializeDestructible();
            gameObject.name = $"[NetworkObjectId-{NetworkObjectId}]{name}";
            m_OriginalPosition = transform.position;
            m_OriginalRotation = transform.rotation;
            FindVFXPoolManager();
        }

        void Update()
        {
            if (m_HitPoints <= 0 && m_HealthAtNull == false)
            {
                m_HealthAtNull = true;
                ApplyCollisionDamage(100);
            }
        }

        private void FindVFXPoolManager()
        {
            vfxPoolManager = VFXPoolManager.Instance;
            if (vfxPoolManager == null)
            {
                Debug.LogError("VFXPoolManager not found in the scene.");
            }
        }

        protected override void OnContactEvent(ulong eventId, Vector3 averagedCollisionNormal, Rigidbody collidingBody, Vector3 contactPoint, bool hasCollisionStay = false, Vector3 averagedCollisionStayNormal = default)
        {
            var collidingBaseObjectMotion = collidingBody.GetComponent<BaseObjectMotionHandler>();
            var collidingBodyPhys = collidingBaseObjectMotion as PhysicsObjectMotion;

            if (!Rigidbody.isKinematic && !collidingBody.isKinematic && collidingBodyPhys != null && HasAuthority && collidingBodyPhys.HasAuthority)
            {
                var collisionMessageInfo = new CollisionMessageInfo();
                collisionMessageInfo.Damage = collidingBodyPhys.CollisionDamage;
                collisionMessageInfo.SetFlag(true, (uint)collidingBodyPhys.CollisionType);

                OnHandleCollision(collisionMessageInfo);

                collisionMessageInfo.Damage = CollisionDamage;
                collisionMessageInfo.SetFlag(true, (uint)CollisionType);

                var destructible = collidingBodyPhys.GetComponent<DestructibleObject>();
                destructible.OnHandleCollision(collisionMessageInfo);
            }
            else
            {
                base.OnContactEvent(eventId, averagedCollisionNormal, collidingBody, contactPoint, hasCollisionStay, averagedCollisionStayNormal);
            }
        }

        protected override void OnHandleCollision(CollisionMessageInfo collisionMessage, bool isLocal = false, bool applyImmediately = false)
        {
            if (m_Health.Value == 0.0f || collisionMessage.GetCollisionType() == CollisionType.Avatar)
            {
                base.OnHandleCollision(collisionMessage, isLocal, applyImmediately);
                return;
            }

            if (Time.realtimeSinceStartup - m_LastDamageTime < m_IntangibleDurationAfterDamage)
            {
                base.OnHandleCollision(collisionMessage, isLocal, applyImmediately);
                return;
            }

            ApplyCollisionDamage(collisionMessage.Damage);

            base.OnHandleCollision(collisionMessage, isLocal, applyImmediately);
        }

        void ApplyCollisionDamage(float damage)
        {
            Debug.Log("Applying collision damage.");
            var currentHealth = Mathf.Max(0.0f, m_Health.Value - damage);

            if (currentHealth == 0.0f)
            {
                Rigidbody.isKinematic = true;
                EnableColliders(false);
                m_Health.Value = currentHealth;
                NetworkObject.DeferDespawn(1, destroy: false);
                // Trigger VFX and rubble after deferred despawn time
                StartCoroutine(HandleDestructionAfterDelay(1));
            }
            else
            {
                m_Health.Value = currentHealth;
                m_LastDamageTime = Time.realtimeSinceStartup;
            }
        }

        IEnumerator HandleDestructionAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            PlayDestructionVFX(gameObject.transform.position);
            SpawnVFXRpc();

            // Spawn rubble and then manage its lifecycle
            SpawnRubble(gameObject.transform.position);
            StartCoroutine(DestroyRubbleAndRestoreObject(5));
        }

        [Rpc(SendTo.NotAuthority & SendTo.Authority)]
        void SpawnVFXRpc()
        {
            PlayDestructionVFX(gameObject.transform.position);
        }

        void PlayDestructionVFX(Vector3 position)
        {
            Debug.Log("Playing destruction VFX.");
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
                        particleSystem.Play();
                        StartCoroutine(ReturnVFXInstanceAfterDelay(destructionVFXType, vfxInstance, particleSystem.main.duration - 0.02f));
                    }
                }
            }
        }

        IEnumerator ReturnVFXInstanceAfterDelay(string vfxType, GameObject vfxInstance, float delay)
        {
            Debug.Log("Returning VFX instance after delay.");
            yield return new WaitForSeconds(delay);
            vfxPoolManager?.ReturnVFXInstance(vfxType, vfxInstance);
        }

        void SpawnRubble(Vector3 position)
        {
            if (rubblePrefab != null && spawnedRubble == null)
            {
                spawnedRubble = Instantiate(rubblePrefab, position, Quaternion.identity);
                if (spawnedRubble.TryGetComponent<NetworkObject>(out var networkObject))
                {
                    networkObject.Spawn(true);
                }
            }
        }

        IEnumerator DestroyRubbleAndRestoreObject(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (spawnedRubble != null)
            {
                Destroy(spawnedRubble);
                spawnedRubble = null;

                RestoreObjectRpc();
            }
        }

        [Rpc(SendTo.NotAuthority & SendTo.Authority)]
        void RestoreObjectRpc()
        {
            RestoreObject();
        }

        void RestoreObject()
        {
            transform.position = m_OriginalPosition;
            transform.rotation = m_OriginalRotation;
            EnableColliders(true);
            Rigidbody.isKinematic = false;

            // Re-initialize health and other parameters
            InitializeDestructible(m_StartingHealth);
        }

        void InitializeDestructible()
        {
            if (HasAuthority && !m_Initialized.Value)
            {
                InitializeDestructible(m_StartingHealth);
                m_Initialized.Value = true;
            }
        }

        void InitializeDestructible(float health)
        {
            if (IsSpawned)
            {
                m_Health.Value = health;
            }
        }
    }
}
