using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using Unity.Multiplayer.Samples.SocialHub.Utils;

namespace Unity.Multiplayer.Samples.SocialHub.Physics
{
    class PhysicsObjectMotion : BaseObjectMotionHandler
    {
        [SerializeField]
        float m_MaxAngularVelocity = 30;
        [SerializeField]
        float m_MaxVelocity = 30;

        List<RemoteForce> m_RemoteAppliedForce = new List<RemoteForce>();

        protected NetworkVariable<bool> m_IsInitialized = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        /// <summary>
        /// All of the below values keep the physics objects synchronized between clients so when ownership changes the local Rigidbody can be configured to mirror
        /// the last known physics related states.
        /// </summary>
        protected NetworkVariable<float> m_Mass = new NetworkVariable<float>(1.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        protected NetworkVariable<Vector3> m_AngularVelocity = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        protected NetworkVariable<Vector3> m_Velocity = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        protected NetworkVariable<Vector3> m_Torque = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        protected NetworkVariable<Vector3> m_Force = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        protected override Vector3 OnGetObjectVelocity(bool getReference = false)
        {
            if (getReference)
            {
                return m_Velocity.Value;
            }

            return base.OnGetObjectVelocity(getReference);
        }

        protected override Vector3 OnGetObjectAngularVelocity()
        {
            return m_AngularVelocity.Value;
        }

        protected void UpdateVelocity(Vector3 velocity, bool updateObjectVelocity = true)
        {
            if (HasAuthority)
            {
                if (updateObjectVelocity)
                {
                    SetObjectVelocity(velocity);
                }

                m_Velocity.Value = velocity;
            }
        }

        protected void UpdateAngularVelocity(Vector3 angularVelocity)
        {
            if (HasAuthority)
            {
                Rigidbody.angularVelocity = angularVelocity;
                m_AngularVelocity.Value = angularVelocity;
            }
        }

        protected void UpdateTorque(Vector3 torque)
        {
            if (HasAuthority)
            {
                Rigidbody.AddTorque(torque);
                m_Torque.Value = torque;
            }
        }

        protected void UpdateImpulseForce(Vector3 impulseForce)
        {
            if (HasAuthority)
            {
                Rigidbody.AddForce(impulseForce, ForceMode.Impulse);
                m_Force.Value = impulseForce;
            }
        }

        /// <summary>
        /// Invoked when authority pushes state, we keep track whether the most recent state
        /// had rotation or position deltas.
        /// </summary>
        /// <remarks>
        /// This keeps track of angular and motion velocities in order to keep objects synchronized
        /// when ownership changes.
        /// </remarks>
        /// <param name="networkTransformState"></param>
        protected override void OnAuthorityPushTransformState(ref NetworkTransformState networkTransformState)
        {
            // If we haven't already initialized for the first time or haven't initialized previous state values during spawn then exit early
            if (!m_IsInitialized.Value)
            {
                return;
            }

            if (networkTransformState.HasRotAngleChange && !Rigidbody.isKinematic)
            {
                if (Vector3.Distance(GetObjectAngularVelocity(), Rigidbody.angularVelocity) > RotAngleThreshold)
                {
                    UpdateAngularVelocity(Rigidbody.angularVelocity);
                }
            }

            if (networkTransformState.HasPositionChange && !Rigidbody.isKinematic)
            {
                var velocity = GetObjectVelocity();
                if (Vector3.Distance(GetObjectVelocity(true), velocity) > PositionThreshold)
                {
                    UpdateVelocity(velocity, false);
                }
            }

            base.OnAuthorityPushTransformState(ref networkTransformState);
        }

        public override void OnNetworkSpawn()
        {
            // When creating customized NetworkTransform behaviors, you must always invoke the base OnNetworkSpawn
            // method if you override it in any child derive generation (i.e. always assure the NetworkTransform.OnNetworkSpawn
            // method is invoked)
            base.OnNetworkSpawn();

            // Assure all colliders are enabled (authority and non-authority)
            EnableColliders(true);

            // Register for contact events (authority and non-authority)
            RigidbodyContactEventManager.Instance.RegisterHandler(this);

            // Clamp the linear and angular velocities
            Rigidbody.maxAngularVelocity = m_MaxAngularVelocity;
            Rigidbody.maxLinearVelocity = m_MaxVelocity;
            if (HasAuthority)
            {
                // Assure we are not still in kinematic mode
                NetworkRigidbody.SetIsKinematic(false);

                // Since state can be preserved during a CMB service connection when there are no clients connected,
                // this section determines whether we need to initialize the physics object or just apply the last
                // known velocities.
#if SESSION_STORE_ENABLED
            if (!BeenInitialized.Value)
#endif
                {
                    m_IsInitialized.Value = true;
                }
#if SESSION_STORE_ENABLED
            else
            {
                Rigidbody.angularVelocity = Vector3.ClampMagnitude(GetObjectAngularVelocity(), MaxAngularVelocity);
                SetObjectVelocity(Vector3.ClampMagnitude(GetObjectVelocity(), MaxVelocity));
            }
#endif
            }
        }

        public override void OnNetworkDespawn()
        {
            RigidbodyContactEventManager.Instance.RegisterHandler(this, false);

            // Invoke the base before applying any additional adjustments
            base.OnNetworkDespawn();

            // If we are pooled and not shutting down, then reset the physics object for re-use later
            // ** Important to do this **
            if (m_IsPooled)
            {
                EnableColliders(false);
                if (!Rigidbody.isKinematic)
                {
                    Rigidbody.angularVelocity = Vector3.zero;
                    SetObjectVelocity(Vector3.zero);
                    NetworkRigidbody.SetIsKinematic(true);
                }

                m_IsInitialized.Reset();
                m_AngularVelocity.Reset();
                m_Velocity.Reset();
                m_Torque.Reset();
                m_Force.Reset();
                m_Mass.Reset();
            }
        }

        /// <summary>
        /// When ownership changes, we apply the last known angular and motion velocities.
        /// Otherwise,
        /// </summary>
        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            if (NetworkManager.LocalClientId == current)
            {
                NetworkRigidbody.SetIsKinematic(false);
                if (m_IsInitialized.Value)
                {
                    Rigidbody.angularVelocity = Vector3.ClampMagnitude(GetObjectAngularVelocity(), m_MaxAngularVelocity);
                    SetObjectVelocity(Vector3.ClampMagnitude(GetObjectVelocity(true), m_MaxVelocity));
                }
                else
                {
                    Rigidbody.AddTorque(m_Torque.Value, ForceMode.Impulse);
                    Rigidbody.AddForce(m_Force.Value, ForceMode.Impulse);
                }
            }
            else
            {
                NetworkRigidbody.SetIsKinematic(true);
            }

            base.OnOwnershipChanged(previous, current);
        }

