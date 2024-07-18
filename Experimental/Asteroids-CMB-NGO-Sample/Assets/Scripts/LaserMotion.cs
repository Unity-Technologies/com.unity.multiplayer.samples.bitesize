using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
/// <summary>
/// The custom editor for the <see cref="LaserMotion"/> component.
/// </summary>
[CustomEditor(typeof(LaserMotion), true)]
public class LaserMotionEditor : BaseObjectMotionHandlerEditor
{
    private SerializedProperty m_VelocityRate;
    private SerializedProperty m_TimeoutPeriod;
    private SerializedProperty m_LaserExplosionOffset;
    private SerializedProperty m_LaserExplosion;
    private SerializedProperty m_DeferredDespawn;
    private SerializedProperty m_DeferredDespawnTicks;

    public override void OnEnable()
    {
        m_VelocityRate = serializedObject.FindProperty(nameof(LaserMotion.VelocityRate));
        m_TimeoutPeriod = serializedObject.FindProperty(nameof(LaserMotion.TimeoutPeriod));
        m_LaserExplosionOffset = serializedObject.FindProperty(nameof(LaserMotion.LaserExplosionOffset));
        m_LaserExplosion = serializedObject.FindProperty(nameof(LaserMotion.LaserExplosion));
        m_DeferredDespawn = serializedObject.FindProperty(nameof(LaserMotion.DeferredDespawn));
        m_DeferredDespawnTicks = serializedObject.FindProperty(nameof(LaserMotion.DeferredDespawnTicks));
        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        var laserMotion = target as LaserMotion;
        laserMotion.ShipControllerPropertiesVisible = EditorGUILayout.BeginFoldoutHeaderGroup(laserMotion.ShipControllerPropertiesVisible, $"{nameof(LaserMotion)} Properties");
        if (laserMotion.ShipControllerPropertiesVisible)
        {
            EditorGUILayout.PropertyField(m_VelocityRate);
            EditorGUILayout.PropertyField(m_TimeoutPeriod);
            EditorGUILayout.PropertyField(m_LaserExplosionOffset);
            EditorGUILayout.PropertyField(m_LaserExplosion);
            EditorGUILayout.PropertyField(m_DeferredDespawn);
            EditorGUILayout.PropertyField(m_DeferredDespawnTicks);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
        base.OnInspectorGUI();
    }
}
#endif


public class LaserMotion : BaseObjectMotionHandler
{
#if UNITY_EDITOR
    public bool ShipControllerPropertiesVisible = false;
#endif
    public float VelocityRate = 15f;
    [Tooltip("The time that will pass before the instance will destroy itself.")]
    public float TimeoutPeriod = 5.0f;

    [Tooltip("Primarily used for offsetting the explosion from Asteroids.")]
    public float LaserExplosionOffset = 2.0f;

    public GameObject LaserExplosion;
    private FXPrefabPool LaserExplosionPoolSystem;

    private Vector3 m_StartPosition;
    private Vector3 m_InitialVelocity;
    private Quaternion m_StartRotation;
    private float EndOfLife;

    [Tooltip("When enabled, lasers will defer their despawn by the DeferredDespawn number of ticks setting.")]
    public bool DeferredDespawn = true;

    [Tooltip("Number of ticks to wait until the non-authoritative instances are despawned. Upon despawn, the assign LaserImpact will start the explosion PFX.")]
    public int DeferredDespawnTicks = 4;

    /// <summary>
    /// Used for the deferred despawn demo
    /// </summary>
    [HideInInspector]
    public bool IgnoreStartValues;
    [HideInInspector]
    public bool IgnoreYAxisClamp;
    [HideInInspector]
    public bool IgnoreImpactForce;

    private GameObject m_ShipOwner;
    private Vector3 m_ImpactPoint;
    private BaseObjectMotionHandler m_ObjectImpacted;

    private NetworkVariable<NetworkBehaviourReference> m_ImpactedObjectSynch = new NetworkVariable<NetworkBehaviourReference>();
    private NetworkVariable<bool> m_ImpactedObject = new NetworkVariable<bool>();
    private NetworkVariable<Vector3> m_ImpactPosition = new NetworkVariable<Vector3>();

