using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
/// <summary>
/// The custom editor for the <see cref="PhysicsObjectMotion"/> component.
/// </summary>
[CustomEditor(typeof(PhysicsObjectMotion), true)]
public class PhysicsObjectMotionEditor : BaseObjectMotionHandlerEditor
{
    private SerializedProperty m_CollisionImpulseEntries;
    private SerializedProperty m_MaxAngularVelocity;
    private SerializedProperty m_MaxVelocity;
    private SerializedProperty m_MinMaxStartingTorque;
    private SerializedProperty m_MinMaxStartingForce;

    public override void OnEnable()
    {
        m_CollisionImpulseEntries = serializedObject.FindProperty(nameof(PhysicsObjectMotion.CollisionImpulseEntries));
        m_MaxAngularVelocity = serializedObject.FindProperty(nameof(PhysicsObjectMotion.MaxAngularVelocity));
        m_MaxVelocity = serializedObject.FindProperty(nameof(PhysicsObjectMotion.MaxVelocity));
        m_MinMaxStartingTorque = serializedObject.FindProperty(nameof(PhysicsObjectMotion.MinMaxStartingTorque));
        m_MinMaxStartingForce = serializedObject.FindProperty(nameof(PhysicsObjectMotion.MinMaxStartingForce));
        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        var physicsObject = target as PhysicsObjectMotion;

        physicsObject.PhysicsObjectMotionPropertiesVisible = EditorGUILayout.BeginFoldoutHeaderGroup(physicsObject.PhysicsObjectMotionPropertiesVisible, $"{nameof(PhysicsObjectMotion)} Properties");
        if (physicsObject.PhysicsObjectMotionPropertiesVisible)
        {
            // End the header group since m_MinMaxStartingTorque and m_MinMaxStartingForce both use header groups
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.PropertyField(m_CollisionImpulseEntries);
            EditorGUILayout.PropertyField(m_MaxAngularVelocity);
            EditorGUILayout.PropertyField(m_MaxVelocity);
            EditorGUILayout.PropertyField(m_MinMaxStartingTorque);
            EditorGUILayout.PropertyField(m_MinMaxStartingForce);
        }
        else
        {
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        EditorGUILayout.Space();

        base.OnInspectorGUI();
    }
}
#endif


public partial class PhysicsObjectMotion : BaseObjectMotionHandler
{
#if UNITY_EDITOR
    public bool PhysicsObjectMotionPropertiesVisible = false;
#endif

    [Serializable]
    public struct CollisionImpulseMultiplierEntry
    {
        public CollisionTypes CollisionType;
        public float MaxCollisionForce;
    }
    public List<CollisionImpulseMultiplierEntry> CollisionImpulseEntries;
    private Dictionary<CollisionTypes, CollisionImpulseMultiplierEntry> CollisionImpulseTable;

    public float MaxAngularVelocity = 30;
    public float MaxVelocity = 30;
    [HideInInspector]
    public float StartingMass = 1.0f;

    public const float MaxMass = 5.0f;
    public const float MinMass = 0.10f;

    public MinMaxVector2Physics MinMaxStartingTorque = new MinMaxVector2Physics(5.0f, 15.0f);
    public MinMaxVector2Physics MinMaxStartingForce = new MinMaxVector2Physics(5.0f, 30.0f);


    protected NetworkVariable<bool> BeenInitialized = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    /// <summary>
    /// All of the below values keep the physics objects synchronized between clients so when ownership changes the local Rigidbody can be configured to mirror
    /// the last known physics related states.
    /// </summary>
    protected NetworkVariable<float> Mass = new NetworkVariable<float>(1.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<Vector3> AngularVelocity = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<Vector3> Velocity = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<Vector3> Torque = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<Vector3> Force = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);



    protected override void Awake()
    {
        base.Awake();
        CollisionImpulseTable = new Dictionary<CollisionTypes, CollisionImpulseMultiplierEntry>();
        foreach (var entry in CollisionImpulseEntries)
        {
            if (!CollisionImpulseTable.ContainsKey(entry.CollisionType))
            {
                CollisionImpulseTable.Add(entry.CollisionType, entry);
            }
            else
            {
                Debug.LogWarning($"[Duplicate Entry] A duplicate {nameof(CollisionImpulseMultiplierEntry)} of type {entry.CollisionType} was detected! Ignoring entry.");
            }
        }

        StartingMass = Rigidbody.mass;
    }

