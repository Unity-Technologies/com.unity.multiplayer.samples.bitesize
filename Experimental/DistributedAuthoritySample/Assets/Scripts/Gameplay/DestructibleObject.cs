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

        VFXPoolManager m_VFXPoolManager;
        GameObject m_SpawnedRubble;

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

        public override void OnNetworkDespawn()
        {
            if (!HasAuthority)
            {
                PlayDestructionVFX(transform.position);
                SpawnRubble(transform.position);
            }
            base.OnNetworkDespawn();
        }

        void Update()
        {
            if (m_HitPoints <= 0 && m_HealthAtNull == false)
            {
                m_HealthAtNull = true;
                ApplyCollisionDamage(100);
            }
        }

        void FindVFXPoolManager()
        {
            m_VFXPoolManager = VFXPoolManager.Instance;
            if (m_VFXPoolManager == null)
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
            }
            else
            {
                m_Health.Value = currentHealth;
                m_LastDamageTime = Time.realtimeSinceStartup;
            }
        }

        // This method is authority relative
        public override void OnDeferringDespawn(int despawnTick)
        {
            PlayDestructionVFX(transform.position);
            base.OnDeferringDespawn(despawnTick);
            ChangeObjectVisuals(false);
            SpawnRubble(transform.position);
        }

        void ChangeObjectVisuals(bool enable)
        {
            // Ensure the object is at ground level when re-enabled
            if (enable)
            {
                var carryableObject = transform;
                carryableObject.position = m_OriginalPosition;
                carryableObject.rotation = m_OriginalRotation;
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

            // Disable or enable rigidbody physics
            Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rigidbody in rigidbodies)
            {
                rigidbody.isKinematic = !enable;
            }

            //ChangeRubbleVisuals(!enable); // Ensure the rubble is active when the object is inactive and vice-versa
        }


        void PlayDestructionVFX(Vector3 position)
        {
            if (m_VFXPoolManager != null)
            {
                GameObject vfxInstance = m_VFXPoolManager.GetVFXInstance(destructionVFXType);
                if (vfxInstance != null)
                {
                    vfxInstance.transform.position = position;
                    var particleSystem = vfxInstance.GetComponent<ParticleSystem>();

                    if (particleSystem != null && !particleSystem.isPlaying)
                    {
                        Debug.Log(vfxInstance.gameObject.GetInstanceID());
                        Debug.Log("Playing destruction VFX.");
                        particleSystem.Play();
                    }
                }
            }
        }

        void SpawnRubble(Vector3 position)
        {
            if (rubblePrefab != null && m_SpawnedRubble == null)
            {
                m_SpawnedRubble = Instantiate(rubblePrefab, position, Quaternion.identity);
                if (m_SpawnedRubble.TryGetComponent<NetworkObject>(out var networkObject))
                {
                    networkObject.Spawn(true);
                }
            }
        }

        IEnumerator DestroyRubbleAndRestoreObject(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (m_SpawnedRubble != null)
            {
                Destroy(m_SpawnedRubble);
                m_SpawnedRubble = null;

                RestoreObjectRpc();
            }
        }

        [Rpc(SendTo.Everyone)]
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