        /// <summary>
        /// Handles queuing up incoming collisions (remote and local) to be processed
        /// </summary>
        protected override void OnHandleCollision(CollisionMessageInfo collisionMessage, bool isLocal = false, bool applyImmediately = false)
        {
            if (collisionMessage.HasCollisionForce())
            {
                AddForceDirect(collisionMessage.CollisionForce);
            }

            base.OnHandleCollision(collisionMessage, isLocal, applyImmediately);
        }

        void AddForceDirect(Vector3 force)
        {
            var remoteForce = new RemoteForce()
            {
                TargetForce = force,
                AppliedForce = Vector3.zero,
            };

            m_RemoteAppliedForce.Add(remoteForce);
        }

        protected override void OnContactEvent(ulong eventId, Vector3 averagedCollisionNormal, Rigidbody collidingBody, Vector3 contactPoint, bool hasCollisionStay = false, Vector3 averagedCollisionStayNormal = default)
        {
            var collidingBaseObjectMotion = collidingBody.GetComponent<BaseObjectMotionHandler>();
            var collidingBodyPhys = collidingBaseObjectMotion as PhysicsObjectMotion;

            // If we don't have authority over either object or we are doing a second FixedUpdate pass, then exit early
            if (eventId == LastEventId || collidingBaseObjectMotion == null || (!HasAuthority && !collidingBaseObjectMotion.HasAuthority))
            {
                return;
            }

            if (collidingBodyPhys == null || !collidingBodyPhys.IsSpawned)
            {
                return;
            }

            var collisionNormal = hasCollisionStay ? averagedCollisionStayNormal : averagedCollisionNormal;

            var thisVelocity = (!Rigidbody.isKinematic ? Rigidbody.linearVelocity.sqrMagnitude : GetObjectVelocity().sqrMagnitude) * 0.5f;
            var otherVelocity = (!collidingBody.isKinematic ? collidingBody.linearVelocity.sqrMagnitude : collidingBodyPhys.GetObjectVelocity().sqrMagnitude) * 0.5f;
            var thisKineticForce = (Rigidbody.mass / collidingBody.mass) * -collisionNormal * thisVelocity;
            var otherKineticForce = (collidingBody.mass / Rigidbody.mass) * collisionNormal * otherVelocity;

            if (!Rigidbody.isKinematic && collidingBody.isKinematic && thisVelocity > 0.01f)
            {
                m_CollisionMessage.CollisionForce = thisKineticForce;
                m_CollisionMessage.SetFlag(true, (uint)CollisionCategoryFlags.CollisionForce);
                if (m_DebugCollisions)
                {
                    Debug.Log($"[{name}][SecondBody][Collision Stay: {hasCollisionStay}] Sending impulse thrust {MathUtils.GetVector3Values(thisKineticForce)} to {collidingBody.name}.", this);
                }

                // Send collision to owner of kinematic body
                EventCollision(averagedCollisionNormal, collidingBodyPhys);
            }

            if (Rigidbody.isKinematic && !collidingBody.isKinematic && otherVelocity > 0.01f)
            {
                // our kinematic Rigidbody was hit by a non-kinematic physics-moving Rigidbody

                collidingBodyPhys.m_CollisionMessage.CollisionForce = otherKineticForce;
                collidingBodyPhys.m_CollisionMessage.SetFlag(true, (uint)CollisionCategoryFlags.CollisionForce);
                collidingBodyPhys.EventCollision(averagedCollisionNormal, this);
                if (m_DebugCollisions)
                {
                    Debug.Log($"[{collidingBodyPhys.name}][FirstBody][Collision Stay: {hasCollisionStay}] Sending impulse thrust {MathUtils.GetVector3Values(otherKineticForce)} to {name}.", this);
                }
            }
            else if (!Rigidbody.isKinematic && !collidingBody.isKinematic && otherVelocity > 0.01f)
            {
                // Both bodies are non-kinematic (i.e. local client has authority over both) so we create and process event collisions for both
                m_CollisionMessage.CollisionForce = thisKineticForce;
                m_CollisionMessage.SetFlag(true, (uint)CollisionCategoryFlags.CollisionForce);

                // Send collision to owner of kinematic body
                EventCollision(averagedCollisionNormal, collidingBodyPhys);
                if (m_DebugCollisions)
                {
                    Debug.Log($"[{name}][SecondBody][Collision Stay: {hasCollisionStay}] Sending impulse thrust {MathUtils.GetVector3Values(thisKineticForce)} to {collidingBody.name}.", this);
                }
            }

            base.OnContactEvent(eventId, averagedCollisionNormal, collidingBody, contactPoint, hasCollisionStay, averagedCollisionStayNormal);
        }

