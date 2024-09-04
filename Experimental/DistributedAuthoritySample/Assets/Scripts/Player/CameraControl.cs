using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    public class CameraControl : MonoBehaviour
    {
        [SerializeField]
        CinemachineCamera freeLookVCam;
        [SerializeField]
        InputActionReference rotateActionReference;
        [SerializeField]
        InputActionReference lookActionReference;
        [SerializeField]
        float _speedMultiplier = 1.0f; // Speed multiplier for camera movement

        private Transform m_FollowTransform;
        private bool _cameraMovementLock = false;
        private bool _isRMBPressed = false;
        private CinemachineOrbitalFollow _orbitalFollow;

        private void Awake()
        {
            if (rotateActionReference != null)
            {
                rotateActionReference.action.started += OnRotateStarted;
                rotateActionReference.action.canceled += OnRotateCanceled;
                rotateActionReference.action.Enable();
            }

            if (lookActionReference != null)
            {
                lookActionReference.action.performed += OnLookPerformed;
                lookActionReference.action.Enable();
            }

            _orbitalFollow = freeLookVCam.GetComponent<CinemachineOrbitalFollow>();
            freeLookVCam.Follow = null;
        }

        private void OnEnable()
        {
            if (rotateActionReference != null && lookActionReference != null)
            {
                rotateActionReference?.action.Enable();
                lookActionReference?.action.Enable();
            }
        }

        private void OnDisable()
        {
            rotateActionReference?.action.Disable();
            lookActionReference?.action.Disable();
        }

        internal void SetTransform(Transform newTransform)
        {
            m_FollowTransform = newTransform;

            if (m_FollowTransform != null)
            {
                Camera camera = Camera.main;
                if (camera != null)
                {
                    AvatarTransform.SetCameraReference(camera);
                }
                else
                {
                    Debug.LogWarning("Main Camera not found. Ensure there is a camera tagged as 'Main Camera' in the scene.");
                }
            }

            SetupProtagonistVirtualCamera();
        }

        private void OnRotateStarted(InputAction.CallbackContext context)
        {
            _isRMBPressed = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            StartCoroutine(DisableMouseControlForFrame());
        }

        private void OnRotateCanceled(InputAction.CallbackContext context)
        {
            _isRMBPressed = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private IEnumerator DisableMouseControlForFrame()
        {
            _cameraMovementLock = true;
            yield return new WaitForEndOfFrame();
            _cameraMovementLock = false;
        }

        private void OnLookPerformed(InputAction.CallbackContext context)
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
