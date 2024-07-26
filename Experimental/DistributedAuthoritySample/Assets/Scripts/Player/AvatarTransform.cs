using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// The custom editor for the <see cref="AvatarTransform"/> component.
/// </summary>
[CustomEditor(typeof(AvatarTransform), true)]
public class AvatarTransformEditor : PhysicsObjectMotionEditor
{
    SerializedProperty m_PlayerInput;
    SerializedProperty m_AvatarInputs;
    SerializedProperty m_WalkSpeed;
    SerializedProperty m_SprintSpeed;
    SerializedProperty m_Acceleration;
    SerializedProperty m_Deceleration;
    SerializedProperty m_AirControlFactor;
    SerializedProperty m_JumpImpulse;
    SerializedProperty m_CustomGravityMultiplier;
    SerializedProperty m_RotationSpeed;
    SerializedProperty m_GroundCheckDistance;

    public override void OnEnable()
    {
        m_PlayerInput = serializedObject.FindProperty(nameof(AvatarTransform.PlayerInput));
        m_AvatarInputs = serializedObject.FindProperty(nameof(AvatarTransform.AvatarInputs));
        m_WalkSpeed = serializedObject.FindProperty(nameof(AvatarTransform.WalkSpeed));
        m_SprintSpeed = serializedObject.FindProperty(nameof(AvatarTransform.SprintSpeed));
        m_Acceleration = serializedObject.FindProperty(nameof(AvatarTransform.Acceleration));
        m_Deceleration = serializedObject.FindProperty(nameof(AvatarTransform.Deceleration));
        m_AirControlFactor = serializedObject.FindProperty(nameof(AvatarTransform.AirControlFactor));
        m_JumpImpulse = serializedObject.FindProperty(nameof(AvatarTransform.JumpImpusle));
        m_CustomGravityMultiplier = serializedObject.FindProperty(nameof(AvatarTransform.CustomGravityMultiplier));
        m_RotationSpeed = serializedObject.FindProperty(nameof(AvatarTransform.RotationSpeed));
        m_GroundCheckDistance = serializedObject.FindProperty(nameof(AvatarTransform.GroundCheckDistance));

        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        var avatarTransform = target as AvatarTransform;
        avatarTransform.PropertiesVisible = EditorGUILayout.BeginFoldoutHeaderGroup(avatarTransform.PropertiesVisible, $"{nameof(AvatarTransform)} Properties");
        if (avatarTransform.PropertiesVisible)
        {
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.PropertyField(m_PlayerInput);
            EditorGUILayout.PropertyField(m_AvatarInputs);
            EditorGUILayout.PropertyField(m_WalkSpeed);
            EditorGUILayout.PropertyField(m_SprintSpeed);
            EditorGUILayout.PropertyField(m_Acceleration);
            EditorGUILayout.PropertyField(m_Deceleration);
            EditorGUILayout.PropertyField(m_AirControlFactor);
            EditorGUILayout.PropertyField(m_JumpImpulse);
            EditorGUILayout.PropertyField(m_CustomGravityMultiplier);
            EditorGUILayout.PropertyField(m_RotationSpeed);
            EditorGUILayout.PropertyField(m_GroundCheckDistance);
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

[RequireComponent(typeof(Rigidbody))]
public class AvatarTransform : PhysicsObjectMotion
{
#if UNITY_EDITOR
    public bool PropertiesVisible = false;
#endif

    public PlayerInput PlayerInput;
    public AvatarInputs AvatarInputs;
    public float WalkSpeed;
    public float SprintSpeed;
    public float Acceleration;
    public float Deceleration;
    public float AirControlFactor;
    public float JumpImpusle;
    public float CustomGravityMultiplier;
    public float RotationSpeed;
    public float GroundCheckDistance;

    Vector3 m_Movement;

    // grab jump state from input and clear after consumed
    bool m_Jump;

    // cached grounded check
    bool m_IsGrounded;

    RaycastHit[] m_RaycastHits = new RaycastHit[1];
    Ray m_Ray;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            //enabled = false;
            return;
        }

        PlayerInput.enabled = true;

        // Freeze rotation on the x and z axes to prevent toppling
        Rigidbody.freezeRotation = true;

        transform.SetPositionAndRotation(position: Vector3.up, rotation: Quaternion.identity);
        Rigidbody.position = Vector3.up;
        UpdateVelocity(Vector3.zero);
        UpdateAngularVelocity(Vector3.zero);
    }

    void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        m_Movement = new Vector3(AvatarInputs.move.x, 0, AvatarInputs.move.y).normalized;

        // Handle rotation based on input direction
        if (m_Movement.magnitude >= 0.1f)
        {
            var targetAngle = Mathf.Atan2(m_Movement.x, m_Movement.z) * Mathf.Rad2Deg;
            var targetRotation = Quaternion.Euler(0, targetAngle, 0);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * RotationSpeed);
        }

        if (AvatarInputs.jump && IsGrounded())
        {
            m_Jump = true;
            AvatarInputs.jump = false;
        }
    }

    void ApplyMovement()
    {
        var velocity = GetObjectVelocity();
        var desiredVelocity = m_Movement * (AvatarInputs.sprint ? SprintSpeed : WalkSpeed);
        var targetVelocity = new Vector3(desiredVelocity.x, velocity.y, desiredVelocity.z);
        var velocityChange = targetVelocity - velocity;

        if (m_IsGrounded)
        {
            // Apply force proportional to acceleration while grounded
            var force = velocityChange * Acceleration;
            Rigidbody.AddForce(force, ForceMode.Acceleration);

            if (m_Jump)
            {
                Rigidbody.AddForce(Vector3.up * JumpImpusle, ForceMode.Impulse);
                m_Jump = false;
            }
        }
        else
        {
            // Apply reduced force in the air for air control
            var force = velocityChange * (Acceleration * AirControlFactor);
            Rigidbody.AddForce(force, ForceMode.Acceleration);
        }

        ApplyDeceleration();
    }

    void UpdateGroundedStatus()
    {
        m_IsGrounded = IsGrounded();
    }

    bool IsGrounded()
    {
        // Perform a raycast to check if the character is grounded
        m_Ray.origin = transform.position;
        m_Ray.direction = Vector3.down;
        return Physics.RaycastNonAlloc(m_Ray, m_RaycastHits, GroundCheckDistance) > 0;
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

        UpdateGroundedStatus();

        ApplyMovement();

        ApplyCustomGravity();

        base.FixedUpdate();
    }

    void ApplyDeceleration()
    {
        var velocity = GetObjectVelocity();
        if (m_Movement.magnitude < 0.1f && velocity.magnitude > 0)
        {
            // Apply deceleration force to stop movement
            var horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            var decelerationForce = -horizontalVelocity.normalized * Deceleration;
            Rigidbody.AddForce(decelerationForce, ForceMode.Acceleration);
        }
    }

    void ApplyCustomGravity()
    {
        // custom gravity
        if (!m_IsGrounded)
        {
            var customGravity = Physics.gravity * (CustomGravityMultiplier - 1);
            Rigidbody.AddForce(customGravity, ForceMode.Acceleration);
        }
    }
}
