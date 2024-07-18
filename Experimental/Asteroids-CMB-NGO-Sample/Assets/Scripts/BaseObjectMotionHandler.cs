using System;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections.Generic;

#if UNITY_EDITOR
using Unity.Netcode.Editor;
using UnityEditor;
/// <summary>
/// The custom editor for the <see cref="BaseObjectMotionHandler"/> component.
/// </summary>
[CustomEditor(typeof(BaseObjectMotionHandler), true)]
public class BaseObjectMotionHandlerEditor : NetworkTransformEditor
{
    private SerializedProperty m_IsPooled;
    private SerializedProperty m_CollisionType;
    private SerializedProperty m_CollisionDamage;
    private SerializedProperty m_DebugCollisions;
    private SerializedProperty m_DebugDamage;
    private SerializedProperty m_EnableBoundary;
    private SerializedProperty m_Colliders;
    
    public override void OnEnable()
    {
        m_IsPooled = serializedObject.FindProperty(nameof(BaseObjectMotionHandler.IsPooled));
        m_Colliders = serializedObject.FindProperty(nameof(BaseObjectMotionHandler.Colliders));
        m_CollisionType = serializedObject.FindProperty(nameof(BaseObjectMotionHandler.CollisionType));
        m_CollisionDamage = serializedObject.FindProperty(nameof(BaseObjectMotionHandler.CollisionDamage));
        m_DebugCollisions = serializedObject.FindProperty(nameof(BaseObjectMotionHandler.DebugCollisions));
        m_DebugDamage = serializedObject.FindProperty(nameof(BaseObjectMotionHandler.DebugDamage));
        m_EnableBoundary = serializedObject.FindProperty(nameof(BaseObjectMotionHandler.EnableBoundary));

        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        var baseObject = target as BaseObjectMotionHandler;
        baseObject.BaseObjectMotionHandlerPropertiesVisible = EditorGUILayout.BeginFoldoutHeaderGroup(baseObject.BaseObjectMotionHandlerPropertiesVisible, $"{nameof(BaseObjectMotionHandler)} Properties");
        if (baseObject.BaseObjectMotionHandlerPropertiesVisible)
        {
            // End the header group since m_Colliders is a header group
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.PropertyField(m_IsPooled);
            EditorGUILayout.PropertyField(m_Colliders);
            EditorGUILayout.PropertyField(m_CollisionType);
            EditorGUILayout.PropertyField(m_CollisionDamage);
            EditorGUILayout.PropertyField(m_DebugCollisions);
            EditorGUILayout.PropertyField(m_DebugDamage);
            EditorGUILayout.PropertyField(m_EnableBoundary);
        }
        else
        {
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        EditorGUILayout.Space();

        baseObject.NetworkTransformPropertiesVisible = EditorGUILayout.BeginFoldoutHeaderGroup(baseObject.NetworkTransformPropertiesVisible, $"{nameof(NetworkTransform)} Properties");
        if (baseObject.NetworkTransformPropertiesVisible)
        {
            base.OnInspectorGUI();
        }
        else
        {
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
}
#endif

/// <summary>
/// Projectiles will be owner driven
/// </summary>
public partial class BaseObjectMotionHandler : NetworkTransform, ICollisionHandler, IContactEventHandler
{

#if UNITY_EDITOR
    public bool BaseObjectMotionHandlerPropertiesVisible = false;
    public bool NetworkTransformPropertiesVisible = false;
#endif
    private static GameObject WorldBoundary;

    // Defaults to 1024 but can be updated by adding a GameObject named WorldBoundary and adding a SphereCollider to that.
    private static float WorldBoundaryRadius = 1024.0f;

    public bool IsPooled = true;
    public CollisionTypes CollisionType;
    public ushort CollisionDamage;

    public Action OnNetworkObjectDespawned;

    public bool DebugBoundaryCheck;

    protected CollisionMessageInfo CollisionMessage = new CollisionMessageInfo();
    private Rigidbody m_Rigidbody;
    private NetworkRigidbody m_NetworkRigidbody;

    public Rigidbody Rigidbody => m_Rigidbody;
    public NetworkRigidbody NetworkRigidbody => m_NetworkRigidbody;

    [Tooltip("Enables/Disables collision logging (based on per derived type)")]
    public bool DebugCollisions;

    [Tooltip("Enables/Disables damage logging (based on per derived type)")]
    public bool DebugDamage;

    [HideInInspector]
    public bool IsPhysicsBody;

    [Tooltip("When enabled, all physics bodies will head back towards the center once they reach the boundary limits.")]
    public bool EnableBoundary = true;

    [Tooltip("Add all colliders to this list that will be used to detect collisions (exclude triggers).")]
    public List<Collider> Colliders;

    private Dictionary<Collider, Vector3> ColliderScales = new Dictionary<Collider, Vector3>();

    protected void EnableColliders(bool enable)
    {
        foreach (var collider in Colliders)
        {
            collider.enabled = enable;
        }
    }

    private const float k_BoundaryCheckFrequency = 0.06667f;
    private const int k_BoundaryCheckDistributionRes = 10;
    private static int s_BoundaryOffsetCount = 1;
    private float m_NextBoundaryCheck;

    public Rigidbody GetRigidbody() { return m_Rigidbody; }

    protected virtual Vector3 OnGetObjectVelocity(bool getReference = false)
    {
        if (m_Rigidbody != null)
        {
#if UNITY_2023_3_OR_NEWER
            return m_Rigidbody.linearVelocity;
#else
            return m_Rigidbody.velocity;
#endif
        }
        return Vector3.zero;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 GetObjectVelocity(bool getReference = false)
    {
        return OnGetObjectVelocity(getReference);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetObjectVelocity(Vector3 velocity)
    {
        if (m_Rigidbody != null)
        {
#if UNITY_2023_3_OR_NEWER
            m_Rigidbody.linearVelocity = velocity;
#else
            m_Rigidbody.velocity = velocity;
#endif
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual Vector3 OnGetObjectAngularVelocity()
    {
        if (m_Rigidbody != null)
        {
            return m_Rigidbody.angularVelocity;
        }
        return Vector3.zero;
    }

    public Vector3 GetObjectAngularVelocity()
    {
        return OnGetObjectAngularVelocity();
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
                Physics.IgnoreCollision(colliderA, colliderB, shouldIgnore);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        OnNetworkObjectDespawned?.Invoke();
        OnNetworkObjectDespawned = null;

        var fxObjects = GetComponentsInChildren<BaseFxObject>();
        foreach (var fxObject in fxObjects)
        {
            fxObject.transform.SetParent(null);
        }
    }

    /// <summary>
    /// Override this method to make adjustments for wrapping
    /// </summary>
    protected virtual bool OnBoundaryReached()
    {
        return true;
    }

    // Distribute the boundary check processing evenly amongst all instances 
    private void SetNextBoundaryCheck(float timeOffset = k_BoundaryCheckFrequency, bool init = false)
    {
        if (init)
        {
            s_BoundaryOffsetCount++;
            m_NextBoundaryCheck = Time.realtimeSinceStartup + (timeOffset * (s_BoundaryOffsetCount % k_BoundaryCheckDistributionRes));
        }
        else
        {
            m_NextBoundaryCheck = Time.realtimeSinceStartup + timeOffset;
        }
    }

    private void CheckBoundary()
    {
        if (Rigidbody == null || Rigidbody != null && Rigidbody.isKinematic)
        {
            return;
        }
        if (EnableBoundary && m_NextBoundaryCheck < Time.realtimeSinceStartup)
        {
            var distance = Vector3.Distance(Vector3.zero, transform.position);
            // if we reached the maximum boundary, then reverse the velocity of the Rigidbody if it has one
            if (distance >= WorldBoundaryRadius && OnBoundaryReached())
            {

                var dir = Vector3.zero - transform.position;
                var velocity = GetObjectVelocity();
                velocity = velocity.magnitude * dir;
                SetObjectVelocity(velocity);
            }
            else
            {
                SetNextBoundaryCheck();
            }
        }
    }

    protected override void Awake()
    {
        if (WorldBoundary == null)
        {
            WorldBoundary = GameObject.Find("WorldBoundary");
            if (WorldBoundary != null)
            {
                var sphereCollider = WorldBoundary.GetComponent<SphereCollider>();
                WorldBoundaryRadius = sphereCollider.radius;
            }
        }

        m_Rigidbody = GetComponent<Rigidbody>();
        m_NetworkRigidbody = GetComponent<NetworkRigidbody>();

        SetNextBoundaryCheck(init: true);

        base.Awake();
    }

    protected virtual void Start()
    {
        CollisionMessage.Damage = CollisionDamage;
        CollisionMessage.SetFlag(true, (uint)CollisionType);
    }

    /// <summary>
    /// Invoked every network tick if this instance has sent a <see cref="NetworkTransform.NetworkTransformState"/> update.
    /// </summary>
    /// <param name="networkTransformState"></param>
    protected override void OnAuthorityPushTransformState(ref NetworkTransformState networkTransformState)
    {
        CheckBoundary();
        base.OnAuthorityPushTransformState(ref networkTransformState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static public GameObject GetRootParent(GameObject parent)
    {
        return parent.transform.root.gameObject;
    }

    /// <summary>
    /// This method provides the ability to make adjustments to the collision message as well as apply damage locally if needed
    /// </summary>
    /// <param name="collision"></param>
    /// <param name="targetNetworkObject"></param>
    protected virtual bool OnPrepareCollisionMessage(Vector3 averagedCollisionNormal, BaseObjectMotionHandler targetBaseObjectMotionHandler)
    {
        return true;
    }

    protected virtual void OnHandleCollision(CollisionMessageInfo collisionMessage, bool isLocal = false, bool applyImmediately = false)
    {
    }

    public void HandleCollision(CollisionMessageInfo collisionMessage, bool isLocal = false, bool applyImmediately = false)
    {
        OnHandleCollision(collisionMessage, isLocal, applyImmediately);
        // Hanlding is invoked before logging so logging can determine the end result.
        if (DebugCollisions)
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
            SendCollisionMessage(CollisionMessage);
            return;
        }

        CollisionMessage.SourceOwner = rpcParams.Receive.SenderClientId;
        CollisionMessage.TargetOwner = OwnerClientId;
        HandleCollision(collisionMessage);
    }

    /// <summary>
    /// Invoked by the owner of the object inflicting damage, this will handle the RPC routing
    /// of the message to the appropriate targeted owner of the object taking damage
    /// </summary>
    /// <param name="projectileCollisionInfo"></param>
    public void SendCollisionMessage(CollisionMessageInfo collisionMessage)
    {
        LogDamage(collisionMessage);
        HandleCollisionRpc(collisionMessage);
    }

    /// <summary>
    /// Override this method if you have registerd the instance with <see cref="RigidbodyContactEventManager"/> and
    /// want to customize collision.
    /// </summary>
    /// <remarks>
    /// Only <see cref="PhysicsObjectMotion"/> automatically handles collisions. For an example of a customized contact event
    /// handler look over <see cref="LaserMotion"/>.
    /// </remarks>
    /// <param name="averageNormal">The average normal of the collisions contacts</param>
    /// <param name="collidingBody">The <see cref="Rigidbody"/> that collided with this object.</param>
    protected virtual void OnContactEvent(ulong eventId, Vector3 averagedCollisionNormal, Rigidbody collidingBody, Vector3 contactPoint, bool hasCollisionStay = false, Vector3 averagedCollisionStayNormal = default)
    {

    }

    protected ulong LastEventId { get; private set; }
    /// <summary>
    /// Invoked from <see cref="RigidbodyContactEventManager"/> when a non-kinematic body collides
    /// with another registered <see cref="UnityEngine.Rigidbody"/>.
    /// </summary>
    /// <param name="averageNormal">The averaged normal of the collision</param>
    /// <param name="collidingBody">The <see cref="Rigidbody"/> this objects collided with</param>
    public void ContactEvent(ulong eventId, Vector3 averageNormal, Rigidbody collidingBody, Vector3 contactPoint, bool hasCollisionStay = false, Vector3 averagedCollisionStayNormal = default)
    {
        if (!IsSpawned)
        {
            return;
        }
        OnContactEvent(eventId, averageNormal, collidingBody, contactPoint, hasCollisionStay, averagedCollisionStayNormal);
        LastEventId = eventId;
    }


    /// <summary>
    /// Invoked this to send a collision message to the authoritative instance.
    /// </summary>
    /// <param name="force"></param>
    /// <param name="colldingBodyBaseObject"></param>
    protected void EventCollision(Vector3 averagedCollisionNormal, BaseObjectMotionHandler collidingBodyBaseObject)
    {
#if DEBUG || UNITY_EDITOR
        if (DebugCollisions)
        {
            LogCollision(ref collidingBodyBaseObject);
        }
#endif

        if (OnPrepareCollisionMessage(averagedCollisionNormal, collidingBodyBaseObject))
        {
            CollisionId++;
            CollisionMessage.CollisionId = CollisionId;
            CollisionMessage.Time = Time.realtimeSinceStartup;
            CollisionMessage.Source = OwnerClientId;
            CollisionMessage.SourceId = NetworkObjectId;
            CollisionMessage.Destination = collidingBodyBaseObject.OwnerClientId;
            CollisionMessage.DestNetworkObjId = collidingBodyBaseObject.NetworkObjectId;
            CollisionMessage.DestBehaviourId = collidingBodyBaseObject.NetworkBehaviourId;

            // Otherwise, send the collision message to the owner of the object
            collidingBodyBaseObject.SendCollisionMessage(CollisionMessage);
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

    private static int CollisionId = 0;
    private void LogCollision(ref BaseObjectMotionHandler objectHit)
    {
        if (!DebugCollisions)
        {
            return;
        }
        var distance = Vector3.Distance(transform.position, objectHit.transform.position);
        NetworkManagerHelper.Instance.LogMessage($"[{Time.realtimeSinceStartup}][LocalCollision][{name}][collided with][{objectHit.name}][Collider:{name}][Distance: {distance}]" +
            $"{OnLogCollision(ref objectHit)}.");
    }

    protected virtual string OnLogDamage(CollisionMessageInfo collisionMessage)
    {
        return string.Empty;
    }

    protected void LogDamage(CollisionMessageInfo collisionMessage)
    {
        if (!DebugDamage || collisionMessage.Damage == 0)
        {
            return;
        }
        var additionalInfo = OnLogDamage(collisionMessage);
        NetworkManagerHelper.Instance.LogMessage($"[{name}][++Damaged++][Client-{collisionMessage.TargetOwner}][{collisionMessage.GetCollisionType()}][Dmg:{collisionMessage.Damage}] {additionalInfo}");
    }

    /// <summary>
    /// Override to log incoming collision messages
    /// </summary>
    /// <param name="collisionMessage"></param>
    protected virtual string OnLogHandleCollision(ref CollisionMessageInfo collisionMessage)
    {
        return "[CollisionMessage-End]";
    }

    private void LogHandleCollision(CollisionMessageInfo collisionMessage, bool isLocal = false)
    {
        var distance = -1.0f;
        if (NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(collisionMessage.DestNetworkObjId))
        {
            distance = Vector3.Distance(transform.position, NetworkManager.SpawnManager.SpawnedObjects[collisionMessage.DestNetworkObjId].transform.position);
        }
        var distStr = distance == -1.0f ? $"{collisionMessage.DestNetworkObjId} DNE!!" : $"Distance: {distance}";
        NetworkManagerHelper.Instance.LogMessage($"[{collisionMessage.CollisionId}][{collisionMessage.Time}][CollisionMessage][IsLocal: {isLocal}][{name}][Src:{collisionMessage.Source}][Dest:{collisionMessage.Destination}]" +
            $"[NObjId:{collisionMessage.DestNetworkObjId}][NBvrId:{collisionMessage.DestBehaviourId}][{distStr}]{OnLogHandleCollision(ref collisionMessage)}.");
    }

    protected void LogMessage(string msg, bool forceMessage = false, float messageTime = 10.0f)
    {
        NetworkManagerHelper.Instance.LogMessage($"[{name}]{msg}", messageTime, forceMessage);
    }

    #endregion

    #region VECTOR AND EULER HELPER METHODS
    /// <summary>
    /// Enable this to get 6 decimal precision when logging Vector3 values
    /// </summary>
    private bool m_HigPrecisionDecimals = false;
    protected string GetVector3Values(ref Vector3 vector3)
    {
        if (m_HigPrecisionDecimals)
        {
            return $"({vector3.x:F6},{vector3.y:F6},{vector3.z:F6})";
        }
        else
        {
            return $"({vector3.x:F2},{vector3.y:F2},{vector3.z:F2})";
        }
    }

    protected string GetVector3Values(Vector3 vector3)
    {
        return GetVector3Values(ref vector3);
    }

    protected Vector3 GetRandomVector3(float min, float max, Vector3 baseLine, bool randomlyApplySign = false)
    {
        var retValue = new Vector3(baseLine.x * Random.Range(min, max), baseLine.y * Random.Range(min, max), baseLine.z * Random.Range(min, max));
        if (!randomlyApplySign)
        {
            return retValue;
        }

        retValue.x *= Random.Range(1, 100) >= 50 ? -1 : 1;
        retValue.y *= Random.Range(1, 100) >= 50 ? -1 : 1;
        retValue.z *= Random.Range(1, 100) >= 50 ? -1 : 1;
        return retValue;
    }

    protected Vector3 GetRandomVector3(MinMaxVector2Physics minMax, Vector3 baseLine, bool randomlyApplySign = false)
    {
        return GetRandomVector3(minMax.Min, minMax.Max, baseLine, randomlyApplySign);
    }

    private const float k_DefaultThreshold = 0.0025f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool Approximately(float a, float b, float threshold = k_DefaultThreshold)
    {
        return Math.Round(Mathf.Abs(a - b), 4) <= threshold;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool Approximately(Vector3 a, Vector3 b, float threshold = k_DefaultThreshold)
    {
        return Approximately(a.x, b.x) && Approximately(a.y, b.y) && Approximately(a.z, b.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool Approximately(Quaternion a, Quaternion b, float threshold = k_DefaultThreshold)
    {
        return Approximately(a.x, b.x) && Approximately(a.y, b.y) && Approximately(a.z, b.z) && Approximately(a.w, b.w);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected float EulerDelta(float a, float b)
    {
        return Mathf.DeltaAngle(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool ApproximatelyEuler(float a, float b, float threshold = k_DefaultThreshold)
    {
        return Mathf.Abs(EulerDelta(a, b)) <= threshold;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool ApproximatelyEuler(Vector3 a, Vector3 b, float threshold = k_DefaultThreshold)
    {
        return ApproximatelyEuler(a.x, b.x, threshold) && ApproximatelyEuler(a.y, b.y, threshold) && ApproximatelyEuler(a.z, b.z, threshold);
    }
    #endregion
}

[Serializable]
public class MinMaxVector2Physics
{
    [Range(1.0f, 200.0f)]
    public float Min;
    [Range(1.0f, 200.0f)]
    public float Max;

    public MinMaxVector2Physics(float min, float max)
    {
        Min = min;
        Max = max;
    }
}