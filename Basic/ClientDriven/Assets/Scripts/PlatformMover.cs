using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem.LowLevel;

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
    public Transform TargetNode;
    private List<Vector3> PathPositions;
    private int m_Index;

    public int NonAuthorityTicksAgo = 3;

    public int AuthorityTicksAgo = 3;

    public int VisualTicksAgo = 6;

    public bool UseSeparateVisual = false;

    private NetworkVariable<int> m_NonAuthorityTicksAgo = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> m_AuthorityTicksAgo = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> m_VisualTicksAgo = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


    public float PlatformSpeed = 2.0f;

    [Tooltip("When true, the platform starts moving when the NetworkObject is spawned.")]
    public bool IsMoving = false;

    public override void OnNetworkSpawn()
    {
        PlatformVisualMover.PlatformMover = this;
        PlatformVisualMover.Initialize(transform.position, NetworkManager, IsOwner);
        base.OnNetworkSpawn();
        if (CanCommitToTransform)
        {
            PathPositions = new List<Vector3>()
            {
                TargetNode.transform.position,
                transform.position,
            };
            m_NonAuthorityTicksAgo.Value = NonAuthorityTicksAgo;
            m_AuthorityTicksAgo.Value = AuthorityTicksAgo;
            m_VisualTicksAgo.Value = VisualTicksAgo;
        }
        else
        {
            m_VisualTicksAgo.OnValueChanged += VisualTicksAgoChanged;
        }
    }

    private void VisualTicksAgoChanged(int previous, int next)
    {
        if (PlatformVisualMover != null)
        {
            PlatformVisualMover.TicksAgo = next;
        }
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

    protected override void OnInitialize(ref NetworkTransformState replicatedState)
    {
        
        base.OnInitialize(ref replicatedState);
        if (!CanCommitToTransform)
        {
            if (UseSeparateVisual)
            {
                var currentState = replicatedState;
                PlatformVisualMover.PushNextPosition(GetFullPosition(ref currentState), replicatedState.GetNetworkTick());
            }
        }
        else
        {
            // If usng a separate visual object, then we don't need to interpolate on non-authority instances since the
            // visual mover will be interpolating anyway.
            Interpolate = !UseSeparateVisual;
        }
    }

    protected override void OnAuthorityPushTransformState(ref NetworkTransformState networkTransformState)
    {
        if (UseSeparateVisual)
        {
            var ticksAgo = IsOwner ? 0 : m_AuthorityTicksAgo.Value;
            PlatformVisualMover.PushNextPosition(transform.position, networkTransformState.GetNetworkTick() + ticksAgo);
        }
        base.OnAuthorityPushTransformState(ref networkTransformState);


    }

    protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
    {
        if(UseSeparateVisual && newState.HasPositionChange)         
        {
            var ticksAgo = IsOwner ? 0 : m_AuthorityTicksAgo.Value;
            PlatformVisualMover.PushNextPosition(GetFullPosition(ref newState), newState.GetNetworkTick() + ticksAgo);
        }
        base.OnNetworkTransformStateUpdated(ref oldState, ref newState);
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !CanCommitToTransform)
        {
            return;
        }


        if (!IsMoving)
        {
            return;
        }

        var targetPosition = PathPositions[m_Index];
        var distance = Vector3.Distance(transform.position, targetPosition);
        if (distance <= 0.01f)
        {
            m_Index++;
            m_Index = m_Index % PathPositions.Count;
            targetPosition = PathPositions[m_Index];
        }

        var direction = (targetPosition - transform.position).normalized;
        direction.y = 0.0f;

        transform.position = Vector3.Lerp(transform.position, transform.position + (direction * PlatformSpeed), Time.fixedDeltaTime);        
    }

    private void LateUpdate()
    {
        if (!CanCommitToTransform) { return; }


        if (m_NonAuthorityTicksAgo.Value != NonAuthorityTicksAgo)
        {
            m_NonAuthorityTicksAgo.Value = NonAuthorityTicksAgo;
        }

        if (m_AuthorityTicksAgo.Value != AuthorityTicksAgo)
        {
            m_AuthorityTicksAgo.Value = AuthorityTicksAgo;
        }

        if (m_VisualTicksAgo.Value != VisualTicksAgo) 
        {
            m_VisualTicksAgo.Value = VisualTicksAgo;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            IsMoving = !IsMoving;
        }
    }
}
