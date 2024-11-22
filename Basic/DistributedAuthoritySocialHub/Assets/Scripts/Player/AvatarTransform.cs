using System;
using Unity.Collections;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using Unity.Multiplayer.Samples.SocialHub.Gameplay;
using UnityEngine;
using Unity.Multiplayer.Samples.SocialHub.Input;
using Unity.Multiplayer.Samples.SocialHub.Physics;
using Unity.Multiplayer.Samples.SocialHub.UI;
using Unity.Netcode;
using Unity.Services.Authentication;
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

        Camera m_MainCamera;

        PlayersTopUIController m_TopUIController;

        NetworkVariable<FixedString32Bytes> m_PlayerName = new NetworkVariable<FixedString32Bytes>(string.Empty, readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Owner);

        public override void OnNetworkSpawn()
        {
            gameObject.name = $"[Client-{OwnerClientId}]{name}";

            m_TopUIController = FindFirstObjectByType<PlayersTopUIController>();
            m_PlayerName.OnValueChanged += OnPlayerNameChanged;
            OnPlayerNameChanged(string.Empty, m_PlayerName.Value);

            if (!HasAuthority)
            {
                base.OnNetworkSpawn();
                return;
            }

            // a randomly-generated suffix consisting of a hash and four digits (e.g. #1234) is automatically appended to the requested name
            m_PlayerName.Value = new FixedString32Bytes(AuthenticationService.Instance.PlayerName.Split('#')[0]);
            m_PlayerInput.enabled = true;
            m_AvatarInputs.enabled = true;
            m_AvatarInputs.Jumped += OnJumped;
            m_AvatarInteractions.enabled = true;
            m_PhysicsPlayerController.enabled = true;
            Rigidbody.isKinematic = false;
            Rigidbody.freezeRotation = true;
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

            GameplayEventHandler.OnBlockPlayerControls += OnBlockPlayerControls;

            base.OnNetworkSpawn();
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

            GameplayEventHandler.OnBlockPlayerControls -= OnBlockPlayerControls;
            m_TopUIController?.RemovePlayer(gameObject);
        }

        void OnJumped()
        {
            m_PhysicsPlayerController.SetJump(true);
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

                Vector3 movement = forward * m_AvatarInputs.Move.y + right * m_AvatarInputs.Move.x;
                m_PhysicsPlayerController.SetMovement(movement);
                m_PhysicsPlayerController.SetSprint(m_AvatarInputs.Sprint);
            }
        }

        void OnPlayerNameChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
        {
            m_TopUIController.AddPlayer(gameObject, newValue.Value);
        }

        void OnBlockPlayerControls(bool blockInput)
        {
            m_PlayerInput.enabled = !blockInput;
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
