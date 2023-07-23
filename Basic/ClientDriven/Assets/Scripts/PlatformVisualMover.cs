using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class PlatformVisualMover : MonoBehaviour
{
    private bool IsInitialized;

    BufferedLinearInterpolatorVector3 PositionInterpolator = new BufferedLinearInterpolatorVector3();

    public PlatformMover PlatformMover;

    private NetworkManager m_NetworkManager;
    private float m_TickFrequency;

    public int TicksAgo = 1;

    public void Initialize(Vector3 position, NetworkManager networkManager, bool isPlatformOwner)
    {
        m_NetworkManager = networkManager;
        m_TickFrequency = 1.0f / networkManager.NetworkConfig.TickRate;
        PositionInterpolator.ResetTo(position, networkManager.ServerTime.Time);
        IsInitialized = true;
        if (isPlatformOwner)
        {
            TicksAgo = 0;
        }
    }

    private Vector3 m_NextPosition;
    public void PushNextPosition(Vector3 position, int tick) 
    {
        PositionInterpolator.AddMeasurement(position, tick * m_TickFrequency);
    }

    private void Update()
    {
        if (!IsInitialized)
        {
            return;
        }

        //var ticksAgo = m_NetworkManager.IsServer ? TicksAgo * -1 : TicksAgo;

        var ticksAgo = TicksAgo;

        // Update the visual 1 network tick behind the current known time
        var newPosition  = PositionInterpolator.Update(Time.deltaTime, m_NetworkManager.ServerTime.TimeTicksAgo(TicksAgo).Time, m_NetworkManager.ServerTime.Time);

        // Apply the interpolated position
        //var newPosition = PositionInterpolator.GetInterpolatedValue();
        newPosition.y = transform.position.y;
        transform.position = newPosition;
    }    
}
