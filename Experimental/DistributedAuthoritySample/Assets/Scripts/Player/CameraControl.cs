using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    public class CameraControl : MonoBehaviour
    {
        public CinemachineCamera freeLookVCam;
        public InputActionReference rotateActionReference;
        public InputActionReference lookActionReference;

        public float _speedMultiplier = 1.0f; // Speed multiplier for camera movement
        private AvatarTransform avatarTransform; // This will be assigned at runtime
        private bool _cameraMovementLock = false;
        private bool _isRMBPressed = false;

        private CinemachineOrbitalFollow _orbitalFollow;

        private void Awake()
        {
            // Setup input event handlers
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

            // Grab CM's orbital follow because we will be driving its axes directly
            _orbitalFollow = freeLookVCam.GetComponent<CinemachineOrbitalFollow>();

            // Ensure the camera is not tracking anything initially
            freeLookVCam.Follow = null;
        }

        private void OnEnable()
        {
            rotateActionReference?.action.Enable();
            lookActionReference?.action.Enable();
        }

        private void OnDisable()
        {
            rotateActionReference?.action.Disable();
            lookActionReference?.action.Disable();
        }

        public void SetAvatarTransform(AvatarTransform newAvatarTransform)
        {
            avatarTransform = newAvatarTransform;
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

            // Drive the camera's orbit position based on user input
            float deviceMultiplier = isDeviceMouse ? 0.02f : Time.deltaTime;
            _orbitalFollow.HorizontalAxis.Value += cameraMovement.x * deviceMultiplier * _speedMultiplier;
            _orbitalFollow.VerticalAxis.Value += cameraMovement.y * deviceMultiplier * _speedMultiplier * 0.01f; // Y axis units are much smaller
        }

        public void SetupProtagonistVirtualCamera()
        {
            if (avatarTransform == null)
                return;

            Transform target = avatarTransform.transform;
            freeLookVCam.Follow = target;
            CinemachineCore.ResetCameraState(); // snap to new position
        }
    }
}
