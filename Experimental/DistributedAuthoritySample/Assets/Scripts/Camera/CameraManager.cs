using System;
using UnityEngine;
using Cinemachine;
using System.Collections;

public class CameraManager : MonoBehaviour
{
    public InputReader inputReader;
    public Camera mainCamera;
    public CinemachineFreeLook freeLookVCam;
    public CinemachineImpulseSource impulseSource;

    [SerializeField][Range(.5f, 3f)] private float speedMultiplier = 1f;
    [SerializeField] private TransformAnchor cameraTransformAnchor = default;
    [SerializeField] private TransformAnchor protagonistTransformAnchor = default;

    [Header("Listening on channels")]
    [Tooltip("The CameraManager listens to this event, fired by protagonist GettingHit state, to shake the camera")]
    [SerializeField] private VoidEventChannelSO camShakeEvent = default;

    private bool isRMBPressed;
    private bool cameraMovementLock = false;

    private void OnEnable()
    {
        inputReader.CameraMoveEvent += OnCameraMove;
        inputReader.EnableMouseControlCameraEvent += OnEnableMouseControlCamera;
        inputReader.DisableMouseControlCameraEvent += OnDisableMouseControlCamera;

        protagonistTransformAnchor.OnAnchorProvided += SetupProtagonistVirtualCamera;
        camShakeEvent.OnEventRaised += impulseSource.GenerateImpulse;

        cameraTransformAnchor.Provide(mainCamera.transform);
    }

    private void OnDisable()
    {
        inputReader.CameraMoveEvent -= OnCameraMove;
        inputReader.EnableMouseControlCameraEvent -= OnEnableMouseControlCamera;
        inputReader.DisableMouseControlCameraEvent -= OnDisableMouseControlCamera;

        protagonistTransformAnchor.OnAnchorProvided -= SetupProtagonistVirtualCamera;
        camShakeEvent.OnEventRaised -= impulseSource.GenerateImpulse;

        cameraTransformAnchor.Unset();
    }

    private void Start()
    {
        if (protagonistTransformAnchor.isSet)
            SetupProtagonistVirtualCamera();
    }

    private void OnEnableMouseControlCamera()
    {
        isRMBPressed = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        StartCoroutine(DisableMouseControlForFrame());
    }

    IEnumerator DisableMouseControlForFrame()
    {
        cameraMovementLock = true;
        yield return new WaitForEndOfFrame();
        cameraMovementLock = false;
    }

    private void OnDisableMouseControlCamera()
    {
        isRMBPressed = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        freeLookVCam.m_XAxis.m_InputAxisValue = 0;
        freeLookVCam.m_YAxis.m_InputAxisValue = 0;
    }

    private void OnCameraMove(Vector2 cameraMovement, bool isDeviceMouse)
    {
        if (cameraMovementLock || (isDeviceMouse && !isRMBPressed))
            return;

        float deviceMultiplier = isDeviceMouse ? 0.02f : Time.deltaTime;
        freeLookVCam.m_XAxis.m_InputAxisValue = cameraMovement.x * deviceMultiplier * speedMultiplier;
        freeLookVCam.m_YAxis.m_InputAxisValue = cameraMovement.y * deviceMultiplier * speedMultiplier;
    }

    public void SetupProtagonistVirtualCamera()
    {
        Transform target = protagonistTransformAnchor.Value;
        freeLookVCam.Follow = target;
        freeLookVCam.LookAt = target;
    }

    private void LateUpdate()
    {
        if (freeLookVCam.Follow == null && protagonistTransformAnchor.isSet)
        {
            SetupProtagonistVirtualCamera();
        }
    }
}

