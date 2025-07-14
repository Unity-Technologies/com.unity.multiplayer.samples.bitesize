using Unity.Multiplayer.Samples.SocialHub.Input;
using Unity.Multiplayer.Samples.SocialHub.Physics;
using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    [RequireComponent(typeof(PhysicsPlayerController))]
    class AvatarNetworkAnimator : NetworkAnimator
    {
        [SerializeField]
        PhysicsPlayerController m_PhysicsPlayerController;

        static readonly int k_GroundedId = Animator.StringToHash("Grounded");
        static readonly int k_MoveId = Animator.StringToHash("Move");
        static readonly int k_JumpId = Animator.StringToHash("Jump");

        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_PhysicsPlayerController.PlayerJumped += OnPlayerJumped;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (m_PhysicsPlayerController)
            {
                m_PhysicsPlayerController.PlayerJumped -= OnPlayerJumped;
            }
        }

        void OnPlayerJumped()
        {
            SetTrigger(k_JumpId);
        }

        void LateUpdate()
        {
            if (!HasAuthority)
            {
                return;
            }

            Animator.SetBool(k_GroundedId, m_PhysicsPlayerController.Grounded);
            var moveInput = GameInput.Actions.Player.Move.ReadValue<Vector2>();
            var isSprinting = GameInput.Actions.Player.Sprint.ReadValue<float>() > 0f;
            Animator.SetFloat(k_MoveId, moveInput.magnitude * (isSprinting ? 2f : 1f));
        }
    }
}
