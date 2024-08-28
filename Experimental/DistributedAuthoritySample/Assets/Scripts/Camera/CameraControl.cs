using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using Unity.Multiplayer.Samples.SocialHub.Player;
using Unity.Netcode;

public class CameraControl : MonoBehaviour
{
    public CinemachineCamera freeLookVCam;

    public float _speedMultiplier = 1.0f; // Speed multiplier for camera movement

    private AvatarActions playerInputActions;
    private AvatarTransform avatarTransform; // This will be assigned at runtime
    private bool _cameraMovementLock = false;
    private bool _isRMBPressed = false;

    private CinemachineOrbitalFollow _orbitalFollow;

    private void Awake()
    {
        playerInputActions = new AvatarActions();

        // Setup input event handlers
        playerInputActions.Player.Rotate.started += OnRotateStarted;
        playerInputActions.Player.Rotate.canceled += OnRotateCanceled;
        playerInputActions.Player.Look.performed += OnLookPerformed;

        // Grab CM's orbital follow because we will be driving its axes directly
        _orbitalFollow = freeLookVCam.GetComponent<CinemachineOrbitalFollow>();

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
        Transform target = avatarTransform.transform;
        freeLookVCam.Follow = target;
        CinemachineCore.ResetCameraState(); // snap to new position
    }
}

