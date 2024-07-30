using Unity.Netcode.Components;
using Unity.Netcode.Editor;
using UnityEngine;
using UnityEngine.InputSystem;
using com.unity.multiplayer.samples.distributed_authority.input;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.unity.multiplayer.samples.distributed_authority.gameplay
{
#if UNITY_EDITOR
    /// <summary>
    /// The custom editor for the <see cref="AvatarTransform"/> component.
    /// </summary>
    [CustomEditor(typeof(AvatarTransform), true)]
    public class AvatarTransformEditor : NetworkTransformEditor
    {
        SerializedProperty m_Rigidbody;
        SerializedProperty m_PlayerInput;
        SerializedProperty m_AvatarInputs;
        SerializedProperty m_WalkSpeed;
        SerializedProperty m_SprintSpeed;
        SerializedProperty m_Acceleration;
        SerializedProperty m_DragCoefficient;
        SerializedProperty m_AirControlFactor;
        SerializedProperty m_JumpImpulse;
        SerializedProperty m_CustomGravityMultiplier;
        SerializedProperty m_RotationSpeed;
        SerializedProperty m_GroundCheckDistance;

        public override void OnEnable()
        {
            m_Rigidbody = serializedObject.FindProperty(nameof(AvatarTransform.m_Rigidbody));
            m_PlayerInput = serializedObject.FindProperty(nameof(AvatarTransform.m_PlayerInput));
            m_AvatarInputs = serializedObject.FindProperty(nameof(AvatarTransform.m_AvatarInputs));
            m_WalkSpeed = serializedObject.FindProperty(nameof(AvatarTransform.m_WalkSpeed));
            m_SprintSpeed = serializedObject.FindProperty(nameof(AvatarTransform.m_SprintSpeed));
            m_Acceleration = serializedObject.FindProperty(nameof(AvatarTransform.m_Acceleration));
            m_DragCoefficient = serializedObject.FindProperty(nameof(AvatarTransform.m_DragCoefficient));
            m_AirControlFactor = serializedObject.FindProperty(nameof(AvatarTransform.m_AirControlFactor));
            m_JumpImpulse = serializedObject.FindProperty(nameof(AvatarTransform.m_JumpImpusle));
            m_CustomGravityMultiplier = serializedObject.FindProperty(nameof(AvatarTransform.m_CustomGravityMultiplier));
            m_RotationSpeed = serializedObject.FindProperty(nameof(AvatarTransform.m_RotationSpeed));
            m_GroundCheckDistance = serializedObject.FindProperty(nameof(AvatarTransform.m_GroundCheckDistance));

            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            var avatarTransform = target as AvatarTransform;
            avatarTransform.PropertiesVisible = EditorGUILayout.BeginFoldoutHeaderGroup(avatarTransform.PropertiesVisible, $"{nameof(AvatarTransform)} Properties");
            if (avatarTransform.PropertiesVisible)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.PropertyField(m_Rigidbody);
                EditorGUILayout.PropertyField(m_PlayerInput);
                EditorGUILayout.PropertyField(m_AvatarInputs);
                EditorGUILayout.PropertyField(m_WalkSpeed);
                EditorGUILayout.PropertyField(m_SprintSpeed);
                EditorGUILayout.PropertyField(m_Acceleration);
                EditorGUILayout.PropertyField(m_DragCoefficient);
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
    public class AvatarTransform : NetworkTransform
    {
#if UNITY_EDITOR
        public bool PropertiesVisible = false;
#endif

        [SerializeField]
        internal Rigidbody m_Rigidbody;

        [SerializeField]
        internal PlayerInput m_PlayerInput;

        [SerializeField]
        internal AvatarInputs m_AvatarInputs;

        [SerializeField]
        internal float m_WalkSpeed;

        [SerializeField]
        internal float m_SprintSpeed;

        [SerializeField]
        internal float m_Acceleration;

        [SerializeField]
        internal float m_DragCoefficient;

        [SerializeField]
        internal float m_AirControlFactor;

        [SerializeField]
        internal float m_JumpImpusle;

        [SerializeField]
        internal float m_CustomGravityMultiplier;

        [SerializeField]
        internal float m_RotationSpeed;

        [SerializeField]
        internal float m_GroundCheckDistance;

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

            gameObject.name = $"[Client-{OwnerClientId}]{name}";

            if (!HasAuthority)
            {
                return;
            }

            m_PlayerInput.enabled = true;

            m_Rigidbody.isKinematic = false;

            // Freeze rotation on the x and z axes to prevent toppling
            m_Rigidbody.freezeRotation = true;

            var spawnPosition = new Vector3(0f, 1.5f, 0f);
            transform.SetPositionAndRotation(position: spawnPosition, rotation: Quaternion.identity);
            m_Rigidbody.position = spawnPosition;
            m_Rigidbody.linearVelocity = Vector3.zero;
        }

        void Update()
        {
            if (!IsSpawned || !HasAuthority)
            {
                return;
            }

            m_Movement = new Vector3(m_AvatarInputs.move.x, 0, m_AvatarInputs.move.y).normalized;

            // Handle rotation based on input direction
            if (m_Movement.magnitude >= 0.1f)
            {
                var targetAngle = Mathf.Atan2(m_Movement.x, m_Movement.z) * Mathf.Rad2Deg;
                var targetRotation = Quaternion.Euler(0, targetAngle, 0);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * m_RotationSpeed);
            }

            if (IsGrounded() && m_AvatarInputs.jump)
            {
                m_Jump = true;
                m_AvatarInputs.jump = false;
            }
        }

        void ApplyMovement()
        {
            if (Mathf.Approximately(m_Movement.magnitude, 0f))
            {
                return;
            }

            var velocity = m_Rigidbody.linearVelocity;
            var desiredVelocity = m_Movement * (m_AvatarInputs.sprint ? m_SprintSpeed : m_WalkSpeed);
            var targetVelocity = new Vector3(desiredVelocity.x, velocity.y, desiredVelocity.z);
            var velocityChange = targetVelocity - velocity;

            if (m_IsGrounded)
            {
                // Apply force proportional to acceleration while grounded
                var force = velocityChange * m_Acceleration;
                m_Rigidbody.AddForce(force, ForceMode.Acceleration);
            }
            else
            {
                // Apply reduced force in the air for air control
                var force = velocityChange * (m_Acceleration * m_AirControlFactor);
                m_Rigidbody.AddForce(force, ForceMode.Acceleration);
            }
        }

        void ApplyJump()
        {
            if (m_IsGrounded && m_Jump)
            {
                m_Rigidbody.AddForce(Vector3.up * m_JumpImpusle, ForceMode.Impulse);
                m_Jump = false;
            }
        }

        void UpdateGroundedStatus()
        {
            m_IsGrounded = IsGrounded();
        }

        bool IsGrounded()
        {
            // Perform a raycast to check if the character is grounded
            m_Ray.origin = m_Rigidbody.worldCenterOfMass;
            m_Ray.direction = Vector3.down;
            return Physics.RaycastNonAlloc(m_Ray, m_RaycastHits, m_GroundCheckDistance) > 0;
        }

        void FixedUpdate()
        {
            if (!IsSpawned || !HasAuthority || m_Rigidbody != null && m_Rigidbody.isKinematic)
            {
                return;
            }

            UpdateGroundedStatus();

            ApplyMovement();

            ApplyJump();

            ApplyDrag();

            ApplyCustomGravity();
        }

        void ApplyDrag()
        {
            var groundVelocity = m_Rigidbody.linearVelocity;
            groundVelocity.y = 0f;
            if (groundVelocity.magnitude > 0f)
            {
                // Apply deceleration force to stop movement
                var dragForce = -m_DragCoefficient * groundVelocity.magnitude * groundVelocity;
                m_Rigidbody.AddForce(dragForce, ForceMode.Acceleration);
            }
        }

        void ApplyCustomGravity()
        {
            // custom gravity
            if (!m_IsGrounded)
            {
                var customGravity = Physics.gravity * (m_CustomGravityMultiplier - 1);
                m_Rigidbody.AddForce(customGravity, ForceMode.Acceleration);
            }
        }
    }
}
