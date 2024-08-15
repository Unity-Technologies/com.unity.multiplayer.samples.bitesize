using UnityEngine;

public class CustomCameraManager : MonoBehaviour
{
    private CustomCameraController customCameraController;

    private void Awake()
    {
        customCameraController = GetComponent<CustomCameraController>();
        if (customCameraController == null)
        {
            Debug.LogError("CustomCameraController is not attached to the CameraManager.");
        }
    }

    public void SetPlayerTransform(Transform playerTransform)
    {
        if (customCameraController != null)
        {
            customCameraController.SetPlayerTransform(playerTransform);
        }
    }
}

