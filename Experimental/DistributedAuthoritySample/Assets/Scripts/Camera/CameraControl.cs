using com.unity.multiplayer.samples.distributed_authority.gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControl : MonoBehaviour
{
    public Camera mainCamera; // Reference to the main camera
    public float zoomSpeed = 10f;
    public float rotationSpeed = 1f;
    public float minFoV = 15f;
    public float maxFoV = 90f;
    public float minDistance = 2f;
    public float maxDistance = 10f;

    private AvatarActions playerInputActions;
    private AvatarTransform avatarTransform;
    private float distance;
    private bool isRotating = false;

    private void Awake()
    {
        playerInputActions = new AvatarActions();
        playerInputActions.Player.Zoom.performed += OnZoom;
        playerInputActions.Player.Rotate.started += context => isRotating = true;
        playerInputActions.Player.Rotate.canceled += context => isRotating = false;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        playerInputActions.Enable();
    }

    private void OnDisable()
    {
        playerInputActions.Disable();
    }

    private void Update()
    {
        if (isRotating && avatarTransform != null)
        {
            RotateCamera();
        }
        else if (avatarTransform != null)
        {
            UpdateCameraPosition();
        }
    }

    private void OnZoom(InputAction.CallbackContext context)
    {
        if (avatarTransform == null) return;

        float scrollValue = context.ReadValue<float>();

        // Adjust the camera field of view for zooming
        float fov = mainCamera.fieldOfView;
        fov -= scrollValue * zoomSpeed;
        fov = Mathf.Clamp(fov, minFoV, maxFoV);
        mainCamera.fieldOfView = fov;

        // Adjust the distance from the avatar
        distance -= scrollValue * zoomSpeed * Time.deltaTime;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    private void RotateCamera()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float horizontalRotation = mouseDelta.x * rotationSpeed;
        float verticalRotation = -mouseDelta.y * rotationSpeed;

        // Calculate the rotation angles
        mainCamera.transform.RotateAround(avatarTransform.transform.position, Vector3.up, horizontalRotation);
        mainCamera.transform.RotateAround(avatarTransform.transform.position, mainCamera.transform.right, verticalRotation);

        // Ensure the camera maintains the correct distance
        UpdateCameraPosition();
    }

    private void UpdateCameraPosition()
    {
        // Maintain the camera's position behind the avatar using the current rotation and distance
        Vector3 direction = mainCamera.transform.position - avatarTransform.transform.position;
        direction.Normalize();

        mainCamera.transform.position = avatarTransform.transform.position - direction * distance;
    }

    // Method to set the reference to the AvatarTransform
    public void SetAvatarTransform(AvatarTransform newAvatarTransform)
    {
        avatarTransform = newAvatarTransform;
        if (avatarTransform != null)
        {
            // Calculate the initial distance based on the avatar's position
            distance = Vector3.Distance(mainCamera.transform.position, avatarTransform.transform.position);
        }
    }
}




