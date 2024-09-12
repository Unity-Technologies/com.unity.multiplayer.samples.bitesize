using UnityEngine;
using System;

namespace Unity.Multiplayer.Samples.SocialHub.Physics
{
    [RequireComponent(typeof(Rigidbody))]
    class PhysicsPlayerController : MonoBehaviour
    {
        [SerializeField]
        Rigidbody m_Rigidbody;

        [SerializeField]
        PhysicsPlayerControllerSettings m_PhysicsPlayerControllerSettings;

        // cached grounded check
        internal bool Grounded { get; private set; }

        RaycastHit[] m_RaycastHits = new RaycastHit[1];
        Ray m_Ray;

        Vector3 m_Movement;
        bool m_Jump;
        bool m_Sprint;

        internal event Action PlayerJumped;

        internal void OnFixedUpdate()
        {
            if (m_Rigidbody != null && m_Rigidbody.isKinematic)
            {
                return;
            }

            UpdateGroundedStatus();

            ApplyMovement();

            ApplyJump();

            ApplyDrag();

            ApplyCustomGravity();
        }

        void UpdateGroundedStatus()
        {
            Grounded = IsGrounded();
        }

        bool IsGrounded()
        {
            // Perform a raycast to check if the character is grounded
            m_Ray.origin = m_Rigidbody.worldCenterOfMass;
            m_Ray.direction = Vector3.down;
            return UnityEngine.Physics.RaycastNonAlloc(m_Ray, m_RaycastHits, m_PhysicsPlayerControllerSettings.GroundCheckDistance) > 0;
        }

        void ApplyMovement()
        {
            if (Mathf.Approximately(m_Movement.magnitude, 0f))
            {
                return;
            }

            var velocity = m_Rigidbody.linearVelocity;
            var desiredVelocity = m_Movement * (m_Sprint ? m_PhysicsPlayerControllerSettings.SprintSpeed : m_PhysicsPlayerControllerSettings.WalkSpeed);
            var targetVelocity = new Vector3(desiredVelocity.x, velocity.y, desiredVelocity.z);
            var velocityChange = targetVelocity - velocity;

            if (Grounded)
            {
                // Apply force proportional to acceleration while grounded
                var force = velocityChange * m_PhysicsPlayerControllerSettings.Acceleration;
                m_Rigidbody.AddForce(force, ForceMode.Force);
            }
            else
            {
                // Apply reduced force in the air for air control
                var force = velocityChange * (m_PhysicsPlayerControllerSettings.Acceleration * m_PhysicsPlayerControllerSettings.AirControlFactor);
                m_Rigidbody.AddForce(force, ForceMode.Force);
            }

            // maybe add magnitude check?
            var targetAngle = Mathf.Atan2(m_Movement.x, m_Movement.z) * Mathf.Rad2Deg;
            var targetRotation = Quaternion.Euler(0, targetAngle, 0);
            var smoothRotation = Quaternion.Lerp(m_Rigidbody.rotation, targetRotation, Time.fixedDeltaTime * m_PhysicsPlayerControllerSettings.RotationSpeed);
            m_Rigidbody.MoveRotation(smoothRotation);

            m_Movement = Vector3.zero;
        }

        void ApplyJump()
        {
            if (m_Jump && Grounded)
            {
                m_Rigidbody.AddForce(Vector3.up * m_PhysicsPlayerControllerSettings.JumpImpusle, ForceMode.Impulse);
                PlayerJumped?.Invoke();
            }
            m_Jump = false;
        }

        void ApplyDrag()
        {
            var groundVelocity = m_Rigidbody.linearVelocity;
            groundVelocity.y = 0f;
            if (groundVelocity.magnitude > 0f)
            {
                // Apply deceleration force to stop movement
                var dragForce = -m_PhysicsPlayerControllerSettings.DragCoefficient * groundVelocity.magnitude * groundVelocity;
                m_Rigidbody.AddForce(dragForce, ForceMode.Acceleration);
            }
        }

        void ApplyCustomGravity()
        {
            var customGravity = UnityEngine.Physics.gravity * (m_PhysicsPlayerControllerSettings.CustomGravityMultiplier - 1);
            m_Rigidbody.AddForce(customGravity, ForceMode.Acceleration);
        }

        public void SetMovement(Vector3 movement)
        {
            m_Movement = movement;
        }

        public void SetJump(bool jump)
        {
            if (jump)
            {
                m_Jump = true;
            }
        }

        public void SetSprint(bool sprint)
        {
            m_Sprint = sprint;
        }
    }
}
