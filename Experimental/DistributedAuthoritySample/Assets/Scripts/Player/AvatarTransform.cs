using System;
using Unity.Multiplayer.Samples.SocialHub.Gameplay;
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
#if UNITY_EDITOR
        public bool AvatarTransformPropertiesVisible;
#endif

        public PlayerInput PlayerInput;
        
        public AvatarInputs AvatarInputs;
        
        public AvatarInteractions AvatarInteractions;
        
        public PhysicsPlayerController PhysicsPlayerController;

        Camera m_MainCamera;

        public override void OnNetworkSpawn()
        {
            gameObject.name = $"[Client-{OwnerClientId}]{name}";

            if (!HasAuthority)
            {
                base.OnNetworkSpawn();
                return;
            }

            PlayerInput.enabled = true;
            AvatarInputs.enabled = true;
            AvatarInputs.Jumped += OnJumped;
            AvatarInteractions.enabled = true;
            PhysicsPlayerController.enabled = true;
            Rigidbody.isKinematic = false;
            Rigidbody.freezeRotation = true;
            // important: modifying a transform's properties before invoking base.OnNetworkSpawn() will initialize everything based on the transform's current setting
            var spawnPoint = PlayerSpawnPoints.Instance.GetRandomSpawnPoint();
            transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            Rigidbody.linearVelocity = Vector3.zero;

            this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.Update);
            this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.FixedUpdate);

            var cameraControl = Camera.main?.GetComponent<CameraControl>();
            if (cameraControl != null)
            {
                cameraControl.SetTransform(transform);
                m_MainCamera = Camera.main;
            }
            else
            {
                Debug.LogError("CameraControl not found on the Main Camera or Main Camera is missing.");
            }

            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (AvatarInputs != null)
            {
                AvatarInputs.Jumped -= OnJumped;
            }

            this.UnregisterAllNetworkUpdates();

            var cameraControl = Camera.main?.GetComponent<CameraControl>();
            if (cameraControl != null)
            {
                cameraControl.SetTransform(null);
            }
        }

        void OnJumped()
        {
            PhysicsPlayerController.SetJump(true);
        }

        void OnTransformUpdate()
        {
            if (m_MainCamera != null)
            {
                Vector3 forward = m_MainCamera.transform.forward;
                Vector3 right = m_MainCamera.transform.right;

                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                Vector3 movement = forward * AvatarInputs.Move.y + right * AvatarInputs.Move.x;
                PhysicsPlayerController.SetMovement(movement);
                PhysicsPlayerController.SetSprint(AvatarInputs.Sprint);
            }
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.Update:
                    OnTransformUpdate();
                    break;
                case NetworkUpdateStage.FixedUpdate:
                    PhysicsPlayerController.OnFixedUpdate();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(updateStage), updateStage, null);
            }
        }
    }
}
