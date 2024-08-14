using Unity.Netcode.Components;
using UnityEngine;
using com.unity.multiplayer.samples.socialhub.input;

namespace com.unity.multiplayer.samples.socialhub.player
{
    [RequireComponent(typeof(Rigidbody))]
    public class AvatarTransform : NetworkTransform
    {
        [SerializeField]
        Rigidbody m_Rigidbody;
        [SerializeField]
        MonoBehaviour m_PlayerInput;
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

            m_Movement = new Vector3(m_AvatarInputs.Move.x, 0, m_AvatarInputs.Move.y).normalized;

            // Handle rotation based on input direction
            if (m_Movement.magnitude >= 0.1f)
            {
                var targetAngle = Mathf.Atan2(m_Movement.x, m_Movement.z) * Mathf.Rad2Deg;
                var targetRotation = Quaternion.Euler(0, targetAngle, 0);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * m_RotationSpeed);
            }

            if (IsGrounded() && m_AvatarInputs.Jump)
            {
                m_Jump = true;
                m_AvatarInputs.Jump = false;
            }
        }

        void ApplyMovement()
        {
            if (Mathf.Approximately(m_Movement.magnitude, 0f))
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
