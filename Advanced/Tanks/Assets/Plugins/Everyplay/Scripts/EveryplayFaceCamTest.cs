using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class EveryplayFaceCamTest : MonoBehaviour
{
    private bool recordingPermissionGranted = false;
    private GameObject debugMessage = null;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Everyplay.FaceCamRecordingPermission += CheckFaceCamRecordingPermission;
    }

    void Destroy()
    {
        Everyplay.FaceCamRecordingPermission -= CheckFaceCamRecordingPermission;
    }

    private void CheckFaceCamRecordingPermission(bool granted)
    {
        recordingPermissionGranted = granted;

        if (!granted && !debugMessage)
        {
            debugMessage = new GameObject("FaceCamDebugMessage", typeof(Text));
            debugMessage.transform.position = new Vector3(0.5f, 0.5f, 0.0f);

            if (debugMessage != null)
            {
                Text debugMessageGuiText = debugMessage.GetComponent<Text>();

                if (debugMessageGuiText)
                {
                    debugMessageGuiText.text = "Microphone access denied. FaceCam requires access to the microphone.\nPlease enable Microphone access from Settings / Privacy / Microphone.";
                    debugMessageGuiText.alignment = TextAnchor.MiddleCenter;
                }
            }
        }
    }

    void OnGUI()
    {
        if (recordingPermissionGranted)
        {
            if (GUI.Button(new Rect(Screen.width - 10 - 158, 10, 158, 48), Everyplay.FaceCamIsSessionRunning() ? "Stop FaceCam session" : "Start FaceCam session"))
            {
                if (Everyplay.FaceCamIsSessionRunning())
                {
                    Everyplay.FaceCamStopSession();
                }
                else
                {
                    Everyplay.FaceCamStartSession();
                }
                #if UNITY_EDITOR
                Debug.Log("Everyplay FaceCam is not available in the Unity editor. Please compile and run on a device.");
                #endif
            }
        }
        else
        {
            if (GUI.Button(new Rect(Screen.width - 10 - 158, 10, 158, 48), "Request REC permission"))
            {
                Everyplay.FaceCamRequestRecordingPermission();
                #if UNITY_EDITOR
                Debug.Log("Everyplay FaceCam is not available in the Unity editor. Please compile and run on a device.");
                #endif
            }
        }
    }
}
