using System;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;

public class AudioIOController : NetworkBehaviour
{
    private Boolean InputMuted, OutputMuted;

    void Start()
    {
        InputMuted = false;
        OutputMuted = false;
    }

    void Update()
    {
        if (IsOwner)
        {
            // Toggle mute input device
            if (Input.GetKeyDown(KeyCode.I))
            {
                if (InputMuted)
                {
                    VivoxService.Instance.UnmuteInputDevice();
                    Debug.Log("Input Unmuted");
                }
                else
                {
                    VivoxService.Instance.MuteInputDevice();
                    Debug.Log("Input Muted");
                }

                InputMuted = !InputMuted;
            }

            // Toggle mute output device
            if (Input.GetKeyDown(KeyCode.O))
            {
                if (OutputMuted)
                {
                    VivoxService.Instance.UnmuteOutputDevice();
                    Debug.Log("Output Unmuted");
                }
                else
                {
                    VivoxService.Instance.MuteOutputDevice();
                    Debug.Log("Output Muted");
                }

                OutputMuted = !OutputMuted;
            }
        }
    }
}
