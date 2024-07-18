using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Runtime.CompilerServices;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// The custom editor for the <see cref="ShipController"/> component.
/// </summary>
[CustomEditor(typeof(ShipController), true)]
public class ShipControllerEditor : PhysicsObjectMotionEditor
{
    private SerializedProperty m_VelocityRate;
    private SerializedProperty m_RotationRate;
    private SerializedProperty m_RotationDecay;
    private SerializedProperty m_ShipThrusters;
    private SerializedProperty m_TractorBeam;
    private SerializedProperty m_TractorBeamDistance;
    private SerializedProperty m_LaserProjectile;
    private SerializedProperty m_LaserGunBarrelLeft;
    private SerializedProperty m_LaserGunBarrelRight;
    private SerializedProperty m_TargetingSystem;
    private SerializedProperty m_MineCarryPoint;
    private SerializedProperty m_ForwardThrusterLight;
    private SerializedProperty m_LeftThrusterLight;
    private SerializedProperty m_RightThrusterLight;
    private SerializedProperty m_ForwardThrusterLightIntensity;
    private SerializedProperty m_RightLeftThrusterLightIntensity;
    private SerializedProperty m_MaximumCollisionVelocity;
    private SerializedProperty m_PlayerIdentifier;
    private SerializedProperty m_InterestShipMarker;

