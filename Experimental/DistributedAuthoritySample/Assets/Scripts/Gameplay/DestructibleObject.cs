using System;
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

        NetworkVariable<bool> m_Initialized = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        NetworkVariable<float> m_Health = new NetworkVariable<float>(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            InitializeDestructible();
            gameObject.name = $"[NetworkObjectId-{NetworkObjectId}]{name}";
        }

        protected override void OnContactEvent(ulong eventId, Vector3 averagedCollisionNormal, Rigidbody collidingBody, Vector3 contactPoint, bool hasCollisionStay = false, Vector3 averagedCollisionStayNormal = default)
        {
            var collidingBaseObjectMotion = collidingBody.GetComponent<BaseObjectMotionHandler>();
            var collidingBodyPhys = collidingBaseObjectMotion as PhysicsObjectMotion;

            // overriding this method to catch when a physics collision happens between two non-kinematic objects
            if (!Rigidbody.isKinematic && !collidingBody.isKinematic && collidingBodyPhys != null && HasAuthority && collidingBodyPhys.HasAuthority)
            {
                var collisionMessageInfo = new CollisionMessageInfo();
                collisionMessageInfo.Damage = collidingBodyPhys.CollisionDamage;
                collisionMessageInfo.SetFlag(true, (uint)collidingBodyPhys.CollisionType);

                // apply damage to this non-kinematic object
                OnHandleCollision(collisionMessageInfo);

                collisionMessageInfo.Damage = CollisionDamage;
                collisionMessageInfo.SetFlag(true, (uint)CollisionType);

                // this can be reworked to an interface, but for now this routes damage directly to another destructible
                var destructible = collidingBodyPhys.GetComponent<DestructibleObject>();
                // apply damage to other non-kinematic object
                destructible.OnHandleCollision(collisionMessageInfo);
            }
            else
            {
                base.OnContactEvent(eventId, averagedCollisionNormal, collidingBody, contactPoint, hasCollisionStay, averagedCollisionStayNormal);
            }
        }

        protected override void OnHandleCollision(CollisionMessageInfo collisionMessage, bool isLocal = false, bool applyImmediately = false)
        {
            // Avatars don't damage destructible objects
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
            var currentHealth = Mathf.Max(0.0f, m_Health.Value - damage);

            if (currentHealth == 0.0f)
            {
                Rigidbody.isKinematic = true;
                EnableColliders(false);
                m_Health.Value = currentHealth;
                NetworkObject.Despawn();
                // TODO: Spawn VFX locally + send VFX message
            }
            else
            {
                m_Health.Value = currentHealth;
                m_LastDamageTime = Time.realtimeSinceStartup;
            }
        }

        void InitializeDestructible()
        {
            if (IsSessionOwner && !m_Initialized.Value)
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
