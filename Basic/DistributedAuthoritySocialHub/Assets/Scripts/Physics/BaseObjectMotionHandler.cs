using System;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using System.Collections.Generic;

namespace Unity.Multiplayer.Samples.SocialHub.Physics
{
    class BaseObjectMotionHandler : NetworkTransform, ICollisionHandler, IContactEventHandlerWithInfo
    {
        protected bool m_IsPooled = true;

        [SerializeField]
        CollisionType m_CollisionType;

        internal CollisionType CollisionType => m_CollisionType;

        [SerializeField]
        ushort m_CollisionDamage;

        internal ushort CollisionDamage => m_CollisionDamage;

        protected CollisionMessageInfo m_CollisionMessage = new CollisionMessageInfo();

        protected Rigidbody Rigidbody { get; private set; }

        protected NetworkRigidbody NetworkRigidbody { get; private set; }

        [Tooltip("Enables/Disables collision logging (based on per derived type)")]
        [SerializeField]
        protected bool m_DebugCollisions;

        [Tooltip("Enables/Disables damage logging (based on per derived type)")]
        [SerializeField]
        protected bool m_DebugDamage;

        [Tooltip("Add all colliders to this list that will be used to detect collisions (exclude triggers).")]
        [SerializeField]
        List<Collider> m_Colliders;

        static int s_CollisionId = 0;

        protected ulong LastEventId { get; private set; }

        protected void EnableColliders(bool enable)
        {
            foreach (var collider in m_Colliders)
            {
                collider.enabled = enable;
            }
        }