    public override void OnEnable()
    {
        m_VelocityRate = serializedObject.FindProperty(nameof(ShipController.VelocityRate));
        m_RotationRate = serializedObject.FindProperty(nameof(ShipController.RotationRate));
        m_RotationDecay = serializedObject.FindProperty(nameof(ShipController.RotationDecay));
        m_ShipThrusters = serializedObject.FindProperty(nameof(ShipController.ShipThrusters));
        m_TractorBeam = serializedObject.FindProperty(nameof(ShipController.TractorBeam));
        m_TractorBeamDistance = serializedObject.FindProperty(nameof(ShipController.TractorBeamDistance));
        m_LaserProjectile = serializedObject.FindProperty(nameof(ShipController.LaserProjectile));
        m_LaserGunBarrelLeft = serializedObject.FindProperty(nameof(ShipController.LaserGunBarrelLeft));
        m_LaserGunBarrelRight = serializedObject.FindProperty(nameof(ShipController.LaserGunBarrelRight));
        m_TargetingSystem = serializedObject.FindProperty(nameof(ShipController.TargetingSystem));
        m_MineCarryPoint = serializedObject.FindProperty(nameof(ShipController.MineCarryPoint));
        m_ForwardThrusterLight = serializedObject.FindProperty(nameof(ShipController.ForwardThrusterLight));
        m_LeftThrusterLight = serializedObject.FindProperty(nameof(ShipController.LeftThrusterLight));
        m_RightThrusterLight = serializedObject.FindProperty(nameof(ShipController.RightThrusterLight));
        m_ForwardThrusterLightIntensity = serializedObject.FindProperty(nameof(ShipController.ForwardThrusterLightIntensity));
        m_RightLeftThrusterLightIntensity = serializedObject.FindProperty(nameof(ShipController.RightLeftThrusterLightIntensity));
        m_MaximumCollisionVelocity = serializedObject.FindProperty(nameof(ShipController.MaximumCollisionVelocity));
        m_PlayerIdentifier = serializedObject.FindProperty(nameof(ShipController.PlayerIdentifier));
        m_InterestShipMarker = serializedObject.FindProperty(nameof(ShipController.InterestShipMarker));

        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        var shipController = target as ShipController;
        shipController.ShipControllerPropertiesVisible = EditorGUILayout.BeginFoldoutHeaderGroup(shipController.ShipControllerPropertiesVisible, $"{nameof(ShipController)} Properties");
        if (shipController.ShipControllerPropertiesVisible)
        {
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.PropertyField(m_VelocityRate);
            EditorGUILayout.PropertyField(m_RotationRate);
            EditorGUILayout.PropertyField(m_RotationDecay);
            EditorGUILayout.PropertyField(m_ShipThrusters);
            EditorGUILayout.PropertyField(m_TractorBeam);
            EditorGUILayout.PropertyField(m_TractorBeamDistance);
            EditorGUILayout.PropertyField(m_LaserProjectile);
            EditorGUILayout.PropertyField(m_LaserGunBarrelLeft);
            EditorGUILayout.PropertyField(m_LaserGunBarrelRight);
            EditorGUILayout.PropertyField(m_TargetingSystem);
            EditorGUILayout.PropertyField(m_MineCarryPoint);
            EditorGUILayout.PropertyField(m_ForwardThrusterLight);
            EditorGUILayout.PropertyField(m_LeftThrusterLight);
            EditorGUILayout.PropertyField(m_RightThrusterLight);
            EditorGUILayout.PropertyField(m_ForwardThrusterLightIntensity);
            EditorGUILayout.PropertyField(m_RightLeftThrusterLightIntensity);
            EditorGUILayout.PropertyField(m_MaximumCollisionVelocity);
            EditorGUILayout.PropertyField(m_PlayerIdentifier);
            EditorGUILayout.PropertyField(m_InterestShipMarker);
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

public class ShipController : PhysicsObjectMotion
{

#if UNITY_EDITOR
    public bool ShipControllerPropertiesVisible = false;
#endif
    /// <summary>
    /// Set during initialization. It contains the runtime generated spawn point locations.
    /// </summary>
    public static PlayerSpawnPoints PlayerSpawnPoints;

    [Tooltip("This value determines the acceleration of the ship's thrusters linear velocity.")]
    public float VelocityRate = 3f;

    [Tooltip("This value determines the acceleration of the ship's thrusters angular velocity.")]
    public float RotationRate = 5.0f;

    [Tooltip("This value determines the decay rate of the RotationRate as it accumulates over time.")]
    public float RotationDecay = 0.001f;

    [Tooltip("This configures all of the ship's thrusters for this prefab instance.")]
    public List<ShipThruster> ShipThrusters;

    [Tooltip("The tractor beam particle FX to play.")]
    public ParticleSystem TractorBeam;

    [Tooltip("This determines how far out the tractor beam can start pulling objects towards the ship.")]
    public float TractorBeamDistance = 96.0f;

    [Tooltip("This should be the same network prefab as the one used for the laster prefab pool. It is *only* used to find the pool during runtime.")]
    public GameObject LaserProjectile;

    [Tooltip("The left point on the ship that a laser will be positioned when fired.")]
    public GameObject LaserGunBarrelLeft;

    [Tooltip("The right point on the ship that a laser will be positioned when fired.")]
    public GameObject LaserGunBarrelRight;

    [Tooltip("The ship's assigned targeting system (i.e. what is used to point the lasers towards when firing)")]
    public TargetingSystem TargetingSystem;

    [Tooltip("The final resting point of a mine when pulled in by the tractor beam.")]
    public GameObject MineCarryPoint;

    [Tooltip("The light assigned to the main forward thruster that adjusts intensity based on amount of thrust.")]
    public Light ForwardThrusterLight;

    [Tooltip("The light assigned to the left forward and reverse thrusters that adjusts intensity based on amount of thrust (relative to the thruster).")]
    public Light LeftThrusterLight;

    [Tooltip("The light assigned to the right forward and reverse thrusters that adjusts intensity based on amount of thrust (relative to the thruster).")]
    public Light RightThrusterLight;

    [Tooltip("The maximum forward thruster light intensity.")]
    public float ForwardThrusterLightIntensity = 20.0f;

    [Tooltip("The maximum right and left thrusters' light intensity.")]
    public float RightLeftThrusterLightIntensity = 10.0f;

    [Range(1.0f, 50.0f)]
    public float MaximumCollisionVelocity = 20.0f;

    [Tooltip("Preset player identifier texture mesh.")]
    public TextMesh PlayerIdentifier;

    [Tooltip("Preset player's interest marker texture mesh.")]
    public TextMesh InterestShipMarker;

    // This is toggled when firing lasers. When true it uses the left barrel and when false the right.
    private bool UseLeftLaserBarrel;

    // The laser object pool
    private ObjectPoolSystem LaserObjectPoolSystem;

    // Assigned to the mine that a ship has pulled in via the tractor beam.
    private MinebotMotion m_Minebot;

    // When true, the player no longer has to keep the tractor beam active (the FX will also change color as a visual cue)
    private bool m_MinebotLocked;

    // When true, the tactor beam has yet to lock-on fully to the mine. If the player lets up on the tractor beam, the mine
    // will drift away.
    private bool m_MinebotLocking;

    private MeshRenderer m_MeshRenderer;

    #region Ship Tractor Beam
    private NetworkVariable<bool> m_TractorBeam = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private ParticleSystem.MinMaxGradient m_TractorStartColor;

    /// <summary>
    /// Shows/Hides player tags
    /// </summary>
    /// <param name="isVisible"></param>
    public void PlayerTagVisibility(bool isVisible)
    {
        if (IsOwner)
        {
            PlayerIdentifier.gameObject.SetActive(false);
        }
        else
        {
            PlayerIdentifier.gameObject.SetActive(isVisible);
        }
    }

    /// <summary>
    /// Determines if the tractor beam FX should be playing or not
    /// </summary>
    private void UpdateTractorBeam()
    {
        if (TractorBeam != null)
        {
            if (m_TractorBeam.Value)
            {
                TractorBeam.Play();
            }
            else
            {
                TractorBeam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    /// <summary>
    /// Using a NetworkVariable to keep other player's visually in-synch with the 
    /// local player's ship's tractorbeam state.
    /// </summary>
    private void OnTractorBeamChanged(bool previous, bool next)
    {
        UpdateTractorBeam();
    }

    /// <summary>
    /// Handles the tractor beam logic
    /// </summary>
    private void TractorBeamUpdate()
    {
        if (TractorBeam != null)
        {
            if (!m_TractorBeam.Value)
            {
                if (Input.GetKeyDown(KeyCode.G))
                {
                    if (m_Minebot != null)
                    {
                        // Detatch the mine (if attached)
                        m_Minebot.NetworkRigidbody.DetachFromFixedJoint();
                        IgnoreCollision(m_Minebot.gameObject, gameObject, false);
                        m_Minebot.ReleaseMine(transform.forward, GetObjectVelocity());
                        m_Minebot = null;
                        m_MinebotLocked = false;
                        m_MinebotLocking = false;
                    }
                    else
                    {
                        var main = TractorBeam.main;
                        main.startColor = m_TractorStartColor;
                        TractorBeam.Play();
                        m_TractorBeam.Value = true;
                    }
                }
            }
            else
            {
                if (Input.GetKeyUp(KeyCode.G) && !m_MinebotLocking)
                {
                    m_TractorBeam.Value = false;
                    if (m_Minebot != null)
                    {
                        // Detatch the mine (if attached)
                        m_Minebot.NetworkRigidbody.DetachFromFixedJoint();
                        IgnoreCollision(m_Minebot.gameObject, gameObject, false);
                        m_Minebot.ReleaseMine(transform.forward, GetObjectVelocity());
                        m_Minebot = null;
                        m_MinebotLocked = false;
                    }
                    if (!m_MinebotLocking)
                    {
                        TractorBeam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }
                else
                {
                    if (m_Minebot == null)
                    {
                        var targetedGameObject = TargetingSystem.ClosestTarget;
                        if (targetedGameObject != null)
                        {
                            var distance = Vector3.Distance(targetedGameObject.transform.position, transform.position);
                            if (distance <= TractorBeamDistance)
                            {
                                var targetMinebot = targetedGameObject.GetComponent<MinebotMotion>();
                                if (targetMinebot != null && (targetMinebot.IsOwner || !targetMinebot.NetworkObject.IsOwnershipLocked))
                                {
                                    if (!targetMinebot.IsOwner)
                                    {
                                        targetMinebot.NetworkObject.ChangeOwnership(NetworkManager.LocalClientId);
                                    }
                                    targetMinebot.Rigidbody.linearVelocity = Vector3.zero;
                                    m_Minebot = targetMinebot;

                                    // Lock ownership so other ships can't take the mine
                                    m_Minebot.NetworkObject.SetOwnershipLock();
                                }
                            }
                        }
                    }
                    else if (!m_MinebotLocking)
                    {
                        m_Minebot.Rigidbody.linearVelocity = Rigidbody.linearVelocity;
                        m_Minebot.Rigidbody.angularVelocity = Rigidbody.angularVelocity;
                        var deltaDistance = Vector3.Distance(m_Minebot.Rigidbody.position, MineCarryPoint.transform.position);
                        if (deltaDistance > 2.5f)
                        {
                            m_Minebot.NetworkRigidbody.MovePosition(Vector3.Lerp(m_Minebot.Rigidbody.position, MineCarryPoint.transform.position, 0.15f));
                        }
                        else
                        {
                            ParticleSystem.MainModule main = TractorBeam.main;
                            var adjusted = m_TractorStartColor;
                            var color = m_TractorStartColor.color;
                            color.g = 0.85f;
                            color.b *= 0.50f;
                            color.r *= 0.50f;
                            adjusted.color = color;
                            main.startColor = adjusted;
                            var particles = new ParticleSystem.Particle[TractorBeam.particleCount];
                            TractorBeam.GetParticles(particles);
                            for (int i = 0; i < TractorBeam.particleCount; i++)
                            {
                                particles[i].startColor = color;
                            }
                            TractorBeam.SetParticles(particles);
                            m_MinebotLocking = true;

                            IgnoreCollision(m_Minebot.gameObject, gameObject, true);
                        }
                    }
                    else if (m_MinebotLocking && !m_MinebotLocked)
                    {
                        m_Minebot.Rigidbody.linearVelocity = Rigidbody.linearVelocity;
                        m_Minebot.Rigidbody.angularVelocity = Rigidbody.angularVelocity;
                        var deltaDistance = Vector3.Distance(m_Minebot.Rigidbody.position, MineCarryPoint.transform.position);
                        m_Minebot.NetworkRigidbody.MovePosition(Vector3.Lerp(m_Minebot.Rigidbody.position, MineCarryPoint.transform.position, 0.10f));
                        if (m_TractorBeam.Value)
                        {
                            if (deltaDistance < 1.0f)
                            {
                                TractorBeam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                            }
                            if (deltaDistance < 0.75f)
                            {
                                // Attach the mine to the player's ship.
                                // Make the mine's mass relative to the player's ship so small it doesn't impact thrust applied to the ship (i.e. doesn't add to over-all mass)
                                if (!m_Minebot.NetworkRigidbody.AttachToFixedJoint(NetworkRigidbody, MineCarryPoint.transform.position, massScale: 0.00001f))
                                {
                                    Debug.LogError($"Could not connect {m_Minebot.name} fixed joint!");
                                    m_MinebotLocked = false;
                                    m_MinebotLocking = false;
                                    m_TractorBeam.Value = false;
                                }
                                else
                                {
                                    m_MinebotLocked = true;
                                    m_MinebotLocking = false;
                                    m_TractorBeam.Value = false;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    #endregion

    #region Ship Thrusters Logic and Synchronization

    private float m_ForwardThrust;
    private float m_RotationThrust;

    /// <summary>
    /// Used to visually synchronize the ship's thruster states
    /// </summary>
    private struct ThrusterState : INetworkSerializable
    {
        public const byte Forward = 0x01;
        public const byte Left = 0x02;
        public const byte Right = 0x04;
        public const byte Reverse = 0x08;

        public byte Flags;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFlag(bool set, byte flag)
        {
            var flags = (uint)Flags;
            if (set) { flags = flags | flag; }
            else { flags = flags & ~(uint)flag; }

            Flags = (byte)flags;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetFlag(byte flag)
        {
            var flags = (uint)Flags;
            return (flags & flag) != 0;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Flags);
        }
    }

    private Dictionary<ThrusterPositions, ShipThruster> ThrusterTable;
    private NetworkVariable<ThrusterState> m_ThrusterFxScale = new NetworkVariable<ThrusterState>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private ThrusterState m_ThrusterState = new ThrusterState();

    private void UpdateShipThrusterFx()
    {
        if (ThrusterTable == null)
        {
            return;
        }
        var mainThruster = ThrusterTable[ThrusterPositions.Main];
        if (m_ThrusterState.GetFlag(ThrusterState.Forward))
        {
            if (!mainThruster.IsActive)
            {
                mainThruster.IsActive = true;
                mainThruster.ThrusterFX.Play();
                ForwardThrusterLight.enabled = true;
            }
            mainThruster.ThrusterFX.transform.localScale = Vector3.Lerp(mainThruster.ThrusterFX.transform.localScale, Vector3.one * 1.5f, Time.deltaTime);
            ForwardThrusterLight.intensity = mainThruster.ThrusterFX.transform.localScale.magnitude * ForwardThrusterLightIntensity;
        }
        else if (mainThruster.IsActive)
        {
            mainThruster.ThrusterFX.transform.localScale = Vector3.Lerp(mainThruster.ThrusterFX.transform.localScale, Vector3.zero, 0.025f);
            ForwardThrusterLight.intensity = mainThruster.ThrusterFX.transform.localScale.magnitude * ForwardThrusterLightIntensity;
            if (mainThruster.ThrusterFX.transform.localScale.magnitude < 0.1f)
            {
                mainThruster.IsActive = false;
                mainThruster.ThrusterFX.Stop();
                mainThruster.ThrusterFX.transform.localScale = Vector3.zero;
                ForwardThrusterLight.intensity = 0f;
                ForwardThrusterLight.enabled = false;
            }
        }

        var isReverse = m_ThrusterState.GetFlag(ThrusterState.Reverse);

        if (m_ThrusterState.GetFlag(ThrusterState.Right) || m_ThrusterState.GetFlag(ThrusterState.Left))
        {
            var thrusterPosition = m_ThrusterState.GetFlag(ThrusterState.Left) ? isReverse ? ThrusterPositions.LeftReverse : ThrusterPositions.RightForward : isReverse ? ThrusterPositions.RightReverse : ThrusterPositions.LeftForward;

            UpdateRotationThruster(thrusterPosition);
        }
        else if (isReverse)
        {
            UpdateRotationThruster(ThrusterPositions.RightReverse);
            UpdateRotationThruster(ThrusterPositions.LeftReverse);
        }

        var isFullReverse = isReverse && !m_ThrusterState.GetFlag(ThrusterState.Right) && !m_ThrusterState.GetFlag(ThrusterState.Left);

        UpdateRotationThrusterDecay(ThrusterPositions.RightForward, ThrusterState.Left, false, isReverse);
        UpdateRotationThrusterDecay(ThrusterPositions.LeftForward, ThrusterState.Right, false, isReverse);
        UpdateRotationThrusterDecay(ThrusterPositions.RightReverse, ThrusterState.Right, isFullReverse, isReverse);
        UpdateRotationThrusterDecay(ThrusterPositions.LeftReverse, ThrusterState.Left, isFullReverse, isReverse);
    }

    private Light GetThrusterLight(ThrusterPositions thrusterPosition)
    {
        var offset = transform.forward * 4;
        offset.y = 0.0f;
        if (thrusterPosition == ThrusterPositions.RightForward || thrusterPosition == ThrusterPositions.RightReverse)
        {
            offset *= -0.85f;
        }

        if (thrusterPosition == ThrusterPositions.LeftForward || thrusterPosition == ThrusterPositions.LeftReverse)
        {
            LeftThrusterLight.transform.localPosition = offset;
            return LeftThrusterLight;
        }
        RightThrusterLight.transform.localPosition = offset;
        return RightThrusterLight;
    }

    private void UpdateRotationThruster(ThrusterPositions thrusterPosition)
    {
        var thruster = ThrusterTable[thrusterPosition];
        var thrusterLight = GetThrusterLight(thrusterPosition);
        if (!thruster.IsActive)
        {
            thruster.IsActive = true;
            thruster.FXObject.SetActive(true);
            thruster.ThrusterFX.Play();
            thruster.FXObject.transform.localScale = Vector3.one * 0.1f;
            thrusterLight.enabled = true;
            thrusterLight.transform.SetParent(thruster.FXObject.transform.parent, false);
        }
        thruster.FXObject.transform.localScale = Vector3.Lerp(thruster.FXObject.transform.localScale, Vector3.one, Time.deltaTime);
        thrusterLight.intensity = thruster.FXObject.transform.localScale.magnitude * RightLeftThrusterLightIntensity;
    }

    private void UpdateRotationThrusterDecay(ThrusterPositions thrusterPosition, byte thrusterState, bool isFullReverse, bool isReverse)
    {
        var thruster = ThrusterTable[thrusterPosition];

        var isForwardThruster = thrusterPosition == ThrusterPositions.RightForward || thrusterPosition == ThrusterPositions.LeftForward;

        if (thruster.IsActive && ((!m_ThrusterState.GetFlag(thrusterState) && !isFullReverse) || (isForwardThruster && isReverse) || (!isForwardThruster && !isReverse)))
        {
            var thrusterLight = GetThrusterLight(thrusterPosition);
            thruster.FXObject.transform.localScale = Vector3.Lerp(thruster.FXObject.transform.localScale, Vector3.zero, 0.025f);
            thrusterLight.intensity = thruster.FXObject.transform.localScale.magnitude * RightLeftThrusterLightIntensity;
            if (thruster.FXObject.transform.localScale.magnitude < 0.1f)
            {
                thruster.IsActive = false;
                thruster.ThrusterFX.Stop();
                thruster.FXObject.transform.localScale = Vector3.zero;
                thrusterLight.intensity = 0.0f;
                thrusterLight.enabled = false;
            }
        }
    }

    private void OnThrusterValuesChanged(ThrusterState previous, ThrusterState current)
    {
        m_ThrusterState = current;
    }
    #endregion

    #region Initialization and Spawning
    /// <summary>
    /// Since this is pooled, we keep the base name assigned and
    /// reset it if the ship ends up being re-used at a later time
    /// </summary>
    private string m_OriginalName;

    protected override void Start()
    {
        base.Start();

        ThrusterTable = new Dictionary<ThrusterPositions, ShipThruster>();
        foreach (var thruster in ShipThrusters)
        {
            if (ThrusterTable.ContainsKey(thruster.Position))
            {
                Debug.LogError($"Duplicate {thruster.Position} thruster entry detected! Skipping!");
                continue;
            }

            if (thruster.FXObject == null)
            {
                Debug.LogError($"Thruster {thruster.Position} has no FXObject assigned! Skipping!");
                continue;
            }

            thruster.ThrusterFX = thruster.FXObject.GetComponent<ParticleSystem>();
            if (thruster.ThrusterFX == null)
            {
                Debug.LogError($"Thruster {thruster.Position} FXObject has no ParticleSystem component! Skipping!");
                continue;
            }

            thruster.ThrusterFX.Stop();
            thruster.FXObject.transform.localScale = Vector3.zero;
            ThrusterTable.Add(thruster.Position, thruster);
        }
        Rigidbody.isKinematic = true;
    }

    public override void OnNetworkSpawn()
    {
        // Since this is derived from NetworkTransform, we always
        // want to make sure the base.OnNetworkSpawn is invoked prior
        // to doing any child specific spawn initialization.
        base.OnNetworkSpawn();

        // Assure the name of the ship remains the same, but the spawned
        // name is specific to the authority instance. This is primarily
        // since we are using pools and players can connect and disconnect
        // over time (i.e. re-using previously connected players' ships)
        m_OriginalName = name;
        gameObject.name = $"[Client-{OwnerClientId}]{name}";

        // If true, then setup local player/authority specific settings
        if (CanCommitToTransform)
        {
            if (TractorBeam != null)
            {
                TractorBeam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                m_TractorStartColor = TractorBeam.main.startColor;
                m_TractorBeam.Value = false;
            }

            PlayerTagVisibility(false);

            // Parent the main camera under the local player's ship
            Camera.main.transform.parent = Rigidbody.transform;

            // Since we need to wait for everything else to be instantiated and spawned,
            // we disable our ship visual, wait for 1 network tick, and then teleport
            // to a safe spawn point where nothing exists.
            m_MeshRenderer = GetComponent<MeshRenderer>();
            m_MeshRenderer.enabled = false;
            StartCoroutine(WaitToTeleport());
        }
        else
        {
            // Non-players disable the remote player's ship thruster and tractor beam FX
            m_ThrusterFxScale.OnValueChanged += OnThrusterValuesChanged;
            m_TractorBeam.OnValueChanged += OnTractorBeamChanged;
            UpdateTractorBeam();
        }

        // Check for the laster object pool, if found assign it locally
        if (ObjectPoolSystem.ExistingPoolSystems.ContainsKey(LaserProjectile))
        {
            LaserObjectPoolSystem = ObjectPoolSystem.ExistingPoolSystems[LaserProjectile];
        }
    }

    protected override void OnNetworkPostSpawn()
    {
        PlayerIdentifier.text = $"Player-{OwnerClientId}";

        InterestShipMarker.color = PlayerColor.GetPlayerColor(OwnerClientId);

        base.OnNetworkPostSpawn();
    }

    private bool m_SpawnAtLocation;
    private Vector3 m_SpawnLocation;

    private IEnumerator WaitToTeleport()
    {
        yield return new WaitForSeconds(0.01f);

        var position = PlayerSpawnPoints != null ? PlayerSpawnPoints.GetSpawnPoint(gameObject) : transform.position;
        position.y = 0.5f;
        m_SpawnLocation = position;
        m_SpawnAtLocation = true;
    }

    public override void OnNetworkDespawn()
    {
        gameObject.name = m_OriginalName;

        if (HasAuthority)
        {
            Camera.main.transform.SetParent(null, false);
            if (m_Minebot != null)
            {
                m_Minebot.NetworkObject.TryRemoveParent();
            }
        }
        else
        {
            RemovePlayerObserver();
            m_ThrusterFxScale.OnValueChanged -= OnThrusterValuesChanged;
        }

        m_Minebot = null;
        m_MinebotLocked = false;
        m_MinebotLocking = false;

        if (TractorBeam != null)
        {
            TractorBeam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            m_TractorStartColor = TractorBeam.main.startColor;
        }

        base.OnNetworkDespawn();
    }
    #endregion

    #region Player Input & Update, Shooting Lasers, and Ship Physics

    internal ShipController PlayerObserver;
    private ShipController m_PlayerBeingObserved;

    private void RemovePlayerObserver()
    {
        if (PlayerObserver != null)
        {
            PlayerObserver.SwitchCameraToTarget(false);
        }
    }

    public void SwitchCameraToTarget(bool switchTo)
    {

        if (switchTo)
        {
            if (m_PlayerBeingObserved != null)
            {
                return;
            }

            var shipController = TargetingSystem.ClosestTarget.GetComponent<ShipController>();
            if (shipController == null)
            {
                return;
            }
            m_PlayerBeingObserved = shipController;
            m_PlayerBeingObserved.PlayerObserver = this;
            Camera.main.transform.SetParent(m_PlayerBeingObserved.transform, false);
        }
        else
        {
            if (m_PlayerBeingObserved != null)
            {
                Camera.main.transform.SetParent(Rigidbody.transform, false);
                m_PlayerBeingObserved.PlayerObserver = null;
                m_PlayerBeingObserved = null;
            }
        }
    }

    /// <summary>
    /// This fires the ship's lasers
    /// </summary>
    private void FireLaser()
    {
        // Check for the pool if it wasn't found during spawn
        if (LaserObjectPoolSystem == null)
        {
            if (ObjectPoolSystem.ExistingPoolSystems.ContainsKey(LaserProjectile))
            {
                LaserObjectPoolSystem = ObjectPoolSystem.ExistingPoolSystems[LaserProjectile];
            }
        }

        if (LaserObjectPoolSystem != null)
        {
            // As long as the targeting system is referenced, then use the target marker as the target
            var targetedGameObject = TargetingSystem.ClosestTarget != null ? TargetingSystem.TargetMarker : null;

            // Acquire a non-spawned pooled laser instance
            var instance = LaserObjectPoolSystem.GetInstance((IsServer && !NetworkManager.DistributedAuthorityMode) || (NetworkManager.DistributedAuthorityMode && NetworkManager.LocalClientId == OwnerClientId));

            // Get the LaserMotion class and configure which barrel the laser is being shot from
            var laserMotion = instance.GetComponent<LaserMotion>();
            var laserBarrel = LaserGunBarrelLeft;
            if (!UseLeftLaserBarrel)
            {
                laserBarrel = LaserGunBarrelRight;
            }
            UseLeftLaserBarrel = !UseLeftLaserBarrel;

            var rotation = laserBarrel.transform.rotation;
            if (targetedGameObject != null)
            {
                var forward = (targetedGameObject.transform.position - laserBarrel.transform.position).normalized;

                laserMotion.transform.forward = forward;
                rotation = laserMotion.transform.rotation;
            }

            // When we shoot the laser, we just set the start position, rotation, and velocity that are applied when spawned locally
            laserMotion.ShootLaser(laserBarrel.transform.position, rotation, GetObjectVelocity(), gameObject);

            // Spawn the laser 
            instance.Spawn();
        }
    }
    private float m_RotationRate;
    /// <summary>
    /// The local player's (authority) update, handles player inputs
    /// specific to the ship.
    /// </summary>
    private void AuthorityUpdate()
    {
        if (m_SpawnAtLocation)
        {
            SetState(m_SpawnLocation, null, null, false);
            m_MeshRenderer.enabled = true;
            m_SpawnAtLocation = false;
            Rigidbody.isKinematic = false;
        }

        #region User Motion Input Handling
        bool up = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
        bool left = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
        bool down = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
        bool right = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
        bool updateThrusterValues = false;

        if (left || right)
        {
            var rotationThrust = left ? -1 : 1;
            m_RotationRate = Mathf.Lerp(m_RotationRate, rotationThrust, Time.deltaTime);
            m_RotationThrust = Mathf.Lerp(m_RotationThrust, m_RotationRate, Time.deltaTime);
            updateThrusterValues = true;
            if (left)
            {
                m_ThrusterState.SetFlag(true, ThrusterState.Left);
                m_ThrusterState.SetFlag(false, ThrusterState.Right);
            }
            else
            {
                m_ThrusterState.SetFlag(false, ThrusterState.Left);
                m_ThrusterState.SetFlag(true, ThrusterState.Right);
            }
        }
        else if (Mathf.Abs(m_RotationThrust) > 0.0f)
        {
            m_RotationRate = Mathf.Lerp(m_RotationRate, 0.0f, Time.deltaTime * RotationDecay);
            m_RotationThrust = Mathf.Lerp(m_RotationThrust, 0.0f, Time.deltaTime * RotationDecay);
            if (m_ThrusterState.GetFlag(ThrusterState.Left) || m_ThrusterState.GetFlag(ThrusterState.Right))
            {
                updateThrusterValues = true;
                m_ThrusterState.SetFlag(false, ThrusterState.Right);
                m_ThrusterState.SetFlag(false, ThrusterState.Left);
            }

        }

        if (up || down)
        {
            var forwardThrust = down ? -1 : 1;
            m_ForwardThrust = Mathf.Lerp(m_ForwardThrust, forwardThrust, Time.deltaTime);
            updateThrusterValues = true;
            if (up)
            {
                m_ThrusterState.SetFlag(true, ThrusterState.Forward);
                m_ThrusterState.SetFlag(false, ThrusterState.Reverse);
            }
            else
            {
                m_ThrusterState.SetFlag(false, ThrusterState.Forward);
                m_ThrusterState.SetFlag(true, ThrusterState.Reverse);

            }
        }
        else if (Mathf.Abs(m_ForwardThrust) > 0.0f)
        {
            m_ForwardThrust = Mathf.Lerp(m_ForwardThrust, 0.0f, Time.deltaTime);
            if (m_ThrusterState.GetFlag(ThrusterState.Forward) || m_ThrusterState.GetFlag(ThrusterState.Reverse))
            {
                updateThrusterValues = true;
                m_ThrusterState.SetFlag(false, ThrusterState.Forward);
                m_ThrusterState.SetFlag(false, ThrusterState.Reverse);
            }
        }

        if (updateThrusterValues)
        {
            m_ThrusterFxScale.Value = m_ThrusterState;
        }
        #endregion

        #region User Weapon System Input Handling

        bool fireLaser = Input.GetKeyDown(KeyCode.Space);
        if (fireLaser)
        {
            FireLaser();
        }
        TractorBeamUpdate();
        #endregion


        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (NetworkManager != null)
            {
                SwitchCameraToTarget(true);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (NetworkManager != null)
            {
                SwitchCameraToTarget(false);
            }
        }

    }

    /// <summary>
    /// Primary update
    /// </summary>
    private void Update()
    {
        // Always just exit if not spawned
        if (!IsSpawned)
        {
            return;
        }

        // If this instance has authoritym, then invoke
        // the local player authority update
        if (HasAuthority)
        {
            AuthorityUpdate();
        }

        // Update thusters FX
        UpdateShipThrusterFx();


        if (PlayerIdentifier != null)
        {
            PlayerIdentifier.transform.LookAt(Camera.main.transform, transform.up);
            PlayerIdentifier.transform.forward = -PlayerIdentifier.transform.forward;
        }
    }

    /// <summary>
    /// This applies the local player's thruster values to the ship in both
    /// linear and angular velocity values
    /// </summary>
    protected override void FixedUpdate()
    {
        if (!IsSpawned || !HasAuthority || Rigidbody != null && Rigidbody.isKinematic)
        {
            base.FixedUpdate();
            return;
        }

        if (Mathf.Abs(m_RotationThrust) > 0.001f)
        {
            var angularVelocity = Rigidbody.angularVelocity;
            angularVelocity.y += Mathf.Min(m_RotationThrust * RotationRate * Time.fixedDeltaTime, MaxAngularVelocity);
            Rigidbody.angularVelocity = angularVelocity;
        }

        if (Mathf.Abs(m_ForwardThrust) > 0.001f)
        {
            var forward = transform.forward;
            forward.y = 0.0f;
            forward *= m_ForwardThrust * Time.fixedDeltaTime * VelocityRate;
            SetObjectVelocity(GetObjectVelocity() + forward);
        }
        base.FixedUpdate();
    }

    public float GetPrimaryThrusterScale()
    {
        return ThrusterTable[ThrusterPositions.Main].ThrusterFX.transform.localScale.magnitude;
    }

    /// <summary>
    /// Handle applying the collision force to ourselves and the object we collided with
    /// </summary>
    protected override bool OnPrepareCollisionMessage(Vector3 averagedCollisionNormal, BaseObjectMotionHandler targetBaseObject)
    {
        if (targetBaseObject.HasAuthority)
        {
            // Ignore trigger invoked sequences for own own projectiles
            if (targetBaseObject.CollisionType == CollisionTypes.Laser || targetBaseObject.CollisionType == CollisionTypes.Missile)
            {
                return false;
            }

            // Don't collide with a minebot if it is parented
            if (m_Minebot != null && m_Minebot.HasAuthority && targetBaseObject.NetworkObject == m_Minebot.NetworkObject)
            {
                return false;
            }
        }
        return base.OnPrepareCollisionMessage(averagedCollisionNormal, targetBaseObject);
    }

    /// <summary>
    /// For debugging purposes
    /// </summary>
    protected override string OnLogCollision(ref BaseObjectMotionHandler objectHit)
    {
        return $"[Ship Collide]-{base.OnLogCollision(ref objectHit)}";
    }

    /// <summary>
    /// For debugging purposes
    /// </summary>
    protected override string OnLogHandleCollision(ref CollisionMessageInfo collisionMessage)
    {
        return $"[**Ship Damage**][{collisionMessage.GetCollisionType()}][Client-{OwnerClientId}][Dmg:{collisionMessage.Damage}]-{base.OnLogHandleCollision(ref collisionMessage)}";
    }
    #endregion
}

#region Ship laser & thruster configuration enums and classes
public enum LaserCannonPositions
{
    LeftBarrel,
    RightBarrel,
}

[Serializable]
public class LaserCannon : ShipPart
{
    public LaserCannonPositions Position;
}

public enum ThrusterPositions
{
    LeftForward,
    LeftReverse,
    RightForward,
    RightReverse,
    Main,
}

[Serializable]
public class ShipThruster : ShipPart
{
    public ThrusterPositions Position;
    public ParticleSystem ThrusterFX;
    public bool IsActive;
}

[Serializable]
public class ShipPart
{
    public GameObject FXObject;
    public GameObject PositionNode;
}
#endregion