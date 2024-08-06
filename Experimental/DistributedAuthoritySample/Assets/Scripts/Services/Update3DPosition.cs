using Services;
using Unity.Services.Vivox;
using UnityEngine;
using Unity.Netcode;

public class Update3DPosition : NetworkBehaviour
{
    private float m_NextPosUpdate;
    private string m_ChannelName;

    void Start()
    {
        while (VivoxManager.Instance == null) {};

        m_ChannelName = VivoxManager.Instance.SessionName;
        m_NextPosUpdate = Time.time + 3.0f; // wait 3 seconds to give a chance for player to join channel
    }

    void Update()
    {
        if (IsOwner)
        {
            if (Time.time > m_NextPosUpdate)
            {
                VivoxService.Instance.Set3DPosition(gameObject, m_ChannelName);
                m_NextPosUpdate += 0.3f; // update position every 0.3s
            }
        }

    }
}
