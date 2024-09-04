using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    class CameraControl : MonoBehaviour
    {
        [SerializeField]
        CinemachineCamera freeLookVCam;
        [SerializeField]
        InputActionReference m_RotateActionReference;
        [SerializeField]
        InputActionReference m_LookActionReference;
        [SerializeField]
        float _speedMultiplier = 1.0f; // Speed multiplier for camera movement

        Transform m_FollowTransform;
        bool _cameraMovementLock = false;
        bool _isRMBPressed = false;
        CinemachineOrbitalFollow _orbitalFollow;

        private void Awake()
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

            _orbitalFollow = freeLookVCam.GetComponent<CinemachineOrbitalFollow>();
            freeLookVCam.Follow = null;
        }

        private void OnEnable()
        {
            if (m_RotateActionReference != null && m_LookActionReference != null)
            {
                m_RotateActionReference.action.Enable();
                m_LookActionReference.action.Enable();
            }
        }

        private void OnDisable()
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
            _isRMBPressed = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            StartCoroutine(DisableMouseControlForFrame());
        }

        void OnRotateCanceled(InputAction.CallbackContext context)
        {
            _isRMBPressed = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        IEnumerator DisableMouseControlForFrame()
        {
            _cameraMovementLock = true;
            yield return new WaitForEndOfFrame();
            _cameraMovementLock = false;
        }

        void OnLookPerformed(InputAction.CallbackContext context)
        {
            Vector2 cameraMovement = context.ReadValue<Vector2>();
            bool isDeviceMouse = context.control.device == Mouse.current;

            if (_cameraMovementLock)
                return;

            if (isDeviceMouse && !_isRMBPressed)
                return;

            float deviceMultiplier = isDeviceMouse ? 0.02f : Time.deltaTime;
            _orbitalFollow.HorizontalAxis.Value += cameraMovement.x * deviceMultiplier * _speedMultiplier;
            _orbitalFollow.VerticalAxis.Value += cameraMovement.y * deviceMultiplier * _speedMultiplier * 0.01f;
        }

        void SetupProtagonistVirtualCamera()
        {
            if (m_FollowTransform == null)
                return;

            freeLookVCam.Follow = m_FollowTransform;
            CinemachineCore.ResetCameraState(); // snap to new position
        }
    }
}