    protected override Vector3 OnGetObjectVelocity(bool getReference = false)
    {
        if (getReference)
        {
            return Velocity.Value;
        }
        return base.OnGetObjectVelocity(getReference);
    }

    protected override Vector3 OnGetObjectAngularVelocity()
    {
        return AngularVelocity.Value;
    }

    protected void UpdateVelocity(Vector3 velocity, bool updateObjectVelocity = true)
    {
        if (HasAuthority)
        {
            if (updateObjectVelocity)
            {
                SetObjectVelocity(velocity);
            }
            Velocity.Value = velocity;
        }
    }

    protected void UpdateAngularVelocity(Vector3 angularVelocity)
    {
        if (HasAuthority)
        {
            Rigidbody.angularVelocity = angularVelocity;
            AngularVelocity.Value = angularVelocity;
        }
    }

    protected void UpdateTorque(Vector3 torque)
    {
        if (HasAuthority)
        {
            Rigidbody.AddTorque(torque);
            Torque.Value = torque;
        }
    }

    protected void UpdateImpulseForce(Vector3 impulseForce)
    {
        if (HasAuthority)
        {
            Rigidbody.AddForce(impulseForce, ForceMode.Impulse);
            Force.Value = impulseForce;
        }
    }

    protected void UpdateMass(float mass)
    {
        if (HasAuthority)
        {
            if (mass > MinMass && mass < MaxMass)
            {
                Rigidbody.mass = mass;
            }
            else
            {
                NetworkLog.LogWarningServer($"[{name}] Trying to assign mass of {mass} which is outside the mass boundary of {MinMass} to {MaxMass}! Clamping.");
                Rigidbody.mass = Mathf.Clamp(mass, MinMass, MaxMass);
            }
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
        if (!BeenInitialized.Value)
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

        IsPhysicsBody = true;
        // Assure all colliders are enabled (authority and non-authority)
        EnableColliders(true);

        // Register for contact events (authority and non-authority)
        RigidbodyContactEventManager.Instance.RegisterHandler(this);

        // Clamp the linear and angular velocities
        Rigidbody.maxAngularVelocity = MaxAngularVelocity;
        Rigidbody.maxLinearVelocity = MaxVelocity;
        if (HasAuthority)
        {

            // Assure we are not still in kinematic mode
            NetworkRigidbody.SetIsKinematic(false);

            // Since state can be preserved during a CMB service connection when there are no clients connected,
            // this section determines whether we need to initialize the physics object or just apply the last
            // known velocities.
            if (!BeenInitialized.Value)
            {
                BeenInitialized.Value = true;
                var torque = GetRandomVector3(MinMaxStartingTorque, Vector3.one, true);
                Rigidbody.AddTorque(torque, ForceMode.Impulse);
                UpdateTorque(torque);
                var force = GetRandomVector3(MinMaxStartingForce, Vector3.one, true);
                force.y = 0f;
                Rigidbody.AddForce(force, ForceMode.Impulse);
                UpdateImpulseForce(force);
            }
            else
            {
                Rigidbody.angularVelocity = Vector3.ClampMagnitude(GetObjectAngularVelocity(), MaxAngularVelocity);
                SetObjectVelocity(Vector3.ClampMagnitude(GetObjectVelocity(), MaxVelocity));
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        RigidbodyContactEventManager.Instance.RegisterHandler(this, false);
        // Invoke the base before applying any additional adjustments
        base.OnNetworkDespawn();

        // If we are pooled and not shutting down, then reset the physics object for re-use later
        // ** Important to do this **
        if (IsPooled)
        {
            EnableColliders(false);
            if (!Rigidbody.isKinematic)
            {
                Rigidbody.angularVelocity = Vector3.zero;
                SetObjectVelocity(Vector3.zero);
                NetworkRigidbody.SetIsKinematic(true);
            }
            Rigidbody.mass = StartingMass;
            BeenInitialized.Reset();
            AngularVelocity.Reset();
            Velocity.Reset();
            Torque.Reset();
            Force.Reset();
            Mass.Reset();
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
            if (BeenInitialized.Value)
            {
                Rigidbody.angularVelocity = Vector3.ClampMagnitude(GetObjectAngularVelocity(), MaxAngularVelocity);
                SetObjectVelocity(Vector3.ClampMagnitude(GetObjectVelocity(true), MaxVelocity));
            }
            else
            {
                Rigidbody.AddTorque(Torque.Value, ForceMode.Impulse);
                Rigidbody.AddForce(Force.Value, ForceMode.Impulse);
            }
        }
        else
        {
            NetworkRigidbody.SetIsKinematic(true);
        }
        base.OnOwnershipChanged(previous, current);
    }

    private struct RemoteForce
    {
        public float EndOfLife;
        public Vector3 TargetForce;
        public Vector3 AppliedForce;
    }

    private List<RemoteForce> m_RemoteAppliedForce = new List<RemoteForce>();
    private Dictionary<ulong, float> m_CollisionLatency = new Dictionary<ulong, float>();

    /// <summary>
    /// Handles queuing up incoming collisions (remote and local) to be processed
    /// </summary>
    protected override void OnHandleCollision(CollisionMessageInfo collisionMessage, bool isLocal = false, bool applyImmediately = false)
    {
        if (collisionMessage.HasCollisionForce())
        {
            AddForceDirect(collisionMessage.CollisionForce);
        }
        base.OnHandleCollision(collisionMessage);
    }

    public void AddForceDirect(Vector3 force)
    {
        var remoteForce = new RemoteForce()
        {
            TargetForce = force,
            AppliedForce = Vector3.zero,
        };


        m_RemoteAppliedForce.Add(remoteForce);
    }

    /// <summary>
    /// TODO: 
    /// - Track body collisions and the recent kineticForce applied.
    /// - Cap the total applied kineticForce to (n) value.
    /// - Only update the delta kineticForce relative to recent collision messages sent to another body
    ///   - This should help with the "sudden" bursts of force when colliding against other bodies.
    /// </summary>
    protected void OnContactEventOld(ulong eventId, Vector3 averagedCollisionNormal, Rigidbody collidingBody, Vector3 contactPoint, bool hasCollisionStay = false, Vector3 averagedCollisionStayNormal = default)
    {
        // TODO: Possibly come up with a better way to route contact events at this level.
        // For now, since lasers are always kinematic and send damage messages we divert contact events to the LaserMotion child class
        var collidingBaseObjectMotion = collidingBody.GetComponent<BaseObjectMotionHandler>();
        var collidingBodyPhys = collidingBaseObjectMotion as PhysicsObjectMotion;
        // If we don't have authority over either object or we are doing a second FixedUpdate pass, then exit early
        if (eventId == LastEventId || collidingBaseObjectMotion == null || (!HasAuthority && !collidingBaseObjectMotion.HasAuthority))
        {
            return;
        }
        if (collidingBaseObjectMotion.CollisionType == CollisionTypes.Laser)
        {
            var laserMotion = collidingBaseObjectMotion as LaserMotion;
            laserMotion.ContactEvent(eventId, averagedCollisionNormal, Rigidbody, contactPoint);
            return;
        }

        if (collidingBodyPhys == null)
        {
            return;
        }

        var collisionNormal = hasCollisionStay ? averagedCollisionStayNormal : averagedCollisionNormal;
        var velocity = 0.0f;
        var kineticForce = Vector3.zero;
        var massRatio = 0.0f;

        if (!Rigidbody.isKinematic && collidingBody.isKinematic)
        {
            // Handle kinematic to non-kinematic local collisions
            if (collidingBodyPhys.CollisionType == CollisionTypes.Ship && hasCollisionStay)
            {
                velocity = collidingBodyPhys.MaxVelocity * 0.85f * (collidingBodyPhys as ShipController).GetPrimaryThrusterScale();
            }
            else
            {
                velocity = collidingBodyPhys.GetObjectVelocity(true).sqrMagnitude * 0.5f;
            }

            if (velocity > 0.01f)
            {
                // Get the mass ratio between the non-kinematic and kinematic when applying to local Rigidbody
                massRatio = (Rigidbody.mass / collidingBody.mass);
                // Get the over-all kinetic force to appply
                // Use the original normal when applying to the local physics body
                kineticForce = massRatio * velocity * collisionNormal;
                Rigidbody.AddForce(kineticForce, ForceMode.Impulse);
                if (DebugCollisions)
                {
                    if (kineticForce.magnitude < 1.0f)
                    {
                        NetworkManagerHelper.Instance.LogMessage($"[{name}][FirstBody] Mass Ratio: {massRatio} | Collision Normal:{GetVector3Values(-collisionNormal)} | {velocity}");
                    }
                    NetworkManagerHelper.Instance.LogMessage($"[{name}][FirstBody][Collision Stay: {hasCollisionStay}] Collided with {collidingBody.name} that is locally applying {GetVector3Values(kineticForce)} impulse force to {name}.");
                }
            }

            // Handle non-kinematic to kinematic remote collisions
            if (hasCollisionStay)
            {
                if (CollisionType == CollisionTypes.Ship && hasCollisionStay)
                {
                    velocity = MaxVelocity * (this as ShipController).GetPrimaryThrusterScale();
                }
                else
                {
                    velocity = Rigidbody.linearVelocity.sqrMagnitude * 0.5f;
                }

                if (velocity > 0.01f)
                {
                    // Get the mass ratio between the kinematic and non-kinematic when sending to remote Rigidbody
                    massRatio = (collidingBody.mass / Rigidbody.mass);
                    // Get the over-all kinetic force to appply
                    // Invert normal when applying to the colliding physics body
                    kineticForce = velocity * massRatio * -collisionNormal * 0.0333333f;
                    CollisionMessage.CollisionForce = kineticForce;
                    CollisionMessage.SetFlag(true, (uint)CollisionCategoryFlags.CollisionForce);
                    if (DebugCollisions)
                    {
                        if (kineticForce.magnitude < 1.0f)
                        {
                            NetworkManagerHelper.Instance.LogMessage($"[{name}][SecondBody] Mass Ratio: {massRatio} | Collision Normal:{GetVector3Values(-collisionNormal)} | {velocity}");
                        }
                        NetworkManagerHelper.Instance.LogMessage($"[{name}][SecondBody][Collision Stay: {hasCollisionStay}] Sending impulse thrust {GetVector3Values(kineticForce)} to {collidingBody.name}.");
                    }
                    // Send collision to owner of kinematic body
                    EventCollision(averagedCollisionNormal, collidingBodyPhys);
                }
            }
        }
        if (Rigidbody.isKinematic && !collidingBody.isKinematic)
        {
            if (CollisionType == CollisionTypes.Ship && hasCollisionStay)
            {
                velocity = MaxVelocity * (this as ShipController).GetPrimaryThrusterScale();
            }
            else
            {
                velocity = GetObjectVelocity(true).sqrMagnitude * 0.5f;
            }

            if (velocity > 0.01f)
            {
                // Get the mass ratio between the kinematic and non-kinematic when sending to remote Rigidbody
                massRatio = (Rigidbody.mass / collidingBody.mass);
                // Invert normal when applying to the colliding physics body
                kineticForce = massRatio * velocity * -collisionNormal;
                collidingBody.AddForce(kineticForce, ForceMode.Impulse);
                if (DebugCollisions)
                {
                    if (kineticForce.magnitude < 1.0f)
                    {
                        NetworkManagerHelper.Instance.LogMessage($"[{collidingBody.name}][SecondBody] Mass Ratio: {massRatio} | Collision Normal:{GetVector3Values(-collisionNormal)} | {velocity}");
                    }
                    NetworkManagerHelper.Instance.LogMessage($"[{collidingBody.name}][SecondBody][Collision Stay: {hasCollisionStay}] Collided with {name} that is locally applying {GetVector3Values(kineticForce)} impulse force to {collidingBody.name}.");
                }
            }

            if (hasCollisionStay)
            {
                if (collidingBodyPhys.CollisionType == CollisionTypes.Ship && hasCollisionStay)
                {
                    velocity = collidingBodyPhys.MaxVelocity * (collidingBodyPhys as ShipController).GetPrimaryThrusterScale();
                }
                else
                {
                    velocity = collidingBody.linearVelocity.sqrMagnitude * 0.5f;
                }

                if (velocity > 0.01f)
                {
                    massRatio = (Rigidbody.mass / collidingBody.mass);
                    // Get the over-all kinetic force to appply
                    // Use the original normal when applying to the local physics body
                    kineticForce = massRatio * velocity * collisionNormal * 0.0333333f;
                    collidingBodyPhys.CollisionMessage.CollisionForce = kineticForce;
                    collidingBodyPhys.CollisionMessage.SetFlag(true, (uint)CollisionCategoryFlags.CollisionForce);
                    collidingBodyPhys.EventCollision(averagedCollisionNormal, this);
                    if (DebugCollisions)
                    {
                        if (kineticForce.magnitude < 1.0f)
                        {
                            NetworkManagerHelper.Instance.LogMessage($"[{collidingBody.name}][FirstBody] Mass Ratio: {massRatio} | Collision Normal:{GetVector3Values(-collisionNormal)} | {velocity}");
                        }
                        NetworkManagerHelper.Instance.LogMessage($"[{collidingBodyPhys.name}][FirstBody][Collision Stay: {hasCollisionStay}] Sending impulse thrust {GetVector3Values(kineticForce)} to {name}.");
                    }
                }
            }
        }
        base.OnContactEvent(eventId, averagedCollisionNormal, collidingBody, contactPoint);
    }

    protected override void OnContactEvent(ulong eventId, Vector3 averagedCollisionNormal, Rigidbody collidingBody, Vector3 contactPoint, bool hasCollisionStay = false, Vector3 averagedCollisionStayNormal = default)
    {
        // TODO: Possibly come up with a better way to route contact events at this level.
        // For now, since lasers are always kinematic and send damage messages we divert contact events to the LaserMotion child class
        var collidingBaseObjectMotion = collidingBody.GetComponent<BaseObjectMotionHandler>();
        var collidingBodyPhys = collidingBaseObjectMotion as PhysicsObjectMotion;
        // If we don't have authority over either object or we are doing a second FixedUpdate pass, then exit early
        if (eventId == LastEventId || collidingBaseObjectMotion == null || (!HasAuthority && !collidingBaseObjectMotion.HasAuthority))
        {
            return;
        }
        if (collidingBaseObjectMotion.CollisionType == CollisionTypes.Laser)
        {
            var laserMotion = collidingBaseObjectMotion as LaserMotion;
            laserMotion.ContactEvent(eventId, averagedCollisionNormal, Rigidbody, contactPoint);
            return;
        }

        if (collidingBodyPhys == null)
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
            CollisionMessage.CollisionForce = thisKineticForce;
            CollisionMessage.SetFlag(true, (uint)CollisionCategoryFlags.CollisionForce);
            if (DebugCollisions)
            {
                NetworkManagerHelper.Instance.LogMessage($"[{name}][SecondBody][Collision Stay: {hasCollisionStay}] Sending impulse thrust {GetVector3Values(thisKineticForce)} to {collidingBody.name}.");
            }
            // Send collision to owner of kinematic body
            EventCollision(averagedCollisionNormal, collidingBodyPhys);
        }
        else if (Rigidbody.isKinematic && !collidingBody.isKinematic && otherVelocity > 0.01f)
        {
            collidingBodyPhys.CollisionMessage.CollisionForce = otherKineticForce;
            collidingBodyPhys.CollisionMessage.SetFlag(true, (uint)CollisionCategoryFlags.CollisionForce);
            collidingBodyPhys.EventCollision(averagedCollisionNormal, this);
            if (DebugCollisions)
            {
                NetworkManagerHelper.Instance.LogMessage($"[{collidingBodyPhys.name}][FirstBody][Collision Stay: {hasCollisionStay}] Sending impulse thrust {GetVector3Values(otherKineticForce)} to {name}.");
            }
        }
        base.OnContactEvent(eventId, averagedCollisionNormal, collidingBody, contactPoint);
    }

    /// <summary>
    /// Accumulatively apply the resultant collision force
    /// </summary>
    /// <param name="force"></param>
    private void ApplyCollisionForce(Vector3 force)
    {
        Rigidbody.AddForce(force, ForceMode.Impulse);
        Rigidbody.AddTorque(force * 0.25f, ForceMode.Impulse);
    }

    /// <summary>
    /// Processes the queued collisions forces
    /// </summary>
    private void ProcessRemoteForces()
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
            if (Approximately(remoteForce.TargetForce, Vector3.zero))
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
    /// When <see cref="BaseObjectMotionHandler.DebugCollisions"/> is enabled, this will log locally
    /// generated collision info for the <see cref="PhysicsObjectMotion"/> derived component
    /// </summary>
    /// <param name="objectHit">the <see cref="BaseObjectMotionHandler"/> hit</param>
    /// <returns>log string</returns>
    protected override string OnLogCollision(ref BaseObjectMotionHandler objectHit)
    {
        return $"[CF: {GetVector3Values(ref CollisionMessage.CollisionForce)}]-{base.OnLogCollision(ref objectHit)}";
    }

    /// <summary>
    /// When <see cref="BaseObjectMotionHandler.DebugCollisions"/> is enabled, this will log remotely
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
            resLinearVel = GetVector3Values(GetObjectVelocity());
            resAngularVel = GetVector3Values(Rigidbody.angularVelocity);
        }
        return $"[**Collision-Info**][To: {name}][By:{sourceCollider}][Force:{GetVector3Values(ref collisionMessage.CollisionForce)}]" +
            $"[LinVel: {resLinearVel}][AngVel: {resAngularVel}]-{base.OnLogHandleCollision(ref collisionMessage)}";
    }
}