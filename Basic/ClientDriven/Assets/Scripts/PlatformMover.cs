using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;
using Unity.Netcode;

#if UNITY_EDITOR
using UnityEditor;
// This bypases the default custom editor for NetworkTransform
// and lets you modify your custom NetworkTransform's properties
// within the inspector view
[CustomEditor(typeof(PlatformMover), true)]
public class PlatformMoverEditor : Editor
{
}
#endif

public class PlatformMover : NetworkTransform
{
    public PlatformVisualMover PlatformVisualMover;
    public List<Transform> TargetNodes;
    public float PlatformSpeed = 2.0f;

    /// <summary>
    /// This is used to update clients on the current motion vector of the platform. This is needed to offset the player motion by the platform velocity vector
    /// times 2 network ticks period of time (pretty much consider this a default for owner authoritative)
    /// </summary>
    [HideInInspector]
    public NetworkVariable<Vector3> MovementVector = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    private enum PlatformMovingStates
    {
        None,
        Moving,
        WaitingVisual,
        Paused,
    }
    private int m_Index;
    private int m_IndexDirection = 1;
    private PlatformMovingStates m_CurrentMovingState;
    private PlatformMovingStates m_MovingStateBeforePaused;

    public override void OnNetworkSpawn()
    {
        PlatformVisualMover.PlatformMover = this;
        PlatformVisualMover.Initialize(transform, NetworkManager, IsOwner);
        base.OnNetworkSpawn();
        if (CanCommitToTransform)
        {
            // Target the node that proceeds the starting node first
            m_Index++;
            SetNextTargetAndDirection();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (PlatformVisualMover != null)
        {
            PlatformVisualMover.StopFollowing();
        }
        base.OnNetworkDespawn();
    }

    private Vector3 GetFullPosition(ref NetworkTransformState transformState)
    {
        var currentPosition = GetSpaceRelativePosition(true);
        if (transformState.HasPositionChange) 
        {
            var updatedPosition = transformState.GetPosition();
            if (!UseHalfFloatPrecision)
            {
                if (!transformState.HasPositionX)
                {
                    updatedPosition.x = currentPosition.x;
                }
                if (!transformState.HasPositionY)
                {
                    updatedPosition.y = currentPosition.y;
                }
                if (!transformState.HasPositionZ)
                {
                    updatedPosition.z = currentPosition.z;
                }
            }
            currentPosition = updatedPosition;
        }

        return currentPosition;
    }

    private void UpdateVisualMover(ref NetworkTransformState newState)
    {
        // If there is a position change, then apply the delta position change to the visual mover on the non-authority's side
        if (newState.HasPositionChange)
        {
            PlatformVisualMover.PushNextPosition(GetFullPosition(ref newState), newState.GetNetworkTick());
        }

        if (newState.HasRotAngleChange)
        {
            PlatformVisualMover.PushNextRotation(GetSpaceRelativeRotation(), newState.GetNetworkTick());
        }
    }

    protected override void OnAuthorityPushTransformState(ref NetworkTransformState networkTransformState)
    {
        UpdateVisualMover(ref networkTransformState);
        base.OnAuthorityPushTransformState(ref networkTransformState);
    }

    protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
    {
        UpdateVisualMover(ref newState);
        base.OnNetworkTransformStateUpdated(ref oldState, ref newState);
    }

    public Vector3 CurrentDirection { get { return m_CurrentDirection; } }
    private Vector3 m_CurrentDirection;
    private Vector3 m_CurrentForward;
    private Vector3 CurrentTarget;
    private float m_DelayPlatformStateUpdate;

    private void SetNextTargetAndDirection()
    {
        CurrentTarget = TargetNodes[m_Index].position;
        m_CurrentForward = TargetNodes[m_Index].forward;
        m_CurrentDirection = (CurrentTarget - transform.position).normalized;
        // When moving, we keep track of the platforms velocity to help with player
        // motion when on the platform
        if (m_CurrentMovingState == PlatformMovingStates.Moving)
        {
            var movementDirection = m_CurrentDirection * PlatformSpeed;
            MovementVector.Value = movementDirection;
        }
    }

    private void UpdatePlatformState()
    {
        switch(m_CurrentMovingState)
        {
            case PlatformMovingStates.Moving:
                {
                    if (m_DelayPlatformStateUpdate < Time.realtimeSinceStartup)
                    {
                        var distance = Vector3.Distance(transform.position, CurrentTarget);
                        if (distance <= 0.025f)
                        {
                            m_CurrentMovingState = PlatformMovingStates.WaitingVisual;
                        }
                    }
                    break;
                }
            case PlatformMovingStates.WaitingVisual: 
                {
                    // Now, wait for the visual to catch up so it interpolates to the targt position
                    var distance = Vector3.Distance(transform.position, PlatformVisualMover.transform.position);
                    if (distance <= 0.01f)
                    {
                        // Set our state back to moving
                        m_CurrentMovingState = PlatformMovingStates.Moving;
                        // Increment to the next node to move towards
                        m_Index += m_IndexDirection;
                        if (m_Index == TargetNodes.Count || m_Index < 0)
                        {
                            // We are at the end, reverse the path direction
                            m_IndexDirection *= -1;
                            // Set our next target based on the head/tail + 1
                            m_Index += m_IndexDirection * 2;
                        }
                        m_DelayPlatformStateUpdate = Time.realtimeSinceStartup + 0.1f;
                        // Set our target direction (and notify of the platform's velocity change)
                        SetNextTargetAndDirection();                        
                    }
                    break;
                }
        }
    }

    protected override void Update()
    {
        // Move the platform
        MovePlatform();

        // Invoke the base NetworkTransform Update method
        base.Update();
    }

    private void MovePlatform()
    {
        if (!IsSpawned || !CanCommitToTransform)
        {
            return;
        }

        switch(m_CurrentMovingState)
        {
            case PlatformMovingStates.Paused:
            case PlatformMovingStates.None:
                {
                    break;
                }
            case PlatformMovingStates.Moving: 
                {
                    transform.position = Vector3.Lerp(transform.position, transform.position + (m_CurrentDirection * PlatformSpeed), Time.deltaTime);
                    transform.forward = Vector3.Lerp(transform.forward, m_CurrentForward, Time.deltaTime * 3.0f);                    
                    UpdatePlatformState();                    
                    break;
                }
            case PlatformMovingStates.WaitingVisual: 
                {
                    UpdatePlatformState();
                    break;
                }
        }
    }

    private void LateUpdate()
    {
        if (!CanCommitToTransform) { return; }

        if (Input.GetKeyDown(KeyCode.P))
        {
            switch(m_CurrentMovingState)
            {
                case PlatformMovingStates.None:
                    {
                        m_CurrentMovingState = PlatformMovingStates.Moving;
                        // Set our target direction (and notify of the platform's velocity change)
                        SetNextTargetAndDirection();
                        break;
                    }
                case PlatformMovingStates.Moving:
                case PlatformMovingStates.WaitingVisual:
                    {
                        m_MovingStateBeforePaused = m_CurrentMovingState;
                        m_CurrentMovingState = PlatformMovingStates.Paused;
                        MovementVector.Value = Vector3.zero;                        
                        break;
                    }
                case PlatformMovingStates.Paused:
                    {
                        SetNextTargetAndDirection();
                        m_CurrentMovingState = m_MovingStateBeforePaused;
                        break;
                    }
            }
        }
    }
}
