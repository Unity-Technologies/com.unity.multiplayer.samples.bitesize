using System;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.InputSystem;

public class AudioIOController : NetworkBehaviour
{
    private Boolean InputMuted, OutputMuted;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InputMuted = false;
        OutputMuted = false;
    }

    // Update is called once per frame
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
                }
                else
                {
                    VivoxService.Instance.MuteInputDevice();
                }

                InputMuted = !InputMuted;
            }

            // Toggle mute output device
            if (Input.GetKeyDown(KeyCode.O))
            {
                if (OutputMuted)
                {
                    VivoxService.Instance.UnmuteOutputDevice();
                }
                else
                {
                    VivoxService.Instance.MuteOutputDevice();
                }

                OutputMuted = !OutputMuted;
            }
        }
    }
}
