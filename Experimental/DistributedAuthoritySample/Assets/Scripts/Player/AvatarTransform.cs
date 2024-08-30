using System;
using UnityEngine;
using Unity.Multiplayer.Samples.SocialHub.Input;
using Unity.Multiplayer.Samples.SocialHub.Physics;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class AvatarTransform : PhysicsObjectMotion, INetworkUpdateSystem
    {
        [SerializeField]
        PlayerInput m_PlayerInput;
        [SerializeField]
        AvatarInputs m_AvatarInputs;
        [SerializeField]
        AvatarInteractions m_AvatarInteractions;
        [SerializeField]
        PhysicsPlayerController m_PhysicsPlayerController;

        private Camera mainCamera;

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

            var spawnPosition = new Vector3(0f, 1.5f, 0f);
            transform.SetPositionAndRotation(position: spawnPosition, rotation: Quaternion.identity);
            Rigidbody.position = spawnPosition;
            Rigidbody.linearVelocity = Vector3.zero;

            this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.Update);
            this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.FixedUpdate);

            // Inject this avatar into the camera control system
            var cameraControl = Camera.main?.GetComponent<CameraControl>();
            if (cameraControl != null)
            {
                cameraControl.SetAvatarTransform(this);
                SetCameraReference(Camera.main); // Store the camera reference
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

            // Undo camera system injection
            var cameraControl = Camera.main?.GetComponent<CameraControl>();
            if (cameraControl != null)
            {
                cameraControl.SetAvatarTransform(null);
            }
        }

        private void OnJumped()
        {
            m_PhysicsPlayerController.SetJump(true);
        }

        private void OnTransformUpdate()
        {
            if (mainCamera != null)
            {
                Vector3 forward = mainCamera.transform.forward;
                Vector3 right = mainCamera.transform.right;

                // Project forward and right onto the x-z plane (horizontal plane)
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                Vector3 movement = forward * m_AvatarInputs.Move.y + right * m_AvatarInputs.Move.x;
                m_PhysicsPlayerController.SetMovement(movement);
                m_PhysicsPlayerController.SetSprint(m_AvatarInputs.Sprint);
            }
        }

        public void SetCameraReference(Camera camera)
        {
            mainCamera = camera;
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
