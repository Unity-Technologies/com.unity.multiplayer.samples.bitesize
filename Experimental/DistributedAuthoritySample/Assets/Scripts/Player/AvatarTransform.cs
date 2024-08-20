using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using com.unity.multiplayer.samples.distributed_authority.input;

namespace com.unity.multiplayer.samples.distributed_authority.gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class AvatarTransform : NetworkTransform
    {
        [SerializeField]
        private Rigidbody m_Rigidbody;
        [SerializeField]
        private PlayerInput m_PlayerInput;
        [SerializeField]
        private AvatarInputs m_AvatarInputs;
        [SerializeField]
        private float m_WalkSpeed;
        [SerializeField]
        private float m_SprintSpeed;
        [SerializeField]
        private float m_Acceleration;
        [SerializeField]
        private float m_DragCoefficient;
        [SerializeField]
        private float m_AirControlFactor;
        [SerializeField]
        private float m_JumpImpulse; // Fixed typo from "Impusle" to "Impulse"
        [SerializeField]
        private float m_CustomGravityMultiplier;
        [SerializeField]
        private float m_RotationSpeed;
        [SerializeField]
        private float m_GroundCheckDistance;

        [SerializeField]
        private TransformAnchor protagonistTransformAnchor; // Added the TransformAnchor

        private Vector3 m_Movement;
        private bool m_Jump;
        private bool m_IsGrounded;
        private RaycastHit[] m_RaycastHits = new RaycastHit[1];
        private Ray m_Ray;

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

            m_Rigidbody.freezeRotation = true;

            var spawnPosition = new Vector3(0f, 1.5f, 0f);
            transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
            m_Rigidbody.position = spawnPosition;
            m_Rigidbody.linearVelocity = Vector3.zero;

            // Set the protagonist's transform anchor for camera follow
            //protagonistTransformAnchor.Provide(transform);
        }

        private void Update()
        {
            if (!IsSpawned || !HasAuthority)
            {
                return;
            }

            m_Movement = new Vector3(m_AvatarInputs.Move.x, 0, m_AvatarInputs.Move.y).normalized;

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

        private void ApplyMovement()
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
                var force = velocityChange * m_Acceleration;
                m_Rigidbody.AddForce(force, ForceMode.Acceleration);
            }
            else
            {
                var force = velocityChange * (m_Acceleration * m_AirControlFactor);
                m_Rigidbody.AddForce(force, ForceMode.Acceleration);
            }
        }

        private void ApplyJump()
        {
            if (m_IsGrounded && m_Jump)
            {
                m_Rigidbody.AddForce(Vector3.up * m_JumpImpulse, ForceMode.Impulse);
                m_Jump = false;
            }
        }

        private void UpdateGroundedStatus()
        {
            m_IsGrounded = IsGrounded();
        }

        private bool IsGrounded()
        {
            m_Ray.origin = m_Rigidbody.worldCenterOfMass;
            m_Ray.direction = Vector3.down;
            return Physics.RaycastNonAlloc(m_Ray, m_RaycastHits, m_GroundCheckDistance) > 0;
        }

        private void FixedUpdate()
        {
            if (!IsSpawned || !HasAuthority || m_Rigidbody.isKinematic)
            {
                return;
            }

            UpdateGroundedStatus();
            ApplyMovement();
            ApplyJump();
            ApplyDrag();
            ApplyCustomGravity();
        }

        private void ApplyDrag()
        {
            var groundVelocity = m_Rigidbody.linearVelocity;
            groundVelocity.y = 0f;
            if (groundVelocity.magnitude > 0f)
            {
                var dragForce = -m_DragCoefficient * groundVelocity.magnitude * groundVelocity;
                m_Rigidbody.AddForce(dragForce, ForceMode.Acceleration);
            }
        }

        private void ApplyCustomGravity()
        {
            if (!m_IsGrounded)
            {
                var customGravity = Physics.gravity * (m_CustomGravityMultiplier - 1);
                m_Rigidbody.AddForce(customGravity, ForceMode.Acceleration);
            }
        }
    }
}