        /// <summary>
        /// Accumulative-ly apply the resultant collision force
        /// </summary>
        /// <param name="force"></param>s
        void ApplyCollisionForce(Vector3 force)
        {
            Rigidbody.AddForce(force, ForceMode.Impulse);
            Rigidbody.AddTorque(force * 0.25f, ForceMode.Impulse);
        }

        /// <summary>
        /// Processes the queued collisions forces
        /// </summary>
        void ProcessRemoteForces()
        {
            if (m_RemoteAppliedForce.Count == 0)
            {
                return;
            }

            var accumulativeForce = Vector3.zero;
            for (int i = m_RemoteAppliedForce.Count - 1; i >= 0; i--)
            {
                var remoteForce = m_RemoteAppliedForce[i];
                accumulativeForce += remoteForce.TargetForce;
                if (MathUtils.Approximately(remoteForce.TargetForce, Vector3.zero))
                {
                    m_RemoteAppliedForce.RemoveAt(i);
                }
                else
                {
                    m_RemoteAppliedForce[i] = remoteForce;
                }
            }

            ApplyCollisionForce(accumulativeForce);
            m_RemoteAppliedForce.Clear();
        }

        /// <summary>
        /// Hijack the FixedUpdate to assure physics simulation is always
        /// taking into consideration the queued collisions to process
        /// </summary>
        /// <remarks>
        /// Override this method to apply additional forces to your physics object
        /// </remarks>
        protected virtual void FixedUpdate()
        {
            if (!IsSpawned || !HasAuthority || Rigidbody != null && Rigidbody.isKinematic)
            {
                return;
            }

            // Process any queued collisions
            ProcessRemoteForces();
        }

        /// <summary>
        /// When <see cref="BaseObjectMotionHandler.m_DebugCollisions"/> is enabled, this will log locally
        /// generated collision info for the <see cref="PhysicsObjectMotion"/> derived component
        /// </summary>
        /// <param name="objectHit">the <see cref="BaseObjectMotionHandler"/> hit</param>
        /// <returns>log string</returns>
        protected override string OnLogCollision(ref BaseObjectMotionHandler objectHit)
        {
            return $"[CF: {MathUtils.GetVector3Values(ref m_CollisionMessage.CollisionForce)}]-{base.OnLogCollision(ref objectHit)}";
        }

        /// <summary>
        /// When <see cref="BaseObjectMotionHandler.m_DebugCollisions"/> is enabled, this will log remotely
        /// received collision info for the <see cref="PhysicsObjectMotion"/> derived component
        /// </summary>
        /// <param name="collisionMessage">the message received</param>
        /// <returns>log string</returns>
        protected override string OnLogHandleCollision(ref CollisionMessageInfo collisionMessage)
        {
            var sourceCollider = $"{collisionMessage.SourceOwner}";
            if (NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(collisionMessage.SourceOwner))
            {
                sourceCollider = NetworkManager.SpawnManager.SpawnedObjects[collisionMessage.SourceOwner].name;
            }

            var resLinearVel = string.Empty;
            var resAngularVel = string.Empty;
            if (Rigidbody != null)
            {
                resLinearVel = MathUtils.GetVector3Values(GetObjectVelocity());
                resAngularVel = MathUtils.GetVector3Values(Rigidbody.angularVelocity);
            }

            return $"[**Collision-Info**][To: {name}][By:{sourceCollider}][Force:{MathUtils.GetVector3Values(ref collisionMessage.CollisionForce)}]" +
                $"[LinVel: {resLinearVel}][AngVel: {resAngularVel}]-{base.OnLogHandleCollision(ref collisionMessage)}";
        }
    }

    struct RemoteForce
    {
        public float EndOfLife;
        public Vector3 TargetForce;
        public Vector3 AppliedForce;
    }
}
