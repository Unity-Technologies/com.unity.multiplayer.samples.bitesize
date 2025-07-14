using System;
using Unity.Collections;
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
        AvatarInteractions m_AvatarInteractions;
        [SerializeField]
        PhysicsPlayerController m_PhysicsPlayerController;

        Camera m_MainCamera;

        PlayersTopUIController m_TopUIController;

        NetworkVariable<FixedString32Bytes> m_PlayerName = new NetworkVariable<FixedString32Bytes>(string.Empty, readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Owner);
        NetworkVariable<FixedString32Bytes> m_PlayerId = new NetworkVariable<FixedString32Bytes>(string.Empty, readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Owner);


        public override void OnNetworkSpawn()
        {
            gameObject.name = $"[Client-{OwnerClientId}]{name}";

            m_TopUIController = FindFirstObjectByType<PlayersTopUIController>();
            m_PlayerName.OnValueChanged += OnPlayerNameChanged;
            m_PlayerId.OnValueChanged += OnPlayerIdChanged;
            OnPlayerNameChanged(string.Empty, m_PlayerName.Value);

            if (!HasAuthority)
            {
                base.OnNetworkSpawn();
                return;
            }

            m_PlayerId.Value = new FixedString32Bytes(AuthenticationService.Instance.PlayerId);
            m_PlayerName.Value = new FixedString32Bytes(UIUtils.ExtractPlayerNameFromAuthUserName(AuthenticationService.Instance.PlayerName));
            m_PlayerInput.enabled = true;
            GameInput.Actions.Player.Jump.performed += OnJumped;
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

            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            GameInput.Actions.Player.Jump.performed -= OnJumped;

            this.UnregisterAllNetworkUpdates();

            var cameraControl = Camera.main?.GetComponent<CameraControl>();
            if (cameraControl != null)
            {
                cameraControl.SetTransform(null);
            }

            m_TopUIController?.RemovePlayer(gameObject);
        }

        void OnJumped(InputAction.CallbackContext _)
        {
            m_PhysicsPlayerController.SetJump(true);
        }

        void OnTransformUpdate()
        {
            if (m_MainCamera != null)
            {
                var forward = m_MainCamera.transform.forward;
                var right = m_MainCamera.transform.right;

                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                var moveInput = GameInput.Actions.Player.Move.ReadValue<Vector2>();
                var movement = forward * moveInput.y + right * moveInput.x;
                m_PhysicsPlayerController.SetMovement(movement);
                var isSprinting = GameInput.Actions.Player.Sprint.ReadValue<float>() > 0f;
                m_PhysicsPlayerController.SetSprint(isSprinting);
            }
        }

        void OnPlayerNameChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
        {
            m_TopUIController.AddOrUpdatePlayer(gameObject, newValue.Value,m_PlayerId.Value.Value);
        }

        void OnPlayerIdChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
        {
            m_TopUIController.AddOrUpdatePlayer(gameObject, m_PlayerName.Value.Value,newValue.Value);
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
