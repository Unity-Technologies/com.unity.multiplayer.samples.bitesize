using System;
using UnityEngine;
using Unity.Multiplayer.Samples.SocialHub.Input;
using Unity.Multiplayer.Samples.SocialHub.Physics;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    [RequireComponent(typeof(Rigidbody))]
    internal class AvatarTransform : PhysicsObjectMotion, INetworkUpdateSystem
    {
        [SerializeField]
        PlayerInput m_PlayerInput;
        [SerializeField]
        AvatarInputs m_AvatarInputs;
        [SerializeField]
        AvatarInteractions m_AvatarInteractions;
        [SerializeField]
        PhysicsPlayerController m_PhysicsPlayerController;

        static Camera m_MainCamera;

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
            Rigidbody.freezeRotation = true;

            var spawnPosition = new Vector3(0f, 1.5f, 0f);
            transform.SetPositionAndRotation(position: spawnPosition, rotation: Quaternion.identity);
            Rigidbody.position = spawnPosition;
            Rigidbody.linearVelocity = Vector3.zero;

            this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.Update);
            this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.FixedUpdate);

            var cameraControl = Camera.main?.GetComponent<CameraControl>();
            if (cameraControl != null)
            {
                cameraControl.SetTransform(transform);
                SetCameraReference(Camera.main);
            }
            else
            {
                Debug.LogError("CameraControl not found on the Main Camera or Main Camera is missing.");
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (m_AvatarInputs != null)
            {
                m_AvatarInputs.Jumped -= OnJumped;
            }

            this.UnregisterAllNetworkUpdates();

            var cameraControl = Camera.main?.GetComponent<CameraControl>();
            if (cameraControl != null)
            {
                cameraControl.SetTransform(null);
            }
        }

        private void OnJumped()
        {
            m_PhysicsPlayerController.SetJump(true);
        }

        private void OnTransformUpdate()
        {
            if (m_MainCamera != null)
            {
                Vector3 forward = m_MainCamera.transform.forward;
                Vector3 right = m_MainCamera.transform.right;

                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                Vector3 movement = forward * m_AvatarInputs.Move.y + right * m_AvatarInputs.Move.x;
                m_PhysicsPlayerController.SetMovement(movement);
                m_PhysicsPlayerController.SetSprint(m_AvatarInputs.Sprint);
            }
        }

        internal static void SetCameraReference(Camera camera)
        {
            m_MainCamera = camera;
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