    /// <inheritdoc/>
    protected override bool OnPrepareCollisionMessage(Vector3 averagedCollisionNormal, BaseObjectMotionHandler targetBaseObject)
    {
        //if (!IgnoreImpactForce)
        //{
        //    if (targetBaseObject.CollisionType == CollisionTypes.Asteroid || targetBaseObject.CollisionType == CollisionTypes.Mine || targetBaseObject.CollisionType == CollisionTypes.Ship)
        //    {
        //        var collisionForce = averagedCollisionNormal * CollisionDamage * 0.01f;
        //        CollisionMessage.CollisionForce = collisionForce;
        //        CollisionMessage.SetFlag(true, (uint)CollisionCategoryFlags.CollisionForce);
        //    }
        //    else
        //    {
        //        CollisionMessage.SetFlag(false, (uint)CollisionCategoryFlags.CollisionForce);
        //        CollisionMessage.CollisionForce = Vector3.zero;
        //    }
        //}

        // Disable the colliders to prevent additional collisions
        EnableColliders(false);

        EndOfLife = Time.realtimeSinceStartup;
        m_ObjectImpacted = targetBaseObject;
        return true;
    }

    /// <inheritdoc/>
    protected override void OnContactEvent(ulong eventId, Vector3 averageNormal, Rigidbody collidingBody, Vector3 contactPoint, bool hasCollisionStay = false, Vector3 averagedCollisionStayNormal = default)
    {
        if (m_ObjectImpacted != null || hasCollisionStay || (contactPoint == Vector3.zero && averageNormal == Vector3.zero))
        {
            return;
        }
        var collidingBodyBaseObject = collidingBody.GetComponent<BaseObjectMotionHandler>();
        m_ImpactPoint = contactPoint;
        // If the local client is the authority, then apply immediately
        if (collidingBodyBaseObject.HasAuthority)
        {
            if (OnPrepareCollisionMessage(averageNormal, collidingBodyBaseObject))
            {
                collidingBodyBaseObject.HandleCollision(CollisionMessage, true, true);
            }
        }
        else
        {
            // If the authoritative client is remote, then send a message
            EventCollision(averageNormal, collidingBodyBaseObject);
        }

        // Note: We don't invoke the base OnContactEvent on purpose 
    }

    /// <summary>
    /// This configures the initial position, rotation, and starting velocity of the laser
    /// </summary>
    public void ShootLaser(Vector3 position, Quaternion rotation, Vector3 initialVelocity, GameObject shipOwner = null)
    {
        // Ignore all collisions by the ship owner of the laser
        m_ShipOwner = shipOwner;
        IgnoreCollision(shipOwner, gameObject, true);

        m_StartPosition = position;
        m_StartRotation = rotation;
        m_InitialVelocity = initialVelocity;
        Rigidbody.position = position;
        Rigidbody.rotation = rotation;
    }

    /// <inheritdoc/>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        m_ObjectImpacted = null;
        RigidbodyContactEventManager.Instance.RegisterHandler(this);
        if (CanCommitToTransform)
        {
            EndOfLife = Time.realtimeSinceStartup + TimeoutPeriod;
            if (!IgnoreStartValues)
            {
                // Teleport to the position and facing of the gun
                SetState(m_StartPosition, m_StartRotation, null, false);
            }
            m_ImpactedObject.Value = false;

            // Assure colliders are enabled
            EnableColliders(true);
        }

        LaserExplosionPoolSystem = FXPrefabPool.GetFxPool(LaserExplosion);

        if (DeferredDespawn)
        {
            m_ImpactedObjectSynch.OnValueChanged += OnImpactedObjectChanged;
        }
    }

    /// <summary>
    /// Non-authority instances will set <see cref="m_ExplosionImpact"/> property when updated just prior to the deferred despawn message being
    /// processed.
    /// </summary>
    private void OnImpactedObjectChanged(NetworkBehaviourReference previous, NetworkBehaviourReference current)
    {
        if (!HasAuthority)
        {
            // It is "ok" if we don't get an object as the object may be an asteroid that fragmented into new pieces and then despawned.
            current.TryGet(out m_ObjectImpacted);
        }
    }

    /// <inheritdoc/>
    private void Update()
    {
        if (!HasAuthority || !IsSpawned)
        {
            return;
        }

        // If we haven't hit anything and our end of life has expired, then despawn the laser
        if (CanCommitToTransform && EndOfLife > 0 && EndOfLife < Time.realtimeSinceStartup)
        {
            IgnoreCollision(m_ShipOwner, gameObject, false);

            if (LaserExplosionPoolSystem == null)
            {
                LaserExplosionPoolSystem = FXPrefabPool.GetFxPool(LaserExplosion);
            }

            // Defer the despawn based on the settings.
            if (DeferredDespawn && m_ObjectImpacted)
            {
                NetworkObject.DeferDespawn(DeferredDespawnTicks);
                m_ObjectImpacted = null;
            }
            else // Otherwise just instantly despawn
            {
                NetworkObject.Despawn();
            }
        }
    }

