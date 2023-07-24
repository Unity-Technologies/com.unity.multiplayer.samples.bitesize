using Unity.Netcode;
using UnityEngine;

public class PlatformVisualMover : MonoBehaviour
{
    private bool IsInitialized;

    BufferedLinearInterpolatorVector3 PositionInterpolator = new BufferedLinearInterpolatorVector3();

    BufferedLinearInterpolatorQuaternion RotationInterpolator = new BufferedLinearInterpolatorQuaternion();

    public PlatformMover PlatformMover;

    private NetworkManager m_NetworkManager;
    private float m_TickFrequency;

    public int TicksAgo = 1;

    public void Initialize(Transform transform, NetworkManager networkManager, bool isPlatformOwner)
    {
        m_NetworkManager = networkManager;
        m_TickFrequency = 1.0f / networkManager.NetworkConfig.TickRate;
        PositionInterpolator.ResetTo(transform.position, networkManager.ServerTime.Time);
        RotationInterpolator.ResetTo(transform.rotation, networkManager.ServerTime.Time);
        IsInitialized = true;
        if (isPlatformOwner)
        {
            TicksAgo = 0;
        }
    }

    public void StopFollowing()
    {
        IsInitialized = false;
    }

    public void PushNextPosition(Vector3 position, int tick) 
    {
        PositionInterpolator.AddMeasurement(position, tick * m_TickFrequency);
    }

    public void PushNextRotation(Quaternion rotation, int tick)
    {
        RotationInterpolator.AddMeasurement(rotation, tick * m_TickFrequency);
    }

    private void Update()
    {
        if (!IsInitialized)
        {
            return;
        }

        // Update the visual mover's position interpolator
        PositionInterpolator.Update(Time.deltaTime, m_NetworkManager.ServerTime.TimeTicksAgo(TicksAgo).Time, m_NetworkManager.ServerTime.Time);

        // Apply the current interpolated position
        transform.position = PositionInterpolator.GetInterpolatedValue();

        // Update the visual mover's rotation interpolator
        RotationInterpolator.Update(Time.deltaTime, m_NetworkManager.ServerTime.TimeTicksAgo(TicksAgo).Time, m_NetworkManager.ServerTime.Time);

        // Apply the current interpolated rotation
        transform.rotation = RotationInterpolator.GetInterpolatedValue();

    }
}
