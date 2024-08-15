using UnityEngine;

public class CustomCameraController : MonoBehaviour
{
    private Transform playerTransform;

    [Header("Camera Settings")]
    public float defaultDistance = 5f;
    public float minDistance = 2f;
    public float maxDistance = 10f;
    public float distanceStep = 1f;
    public float rotationSpeed = 3f;
    public float scrollSensitivity = 2f;

    private float currentDistance;
    private float currentX = 0f;
    private float currentY = 0f;
    private bool isRightMousePressed = false;

    private void Start()
    {
        currentDistance = defaultDistance;
    }

    public void SetPlayerTransform(Transform newPlayerTransform)
    {
        playerTransform = newPlayerTransform;
        // Initialize the camera rotation based on player orientation
        currentX = playerTransform.eulerAngles.y;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isRightMousePressed = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Input.GetMouseButtonUp(1))
        {
            isRightMousePressed = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (isRightMousePressed)
        {
            currentX += Input.GetAxis("Mouse X") * rotationSpeed;
            currentY -= Input.GetAxis("Mouse Y") * rotationSpeed;
            currentY = Mathf.Clamp(currentY, -30f, 60f);
        }

        if (Input.mouseScrollDelta.y != 0)
        {
            currentDistance -= Input.mouseScrollDelta.y * scrollSensitivity;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        Vector3 direction = new Vector3(0, 0, -currentDistance);
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 position = playerTransform.position + rotation * direction;

        transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * 10f);
        transform.LookAt(playerTransform.position + Vector3.up * 1.5f); // Adjust the Y offset for slightly looking down at the player
    }
}

