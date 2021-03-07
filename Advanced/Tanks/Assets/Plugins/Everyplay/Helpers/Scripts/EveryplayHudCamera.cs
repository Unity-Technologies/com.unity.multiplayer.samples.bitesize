#if !UNITY_EDITOR

#if (UNITY_ANDROID && EVERYPLAY_ANDROID)
#if !(UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5)
#define EVERYPLAY_NATIVE_PLUGIN
#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1)
#define EVERYPLAY_NATIVE_PLUGIN_USE_PTR
#endif
#endif

#elif (UNITY_IPHONE && EVERYPLAY_IPHONE)
#if !(UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1)
#define EVERYPLAY_NATIVE_PLUGIN
#define EVERYPLAY_NATIVE_PLUGIN_USE_PTR
#endif
#endif

#endif

using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class EveryplayHudCamera : MonoBehaviour
{
    private const int EPSR = 0x45505352;
    private bool subscribed = false;
    private bool readyForRecording = false;
    #if EVERYPLAY_NATIVE_PLUGIN
    private IntPtr renderEventPtr = IntPtr.Zero;
    private bool isMetalDevice = false;
    private bool isAndroidDevice = false;
    #endif

    void Awake()
    {
        Subscribe(true);
        readyForRecording = Everyplay.IsReadyForRecording();
        #if EVERYPLAY_NATIVE_PLUGIN
        if (readyForRecording)
        {
            renderEventPtr = EveryplayGetUnityRenderEventPtr();
        }
        isMetalDevice = SystemInfo.graphicsDeviceVersion.Contains("Metal") && !SystemInfo.graphicsDeviceVersion.Contains("OpenGL");
        isAndroidDevice = Application.platform == RuntimePlatform.Android;
        #endif
    }

    void OnDestroy()
    {
        Subscribe(false);
    }

    void OnEnable()
    {
        Subscribe(true);
    }

    void OnDisable()
    {
        Subscribe(false);
    }

    void Subscribe(bool subscribe)
    {
        if (!subscribed && subscribe)
        {
            Everyplay.ReadyForRecording += ReadyForRecording;
        }
        else if (subscribed && !subscribe)
        {
            Everyplay.ReadyForRecording -= ReadyForRecording;
        }
        subscribed = subscribe;
    }

    void ReadyForRecording(bool ready)
    {
        #if EVERYPLAY_NATIVE_PLUGIN
        if (ready && renderEventPtr == IntPtr.Zero)
        {
            renderEventPtr = EveryplayGetUnityRenderEventPtr();
        }
        #endif
        readyForRecording = ready;
    }

    void OnPreRender()
    {
        if (readyForRecording)
        {
            #if EVERYPLAY_NATIVE_PLUGIN
            if (renderEventPtr != IntPtr.Zero)
            {
                if (isMetalDevice || isAndroidDevice)
                {
                    #if EVERYPLAY_NATIVE_PLUGIN_USE_PTR
                    GL.IssuePluginEvent(renderEventPtr, EPSR);
                    #else
                    GL.IssuePluginEvent(EPSR);
                    #endif
                }
                else
                {
                    Everyplay.SnapshotRenderbuffer();
                }
            }
            #else
            Everyplay.SnapshotRenderbuffer();
            #endif
        }
    }

    #if EVERYPLAY_NATIVE_PLUGIN
    #if UNITY_ANDROID
    [DllImport("everyplay")]
    #else
    [DllImport("__Internal")]
    #endif
    private static extern IntPtr EveryplayGetUnityRenderEventPtr();
    #endif
}
