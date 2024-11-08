using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    class CameraControl : MonoBehaviour
    {
        [SerializeField]
        CinemachineCamera m_FreeLookVCamera;
        [SerializeField]
        InputActionReference m_RotateActionReference;
        [SerializeField]
        InputActionReference m_LookActionReference;
        const float k_MouseLookMultiplier = 15f;
        const float k_GamepadLookMultiplier = 100f;
        const float k_VerticalScaling = 0.01f;

        Transform m_FollowTransform;
        bool m_CameraMovementLock;
        bool m_IsRotatePressed;
        CinemachineOrbitalFollow m_OrbitalFollow;

        void Awake()
        {
            if (m_RotateActionReference != null)
            {
                m_RotateActionReference.action.started += OnRotateStarted;
                m_RotateActionReference.action.canceled += OnRotateCanceled;
                m_RotateActionReference.action.Enable();
            }

            m_OrbitalFollow = m_FreeLookVCamera.GetComponent<CinemachineOrbitalFollow>();
            m_FreeLookVCamera.Follow = null;
        }

        void OnDestroy()
        {
            if (m_RotateActionReference != null)
            {
                m_RotateActionReference.action.started -= OnRotateStarted;
                m_RotateActionReference.action.canceled -= OnRotateCanceled;
            }

            StopAllCoroutines();
        }

        void OnEnable()
        {
            if (m_RotateActionReference != null && m_LookActionReference != null)
            {
                m_RotateActionReference.action.Enable();
                m_LookActionReference.action.Enable();
            }
        }

        void OnDisable()
        {
            if (m_RotateActionReference != null && m_LookActionReference != null)
            {
                m_RotateActionReference.action.Disable();
                m_LookActionReference.action.Disable();
            }
        }

        internal void SetTransform(Transform newTransform)
        {
            m_FollowTransform = newTransform;
            SetupProtagonistVirtualCamera();
        }

        void OnRotateStarted(InputAction.CallbackContext context)
        {
            m_IsRotatePressed = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            StartCoroutine(DisableMouseControlForFrame());
        }

        void OnRotateCanceled(InputAction.CallbackContext context)
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
            if (m_LookActionReference.action.activeControl == null)
            {
                return;
            }

            var device = m_LookActionReference.action.activeControl.device;
            switch (device)
            {
                case Mouse:
                    HandleRotateMouse();
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

            var cameraMovement = m_LookActionReference.action.ReadValue<Vector2>();
            var deviceScaling = k_MouseLookMultiplier * Time.deltaTime;
            m_OrbitalFollow.HorizontalAxis.Value += cameraMovement.x * deviceScaling;
            m_OrbitalFollow.VerticalAxis.Value += cameraMovement.y * deviceScaling * k_VerticalScaling;
        }

        void HandleRotateGamepad()
        {
            var cameraMovement = m_LookActionReference.action.ReadValue<Vector2>();
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