    /// <summary>
    /// Gets a LaserImpact object from the pool and spawns it based on settings
    /// and what was impacted.
    /// </summary>
    private void CreateExplosion()
    {
        if (NetworkManager.ShutdownInProgress)
        {
            return;
        }

        var fxInstance = (GameObject)null;
        var explosion = (ExplosionFx)null;

        try
        {
            // Get an explosion instance
            fxInstance = LaserExplosionPoolSystem.GetInstance();
            explosion = fxInstance.GetComponent<ExplosionFx>();

            if (HasAuthority && m_ObjectImpacted)
            {
                var stageScale = 1.0f;
                // If impacting an asteroid, we might handle the offset positioning a little differently 
                // due to the change in scale (unless fragmenting)
                if (m_ObjectImpacted.CollisionType == CollisionTypes.Asteroid)
                {
                    var asteroid = m_ObjectImpacted as AsteroidObject;
                    m_ImpactedObject.Value = !(asteroid != null && asteroid.IsFragmenting());

                    // Don't attach if the asteroid is splitting into multiple pieces
                    if (!m_ImpactedObject.Value)
                    {
                        explosion.transform.position = m_ImpactPoint;
                        m_ImpactPosition.Value = m_ImpactPoint;
                    }
                    else
                    {
                        // Otherwise scale the explosion position relative to the fragmentation size based on the asteroid's current stage
                        stageScale = asteroid.GetFragmentStage() / asteroid.NumberOfStages;
                    }
                }
                else
                {
                    m_ImpactedObject.Value = true;
                }

                if (m_ImpactedObject.Value)
                {
                    explosion.transform.position = m_ImpactPoint + (-transform.forward * LaserExplosionOffset * stageScale);
                    explosion.transform.SetParent(m_ObjectImpacted.transform);
                    m_ImpactPosition.Value = explosion.transform.localPosition;
                }
            }
            else
            {
                // If we impacted an object and the object is still around, then parent and apply the local space offset
                if (m_ImpactedObject.Value && m_ImpactedObjectSynch.Value.TryGet(out m_ObjectImpacted))
                {
                    if (m_ObjectImpacted)
                    {
                        explosion.transform.SetParent(m_ObjectImpacted.transform);
                        explosion.transform.localPosition = m_ImpactPosition.Value;
                    }
                }
                else
                {
                    // Otherwise, we didn't impact an object (i.e. Asteroid fragmented) so just apply world space
                    explosion.transform.position = m_ImpactPosition.Value;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.Log(ex);
        }
    }

    /// <summary>
    ///  When deferred despawing, set the reference to the locally spawned explosion's LaserImpact NetworkBehaviour.
    ///  Non-authority instances will use this to start the explosion FX upon despawing on the specified deferred
    ///  network tick.
    /// </summary>
    /// <remarks>
    /// The <see cref="OnImpactedObjectChanged(NetworkBehaviourReference, NetworkBehaviourReference)"/> method sets 
    /// the <see cref="m_ExplosionImpact"/> property when updated just prior to the deferred despawn message being
    /// processed.
    /// </remarks>
    /// <param name="despawnTick">the future network tick this instance will be despawned on non-authority instances</param>
    public override void OnDeferringDespawn(int despawnTick)
    {
        // NOTE: This is authority relative. 
        CreateExplosion();
        if (m_ImpactedObject.Value)
        {
            m_ImpactedObjectSynch.Value = new NetworkBehaviourReference(m_ObjectImpacted);
        }
        base.OnDeferringDespawn(despawnTick);
    }

    /// <summary>
    /// Review the <see cref="OnDeferringDespawn"/> overridden method above.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        if (DeferredDespawn)
        {
            m_ImpactedObjectSynch.OnValueChanged -= OnImpactedObjectChanged;
        }

        RigidbodyContactEventManager.Instance.RegisterHandler(this, false);
        // When despawned on non-authority instances and we are deferring despawns and
        // if the explosion impact (LaserImpact) is assigned, then start the explosion
        // FX. 
        if (!HasAuthority && DeferredDespawn && m_ImpactedObject.Value)
        {
            CreateExplosion();
        }
        EnableColliders(false);

        base.OnNetworkDespawn();
    }

    /// <inheritdoc/>
    private void FixedUpdate()
    {
        if (IsSpawned && CanCommitToTransform)
        {
            // Keep the laser at the same height as the starting point
            var target = transform.position + (transform.forward * (VelocityRate + m_InitialVelocity.magnitude));
            if (!IgnoreYAxisClamp)
            {
                target.y = m_StartPosition.y;
            }
            NetworkRigidbody.MovePosition(Vector3.Lerp(transform.position, target, Time.fixedDeltaTime));
        }
    }
}