        public Rigidbody GetRigidbody()
        {
            return Rigidbody;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Vector3 GetObjectVelocity(bool getReference = false)
        {
            return OnGetObjectVelocity(getReference);
        }

        protected virtual Vector3 OnGetObjectVelocity(bool getReference = false)
        {
            if (Rigidbody != null)
            {
#if UNITY_2023_3_OR_NEWER
                return Rigidbody.linearVelocity;
#else
            return m_Rigidbody.velocity;
#endif
            }

            return Vector3.zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void SetObjectVelocity(Vector3 velocity)
        {
            if (Rigidbody != null)
            {
#if UNITY_2023_3_OR_NEWER
                Rigidbody.linearVelocity = velocity;
#else
            m_Rigidbody.velocity = velocity;
#endif
            }
        }

        protected Vector3 GetObjectAngularVelocity()
        {
            return OnGetObjectAngularVelocity();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual Vector3 OnGetObjectAngularVelocity()
        {
            if (Rigidbody != null)
            {
                return Rigidbody.angularVelocity;
            }

            return Vector3.zero;
        }

        protected void IgnoreCollision(GameObject objectA, GameObject objectB, bool shouldIgnore)
        {
            if (objectA == null || objectB == null)
            {
                return;
            }

            var rootA = objectA.transform.root.gameObject;
            var rootB = objectB.transform.root.gameObject;

            var collidersA = rootA.GetComponentsInChildren<Collider>();
            var collidersB = rootB.GetComponentsInChildren<Collider>();

            foreach (var colliderA in collidersA)
            {
                foreach (var colliderB in collidersB)
                {
                    UnityEngine.Physics.IgnoreCollision(colliderA, colliderB, shouldIgnore);
                }
            }
        }

        protected override void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            NetworkRigidbody = GetComponent<NetworkRigidbody>();

            base.Awake();
        }

        protected virtual void Start()
        {
            m_CollisionMessage.Damage = m_CollisionDamage;
            m_CollisionMessage.SetFlag(true, (uint)m_CollisionType);
        }

        /// <summary>
        /// Invoked every network tick if this instance has sent a <see cref="NetworkTransform.NetworkTransformState"/> update.
        /// </summary>
        /// <param name="networkTransformState"></param>
        protected override void OnAuthorityPushTransformState(ref NetworkTransformState networkTransformState)
        {
            base.OnAuthorityPushTransformState(ref networkTransformState);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static GameObject GetRootParent(GameObject parent)
        {
            return parent.transform.root.gameObject;
        }

        /// <summary>
        /// This method provides the ability to make adjustments to the collision message as well as apply damage locally if needed
        /// </summary>
        /// <param name="averagedCollisionNormal"></param>
        /// <param name="targetBaseObjectMotionHandler"></param>
        protected virtual bool OnPrepareCollisionMessage(Vector3 averagedCollisionNormal, BaseObjectMotionHandler targetBaseObjectMotionHandler)
        {
            return true;
        }

        protected virtual void OnHandleCollision(CollisionMessageInfo collisionMessage, bool isLocal = false, bool applyImmediately = false)
        {

        }

        void HandleCollision(CollisionMessageInfo collisionMessage, bool isLocal = false, bool applyImmediately = false)
        {
            OnHandleCollision(collisionMessage, isLocal, applyImmediately);

            // Handling is invoked before logging so logging can determine the end result.
            if (m_DebugCollisions)
            {
                LogHandleCollision(collisionMessage);
            }
        }

        /// <summary>
        /// Used to communicate collisions
        /// </summary>
        /// <param name="collisionMessage"></param>
        /// <param name="rpcParams"></param>
        [Rpc(SendTo.Authority, RequireOwnership = false)]
        public void HandleCollisionRpc(CollisionMessageInfo collisionMessage, RpcParams rpcParams = default)
        {
            // If authority changes while this message is in flight, forward it to the new authority
            if (!HasAuthority)
            {
                LogMessage($"[HandleCollisionRpc][Not Owner][Routing Collision][{name}] Routing to Client-{OwnerClientId}");
                SendCollisionMessage(m_CollisionMessage);
                return;
            }

            m_CollisionMessage.SourceOwner = rpcParams.Receive.SenderClientId;
            m_CollisionMessage.TargetOwner = OwnerClientId;
            HandleCollision(collisionMessage);
        }

        /// <summary>
        /// Invoked by the owner of the object inflicting damage, this will handle the RPC routing
        /// of the message to the appropriate targeted owner of the object taking damage
        /// </summary>
        /// <param name="collisionMessage"></param>
        public void SendCollisionMessage(CollisionMessageInfo collisionMessage)
        {
            LogCollision(collisionMessage);
            HandleCollisionRpc(collisionMessage);
        }

        /// <summary>
        /// Override this method if you have registered the instance with <see cref="RigidbodyContactEventManager"/> and
        /// want to customize collision.
        /// </summary>
        /// <remarks>
        /// Only <see cref="PhysicsObjectMotion"/> automatically handles collisions.
        /// </remarks>
        /// <param name="eventId"></param>
        /// <param name="averagedCollisionNormal"></param>
        /// <param name="collidingBody">The <see cref="Rigidbody"/> that collided with this object.</param>
        /// <param name="contactPoint"></param>
        /// <param name="hasCollisionStay"></param>
        /// <param name="averagedCollisionStayNormal"></param>
        protected virtual void OnContactEvent(ulong eventId, Vector3 averagedCollisionNormal, Rigidbody collidingBody, Vector3 contactPoint, bool hasCollisionStay = false, Vector3 averagedCollisionStayNormal = default)
        {

        }

        /// <summary>
        /// Invoked from <see cref="RigidbodyContactEventManager"/> when a non-kinematic body collides
        /// with another registered <see cref="UnityEngine.Rigidbody"/>.
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="averageNormal">The averaged normal of the collision</param>
        /// <param name="collidingBody">The <see cref="Rigidbody"/> this objects collided with</param>
        /// <param name="contactPoint"></param>
        /// <param name="hasCollisionStay"></param>
        /// <param name="averagedCollisionStayNormal"></param>
        /// <remarks> To enable this callback to be triggered, make sure you enable the Provides Contacts toggle on your
        /// desired <see cref="Collider"/>
        /// </remarks>
        public void ContactEvent(ulong eventId, Vector3 averageNormal, Rigidbody collidingBody, Vector3 contactPoint, bool hasCollisionStay = false, Vector3 averagedCollisionStayNormal = default)
        {
            if (!IsSpawned)
            {
                return;
            }

            OnContactEvent(eventId, averageNormal, collidingBody, contactPoint, hasCollisionStay, averagedCollisionStayNormal);
            LastEventId = eventId;
        }

        protected virtual bool ProvideNonRigidbodyContactEvents()
        {
            return false;
        }

        public ContactEventHandlerInfo GetContactEventHandlerInfo()
        {
            var contactEventHandlerInfo = new ContactEventHandlerInfo();
            contactEventHandlerInfo.ProvideNonRigidBodyContactEvents = ProvideNonRigidbodyContactEvents();
            contactEventHandlerInfo.HasContactEventPriority = HasAuthority;
            return contactEventHandlerInfo;
        }

        /// <summary>
        /// Invoked this to send a collision message to the authoritative instance.
        /// </summary>
        /// <param name="averagedCollisionNormal"></param>
        /// <param name="collidingBodyBaseObject"></param>
        protected void EventCollision(Vector3 averagedCollisionNormal, BaseObjectMotionHandler collidingBodyBaseObject)
        {
#if DEBUG || UNITY_EDITOR
            if (m_DebugCollisions)
            {
                LogCollision(ref collidingBodyBaseObject);
            }
#endif

            if (OnPrepareCollisionMessage(averagedCollisionNormal, collidingBodyBaseObject))
            {
                s_CollisionId++;
                m_CollisionMessage.CollisionId = s_CollisionId;
                m_CollisionMessage.Time = Time.realtimeSinceStartup;
                m_CollisionMessage.Source = OwnerClientId;
                m_CollisionMessage.SourceId = NetworkObjectId;
                m_CollisionMessage.Destination = collidingBodyBaseObject.OwnerClientId;
                m_CollisionMessage.DestinationNetworkObjId = collidingBodyBaseObject.NetworkObjectId;
                m_CollisionMessage.DestinationBehaviourId = collidingBodyBaseObject.NetworkBehaviourId;

                // Otherwise, send the collision message to the owner of the object
                collidingBodyBaseObject.SendCollisionMessage(m_CollisionMessage);
            }
        }

        #region DEBUG CONSOLE LOGGING METHODS

        /// <summary>
        /// Override to handle local collisions generating an outbound message
        /// </summary>
        /// <param name="objectHit"></param>
        /// <returns></returns>
        protected virtual string OnLogCollision(ref BaseObjectMotionHandler objectHit)
        {
            return "[LocalCollision-End]";
        }

        void LogCollision(ref BaseObjectMotionHandler objectHit)
        {
            if (!m_DebugCollisions)
            {
                return;
            }

            var distance = Vector3.Distance(transform.position, objectHit.transform.position);
            Debug.Log($"[{Time.realtimeSinceStartup}][LocalCollision][{name}][collided with][{objectHit.name}][Collider:{name}][Distance: {distance}]" +
                $"{OnLogCollision(ref objectHit)}.", this);
        }

        protected virtual string OnLogCollision(CollisionMessageInfo collisionMessage)
        {
            return string.Empty;
        }

        protected void LogCollision(CollisionMessageInfo collisionMessage)
        {
            if (!m_DebugDamage || collisionMessage.Damage == 0)
            {
                return;
            }

            var additionalInfo = OnLogCollision(collisionMessage);
            Debug.Log($"[{name}][++Damaged++][Client-{collisionMessage.TargetOwner}][{collisionMessage.GetCollisionType()}][Dmg:{collisionMessage.Damage}] {additionalInfo}", this);
        }

        /// <summary>
        /// Override to log incoming collision messages
        /// </summary>
        /// <param name="collisionMessage"></param>
        protected virtual string OnLogHandleCollision(ref CollisionMessageInfo collisionMessage)
        {
            return "[CollisionMessage-End]";
        }

        void LogHandleCollision(CollisionMessageInfo collisionMessage, bool isLocal = false)
        {
            var distance = -1.0f;
            if (NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(collisionMessage.DestinationNetworkObjId))
            {
                distance = Vector3.Distance(transform.position, NetworkManager.SpawnManager.SpawnedObjects[collisionMessage.DestinationNetworkObjId].transform.position);
            }

            var distStr = distance == -1.0f ? $"{collisionMessage.DestinationNetworkObjId} DNE!!" : $"Distance: {distance}";
            Debug.Log($"[{collisionMessage.CollisionId}][{collisionMessage.Time}][CollisionMessage][IsLocal: {isLocal}][{name}][Src:{collisionMessage.Source}][Dest:{collisionMessage.Destination}]" +
                $"[NObjId:{collisionMessage.DestinationNetworkObjId}][NBvrId:{collisionMessage.DestinationBehaviourId}][{distStr}]{OnLogHandleCollision(ref collisionMessage)}.", this);
        }

        protected void LogMessage(string msg, bool forceMessage = false, float messageTime = 10.0f)
        {
            Debug.Log($"[{name}]{msg} {messageTime} {forceMessage}");
        }

        #endregion
    }
}
