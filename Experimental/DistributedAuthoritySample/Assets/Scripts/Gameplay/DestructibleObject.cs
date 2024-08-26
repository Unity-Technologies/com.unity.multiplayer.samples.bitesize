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

        NetworkVariable<bool> m_Initialized = new NetworkVariable<bool>(false);

        NetworkVariable<float> m_Health = new NetworkVariable<float>(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public override void OnNetworkSpawn()
        {
            m_DebugCollisions = m_DebugDamage = true;
            base.OnNetworkSpawn();
            InitializeDestructible();
            gameObject.name = $"[NetworkObjectId-{NetworkObjectId}]{name}";
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }

        void OnCollisionEnter(Collision other)
        {
            return;
        }

        protected override void OnHandleCollision(CollisionMessageInfo collisionMessage, bool isLocal = false, bool applyImmediately = false)
        {
            // perhaps add invincible frames? would be a neat showcase
            Debug.Log(nameof(OnHandleCollision));

            if (m_Health.Value == 0.0f || collisionMessage.GetCollisionType() == Physics.CollisionType.Avatar)
            {
                base.OnHandleCollision(collisionMessage, isLocal, applyImmediately);
                return;
            }
            var currentHealth = Mathf.Max(0.0f, m_Health.Value - collisionMessage.Damage);

            if (currentHealth == 0.0f)
            {
                Rigidbody.isKinematic = true;
                EnableColliders(false);
                m_Health.Value = currentHealth;
                NetworkObject.Despawn();
                // TODO: Spawn VFX locally + send VFX message
                return;
            }
            else
            {
                m_Health.Value = currentHealth;
            }

            base.OnHandleCollision(collisionMessage, isLocal, applyImmediately);
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
