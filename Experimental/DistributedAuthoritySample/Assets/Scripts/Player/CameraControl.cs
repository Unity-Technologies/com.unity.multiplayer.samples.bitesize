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
        [SerializeField]
        internal float SpeedMultiplier = 1.0f; // Speed multiplier for camera movement

        Transform m_FollowTransform;
        bool m_CameraMovementLock;
        bool m_IsRMBPressed;
        CinemachineOrbitalFollow m_OrbitalFollow;

        void Awake()
        {
            if (m_RotateActionReference != null)
            {
                m_RotateActionReference.action.started += OnRotateStarted;
                m_RotateActionReference.action.canceled += OnRotateCanceled;
                m_RotateActionReference.action.Enable();
            }

            if (m_LookActionReference != null)
            {
                m_LookActionReference.action.performed += OnLookPerformed;
                m_LookActionReference.action.Enable();
            }

            m_OrbitalFollow = m_FreeLookVCamera.GetComponent<CinemachineOrbitalFollow>();
            m_FreeLookVCamera.Follow = null;
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
            m_IsRMBPressed = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            StartCoroutine(DisableMouseControlForFrame());
        }

        void OnRotateCanceled(InputAction.CallbackContext context)
        {
            m_IsRMBPressed = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        IEnumerator DisableMouseControlForFrame()
        {
            m_CameraMovementLock = true;
            yield return new WaitForEndOfFrame();
            m_CameraMovementLock = false;
        }

        void OnLookPerformed(InputAction.CallbackContext context)
        {
            Vector2 cameraMovement = context.ReadValue<Vector2>();
            bool isDeviceMouse = context.control.device == Mouse.current;

            if (m_CameraMovementLock)
                return;

            if (isDeviceMouse && !m_IsRMBPressed)
                return;

            float deviceMultiplier = isDeviceMouse ? 0.02f : Time.deltaTime;
            m_OrbitalFollow.HorizontalAxis.Value += cameraMovement.x * deviceMultiplier * SpeedMultiplier;
            m_OrbitalFollow.VerticalAxis.Value += cameraMovement.y * deviceMultiplier * SpeedMultiplier * 0.01f;
        }

        void SetupProtagonistVirtualCamera()
        {
            if (m_FollowTransform == null)
                return;

            m_FreeLookVCamera.Follow = m_FollowTransform;
            CinemachineCore.ResetCameraState(); // snap to new position
        }
    }
}
