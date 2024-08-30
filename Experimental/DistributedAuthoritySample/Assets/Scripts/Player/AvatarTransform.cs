using System;
using UnityEngine;
using Unity.Multiplayer.Samples.SocialHub.Input;
using Unity.Multiplayer.Samples.SocialHub.Physics;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    [RequireComponent(typeof(Rigidbody))]
    class AvatarTransform : PhysicsObjectMotion, INetworkUpdateSystem
    {
        [SerializeField]
        PlayerInput m_PlayerInput;
        [SerializeField]
        AvatarInputs m_AvatarInputs;
        [SerializeField]
        AvatarInteractions m_AvatarInteractions;
        [SerializeField]
        PhysicsPlayerController m_PhysicsPlayerController;

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
            m_AvatarInputs.Jumped += OnJumped;
            m_AvatarInteractions.enabled = true;
            m_PhysicsPlayerController.enabled = true;
            Rigidbody.isKinematic = false;

            // Freeze rotation on the x and z axes to prevent toppling
            Rigidbody.freezeRotation = true;
            // TODO: MTT-8899 fetch spawn point
            var spawnPosition = new Vector3(51.4228516f,8.88483906f,-11.031064f);
            Teleport(spawnPosition, Quaternion.identity, Vector3.one);
            Rigidbody.linearVelocity = Vector3.zero;

            this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.Update);
            this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.FixedUpdate);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (m_AvatarInputs)
            {
                m_AvatarInputs.Jumped -= OnJumped;
            }

            this.UnregisterAllNetworkUpdates();
        }

        void OnJumped()
        {
            m_PhysicsPlayerController.SetJump(true);
        }

        void OnTransformUpdate()
        {
            var movement = new Vector3(m_AvatarInputs.Move.x, 0, m_AvatarInputs.Move.y).normalized;

            m_PhysicsPlayerController.SetMovement(movement);
            m_PhysicsPlayerController.SetSprint(m_AvatarInputs.Sprint);
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.Update:
                    OnTransformUpdate();
                    break;
                case NetworkUpdateStage.FixedUpdate:
                    m_PhysicsPlayerController.OnFixedUpdate();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(updateStage), updateStage, null);
            }

        }
    }
}
