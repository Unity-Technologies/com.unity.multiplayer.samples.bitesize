using System.Collections;
using com.unity.multiplayer.samples.distributed_authority.gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using Unity.Netcode;

public class CameraControl : MonoBehaviour
{
    public Camera mainCamera; // Reference to the main camera
    public CinemachineFreeLook freeLookVCam; // Reference to the Cinemachine FreeLook virtual camera
    public float _speedMultiplier = 1.0f; // Speed multiplier for camera movement

    private AvatarActions playerInputActions;
    private AvatarTransform avatarTransform; // This will be assigned at runtime
    private float distance;
    private bool isRotating = false;
    private bool _cameraMovementLock = false;
    private bool _isRMBPressed = false; // To track the right mouse button press

    private void Awake()
    {
        playerInputActions = new AvatarActions();

        // Setup input event handlers
        playerInputActions.Player.Rotate.started += OnRotateStarted;
        playerInputActions.Player.Rotate.canceled += OnRotateCanceled;
        playerInputActions.Player.Look.performed += OnLookPerformed;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Register the client connected callback
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnEnable()
    {
        playerInputActions.Enable();
    }

    private void OnDisable()
    {
        playerInputActions.Disable();

        // Unregister the client connected callback
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            OnAvatarSpawned(clientId);
        }
    }

    private void OnAvatarSpawned(ulong clientId)
    {
        Debug.Log("Avatar spawned for client: " + clientId);
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Find the AvatarTransform component on the locally controlled avatar
            GameObject avatar = FindLocalPlayerAvatar();
            if (avatar != null)
            {
                avatarTransform = avatar.GetComponent<AvatarTransform>();
                if (avatarTransform != null)
                {
                    // Calculate the initial distance based on the avatar's position
                    distance = Vector3.Distance(mainCamera.transform.position, avatarTransform.transform.position);
                    UpdateCameraPosition(); // Update camera position to look at the new avatar
                    SetupProtagonistVirtualCamera(); // Setup the virtual camera

                    // Unregister the avatar spawned callback to avoid redundancy
                    NetworkManager.Singleton.OnClientConnectedCallback -= OnAvatarSpawned;
                }
            }
        }
    }

    private GameObject FindLocalPlayerAvatar()
    {
        // Implement your logic to find the local player's avatar
        // This could be based on tags, layers, or specific naming conventions
        // Here is an example where avatars are tagged as "Player"

        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in players)
        {
            // Check if this player belongs to the local client
            // You can add more conditions if required based on your networking setup
            if (player.GetComponent<NetworkObject>().IsLocalPlayer)
            {
                return player;
            }
        }
        return null;
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

        // Clear input values
        freeLookVCam.m_XAxis.m_InputAxisValue = 0;
        freeLookVCam.m_YAxis.m_InputAxisValue = 0;
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

        freeLookVCam.m_XAxis.m_InputAxisValue = cameraMovement.x * deviceMultiplier * _speedMultiplier;
        freeLookVCam.m_YAxis.m_InputAxisValue = cameraMovement.y * deviceMultiplier * _speedMultiplier;
    }

    public void SetupProtagonistVirtualCamera()
    {
            Transform target = avatarTransform.transform;
            freeLookVCam.Follow = target;
            freeLookVCam.LookAt = target;
            freeLookVCam.OnTargetObjectWarped(target, target.position - freeLookVCam.transform.position);
    }

    private void UpdateCameraPosition()
    {
        if (avatarTransform == null) return;

        Vector3 direction = mainCamera.transform.position - avatarTransform.transform.position;
        direction.Normalize(); // Normalize the direction vector to ensure proper positioning

        mainCamera.transform.position = avatarTransform.transform.position - direction * distance;
        mainCamera.transform.LookAt(avatarTransform.transform); // Ensure the camera looks at the avatar
    }
}

