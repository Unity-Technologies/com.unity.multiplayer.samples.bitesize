using System;
using Unity.Multiplayer.Samples.SocialHub.Physics;
using Unity.Multiplayer.Samples.SocialHub.Effects;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    class DestructibleObject : PhysicsObjectMotion, ISpawnable
    {
#if UNITY_EDITOR
        [HideInInspector]
        public bool DestructibleObjectPropertiesVisible;
#endif
        public float StartingHealth = 100f;
        public float IntangibleDurationAfterDamage;
        public float SecondsUntilRespawn;
        public int DeferredDespawnTicks = 4;
        public TransferableObject TransferableObject;

        NetworkVariable<NetworkBehaviourReference> m_SessionOwnerNetworkObjectSpawner = new NetworkVariable<NetworkBehaviourReference>(writePerm: NetworkVariableWritePermission.Owner);

        int m_LastDamageTick;
        public GameObject DestructionFX;
        public NetworkObject RubblePrefab;
        public string DestructionVFXType;

        FXPrefabPool m_DestructionFXPoolSystem;

        NetworkVariable<bool> m_Initialized = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        NetworkVariable<float> m_Health = new NetworkVariable<float>(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        Vector3 m_OriginalPosition;
        Quaternion m_OriginalRotation;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            InitializeDestructible();
            gameObject.name = $"[NetworkObjectId-{NetworkObjectId}]{name}";
            m_OriginalPosition = transform.position;
            m_OriginalRotation = transform.rotation;
            m_DestructionFXPoolSystem = FXPrefabPool.GetFxPool(DestructionFX);
        }

        public override void OnNetworkDespawn()
        {
            RigidbodyContactEventManager.Instance.RegisterHandler(this, false);

            if (!HasAuthority)
            {
                var fxInstance = m_DestructionFXPoolSystem.GetInstance();
                fxInstance.transform.position = transform.position;
                ChangeObjectVisuals(false);
            }
            base.OnNetworkDespawn();
        }

        public void Init(SessionOwnerNetworkObjectSpawner spawner)
        {
            m_SessionOwnerNetworkObjectSpawner.Value = new NetworkBehaviourReference(spawner);
        }

        protected override bool ProvideNonRigidbodyContactEvents()
        {
            return TransferableObject && TransferableObject.CurrentObjectState == TransferableObject.ObjectState.Thrown && HasAuthority;
        }

        protected override void OnContactEvent(ulong eventId, Vector3 averagedCollisionNormal, Rigidbody collidingBody, Vector3 contactPoint, bool hasCollisionStay = false, Vector3 averagedCollisionStayNormal = default)
        {
            if (!collidingBody)
            {
                if (DebugCollisions)
                {
                    Debug.Log($"{name}-{NetworkObjectId} collision with non-rigidbody.");
                }

                var collisionMessageInfo = new CollisionMessageInfo()
                {
                    Damage = CollisionDamage,
                    Source = NetworkObjectId,
                    SourceId = NetworkObjectId,
                    Destination = NetworkObjectId,
                    DestinationBehaviourId = NetworkBehaviourId
                };
                collisionMessageInfo.SetFlag(true, (uint)CollisionType);
                OnHandleCollision(collisionMessageInfo);
                TransferableObject.SetObjectState(TransferableObject.ObjectState.AtRest);
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

            var intangibilityTicks = Mathf.RoundToInt(NetworkManager.ServerTime.TickRate * IntangibleDurationAfterDamage);
            if (NetworkManager.NetworkTickSystem.ServerTime.Tick - m_LastDamageTick < intangibilityTicks)
            {
                base.OnHandleCollision(collisionMessage, isLocal, applyImmediately);
                return;
            }

            if (DebugCollisions || DebugDamage)
            {
                if (NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(collisionMessage.Source))
                {
                    var sourceCollider = NetworkManager.SpawnManager.SpawnedObjects[collisionMessage.Source];
                    Debug.Log($"[{name}] Collided with {sourceCollider.name} owned by Client-{sourceCollider.OwnerClientId} and is applying a damage of {collisionMessage.Damage}!");
                }
                else
                {
                    Debug.Log($"[{name}] Collided with (unknown or self) and is applying a damage of {collisionMessage.Damage}! server tick {NetworkManager.NetworkTickSystem.ServerTime.Tick} last tick {m_LastDamageTick}");
                }
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
                // TODO: add NetworkObject pool here
                NetworkObject.DeferDespawn(DeferredDespawnTicks, destroy: true);
            }
            else
            {
                m_Health.Value = currentHealth;
                m_LastDamageTick = NetworkManager.NetworkTickSystem.ServerTime.Tick;
            }
        }

        // This method is authority relative
        public override void OnDeferringDespawn(int despawnTick)
        {
            var fxInstance = m_DestructionFXPoolSystem.GetInstance();
            fxInstance.transform.position = transform.position;
            ChangeObjectVisuals(false);
            SpawnRubble(transform.position);
            m_SessionOwnerNetworkObjectSpawner.Value.TryGet(out SessionOwnerNetworkObjectSpawner spawner, NetworkManager);
            var tickToRespawn = NetworkManager.NetworkTickSystem.ServerTime.Tick + Mathf.RoundToInt(SecondsUntilRespawn * NetworkManager.NetworkTickSystem.ServerTime.TickRate);
            spawner.RespawnRpc(tickToRespawn);
            base.OnDeferringDespawn(despawnTick);
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

            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var childRenderer in renderers)
            {
                childRenderer.enabled = enable;
            }

            EnableColliders(enable);
            var rigidbodies = GetComponentsInChildren<Rigidbody>();
            foreach (var childRigidbody in rigidbodies)
            {
                childRigidbody.isKinematic = !enable;
            }
        }

        void SpawnRubble(Vector3 position)
        {
            if (RubblePrefab != null)
            {
                RubblePrefab.InstantiateAndSpawn(NetworkManager, destroyWithScene: true, position: position, rotation: Quaternion.identity);
            }
        }

        void InitializeDestructible()
        {
            if (HasAuthority && !m_Initialized.Value)
            {
                if (IsSpawned)
                {
                    m_Health.Value = StartingHealth;
                    m_LastDamageTick = NetworkManager.NetworkTickSystem.ServerTime.Tick;
                }
                m_Initialized.Value = true;
            }
        }
    }
}
