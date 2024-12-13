using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using Unity.Multiplayer.Samples.SocialHub.Input;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    class CameraControl : MonoBehaviour
    {
        [SerializeField]
        CinemachineCamera m_FreeLookVCamera;
        const float k_MouseLookMultiplier = 15f;
        const float k_GamepadLookMultiplier = 100f;
        const float k_VerticalScaling = 0.01f;

        Transform m_FollowTransform;
        bool m_CameraMovementLock;
        bool m_IsRotatePressed;
        CinemachineOrbitalFollow m_OrbitalFollow;

        void Awake()
        {
            GameInput.Actions.Player.Rotate.started += OnRotateStarted;
            GameInput.Actions.Player.Rotate.canceled += OnRotateCanceled;

            m_OrbitalFollow = m_FreeLookVCamera.GetComponent<CinemachineOrbitalFollow>();
            m_FreeLookVCamera.Follow = null;
        }

        void OnDestroy()
        {
            GameInput.Actions.Player.Rotate.started -= OnRotateStarted;
            GameInput.Actions.Player.Rotate.canceled -= OnRotateCanceled;

            StopAllCoroutines();
        }

        internal void SetTransform(Transform newTransform)
        {
            m_FollowTransform = newTransform;
            SetupProtagonistVirtualCamera();
        }

        void OnRotateStarted(InputAction.CallbackContext _)
        {
            m_IsRotatePressed = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            StartCoroutine(DisableMouseControlForFrame());
        }

        void OnRotateCanceled(InputAction.CallbackContext _)
        {
            m_IsRotatePressed = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        IEnumerator DisableMouseControlForFrame()
        {
            m_CameraMovementLock = true;
            yield return new WaitForEndOfFrame();
            m_CameraMovementLock = false;
        }

        void Update()
        {
            if (GameInput.Actions.Player.Look.activeControl == null)
            {
                return;
            }

            var device = GameInput.Actions.Player.Look.activeControl.device;
            switch (device)
            {
                case Mouse:
                    HandleRotateMouse();
                    break;
                case Touchscreen:
                    HandleRotateTouchscreen();
                    break;
                case Gamepad:
                    HandleRotateGamepad();
                    break;
            }
        }

        void HandleRotateMouse()
        {
            if (m_CameraMovementLock || !m_IsRotatePressed)
            {
                return;
            }

            var cameraMovement = GameInput.Actions.Player.Look.ReadValue<Vector2>();
            var deviceScaling = k_MouseLookMultiplier * Time.deltaTime;
            m_OrbitalFollow.HorizontalAxis.Value += cameraMovement.x * deviceScaling;
            m_OrbitalFollow.VerticalAxis.Value += cameraMovement.y * deviceScaling * k_VerticalScaling;
        }

        void HandleRotateTouchscreen()
        {
            var cameraMovement = GameInput.Actions.Player.Look.ReadValue<Vector2>();
            var deviceScaling = k_MouseLookMultiplier * Time.deltaTime;
            m_OrbitalFollow.HorizontalAxis.Value += cameraMovement.x * deviceScaling;
            m_OrbitalFollow.VerticalAxis.Value += cameraMovement.y * deviceScaling * k_VerticalScaling;
        }

        void HandleRotateGamepad()
        {
            var cameraMovement = GameInput.Actions.Player.Look.ReadValue<Vector2>();
            var deviceScaling = k_GamepadLookMultiplier * Time.deltaTime;
            m_OrbitalFollow.HorizontalAxis.Value += cameraMovement.x * deviceScaling;
            m_OrbitalFollow.VerticalAxis.Value += cameraMovement.y * deviceScaling * k_VerticalScaling;
        }

        void SetupProtagonistVirtualCamera()
        {
            if (m_FollowTransform == null)
            {
                return;
            }

            m_FreeLookVCamera.Follow = m_FollowTransform;
            CinemachineCore.ResetCameraState(); // snap to new position
        }
    }
}
