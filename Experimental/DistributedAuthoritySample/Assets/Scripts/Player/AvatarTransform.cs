using Unity.Netcode.Components;
using UnityEngine;
using Unity.Multiplayer.Samples.SocialHub.Input;
using UnityEngine.InputSystem;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class AvatarTransform : NetworkTransform
    {
        [SerializeField]
        Rigidbody m_Rigidbody;
        [SerializeField]
        PlayerInput m_PlayerInput;
        [SerializeField]
        AvatarInputs m_AvatarInputs;
        [SerializeField]
        float m_WalkSpeed;
        [SerializeField]
        float m_SprintSpeed;
        [SerializeField]
        float m_Acceleration;
        [SerializeField]
        float m_DragCoefficient;
        [SerializeField]
        float m_AirControlFactor;
        [SerializeField]
        float m_JumpImpusle;
        [SerializeField]
        float m_CustomGravityMultiplier;
        [SerializeField]
        float m_RotationSpeed;
        [SerializeField]
        float m_GroundCheckDistance;

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
            m_AvatarInputs.enabled = true;
            m_Rigidbody.isKinematic = false;

            var spawnPosition = new Vector3(0f, 1.5f, 0f);
            m_Rigidbody.position = spawnPosition;
            m_Rigidbody.rotation = Quaternion.identity;
            m_Rigidbody.linearVelocity = Vector3.zero;
        }

        void Update()
        {
            if (!IsSpawned || !HasAuthority)
            {
                return;
            }

            // Ensure movement is relative to the camera orientation
            Camera mainCamera = Camera.main;
            Vector3 forward = mainCamera.transform.forward;
            Vector3 right = mainCamera.transform.right;

            // Project forward and right onto the x-z plane (horizontal plane)
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 desiredMoveDirection = forward * m_AvatarInputs.Move.y + right * m_AvatarInputs.Move.x;
            m_Movement = desiredMoveDirection.normalized;

            if (IsGrounded() && m_AvatarInputs.Jump)
            {
                m_Jump = true;
                m_AvatarInputs.Jump = false;
            }
        }

        void ApplyMovement()
        {
            if (Mathf.Approximately(m_Movement.sqrMagnitude, 0f))
            {
                return;
            }

            var velocity = m_Rigidbody.linearVelocity;
            var desiredVelocity = m_Movement * (m_AvatarInputs.Sprint ? m_SprintSpeed : m_WalkSpeed);
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

        void ApplyRotation()
        {
            // Rotate to face direction of motion
            var angularVelocity = Vector3.zero;
            if (m_Movement.sqrMagnitude > 0.01f)
            {
                var delta = Mathf.Atan2(m_Movement.x, m_Movement.z) - m_Rigidbody.rotation.eulerAngles.y * Mathf.Deg2Rad;
                if (Mathf.Abs(delta) > Mathf.PI)
                    delta -= 2 * Mathf.PI * Mathf.Sign(delta);
                angularVelocity = m_RotationSpeed * delta * Vector3.up;
            }
            m_Rigidbody.angularVelocity = angularVelocity;
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

            ApplyRotation();

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
