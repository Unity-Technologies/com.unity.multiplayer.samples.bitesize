using Unity.Multiplayer.Samples.SocialHub.Input;
using Unity.Multiplayer.Samples.SocialHub.Physics;
using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    [RequireComponent(typeof(PhysicsPlayerController))]
    class AvatarAnimator : NetworkAnimator
    {
        [SerializeField]
        PhysicsPlayerController m_PhysicsPlayerController;

        [SerializeField]
        AvatarInputs m_AvatarInputs;

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
            Animator.SetFloat(k_MoveId, m_AvatarInputs.Move.magnitude);
        }
    }
}
