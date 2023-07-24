using StarterAssets;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class CustomClientNetworkTransform : NetworkTransform
{
    private PlatformVisualMover m_CurrentPlatform;
    private PlatformMover m_CurrentPlatformMover;
    private ThirdPersonController m_ThirdPersonController;
    private Transform m_OriginalParent;

    private NetworkVariable<NetworkBehaviourReference> m_AssignedPlatform = new NetworkVariable<NetworkBehaviourReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // This is really to just handle late joining players where everything isn't spawned and the platform parenting
    // does not happen during OnNetworkSpawn (since it is possible that NetworkObjects are still spawning for the late joining player)
    private NetworkVariable<bool> m_IsAssignedPlatform = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private bool m_LateJoinedClientPlatformSynchronize;
    private float m_TickFrequency;

    protected override void Awake()
    {
        base.Awake();
        m_OriginalParent = transform.parent;
    }

    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }

    public override void OnNetworkSpawn()
    {
        m_ThirdPersonController = GetComponent<ThirdPersonController>();
        m_TickFrequency = 1.0f / NetworkManager.NetworkConfig.TickRate;
        base.OnNetworkSpawn();

        if (!CanCommitToTransform)
        {
            m_AssignedPlatform.OnValueChanged += NotifyPlatformParentChanged;
            UpdatePlayerPlatformParenting();

            if (m_IsAssignedPlatform.Value && transform.parent == m_OriginalParent)
            {
                m_LateJoinedClientPlatformSynchronize = true;
            }
        }
    }

    /// <summary>
    /// Updates the RootPlayer's parent based on its parent
    /// </summary>
    /// <param name="worldPoitionStays"></param>
    private void UpdatePlayerPlatformParenting()
    {
        var platformMover = (PlatformMover)null;
        m_AssignedPlatform.Value.TryGet(out platformMover);
        if (transform.parent == m_OriginalParent && platformMover != null)
        {
            m_CurrentPlatformMover = platformMover;
            m_CurrentPlatform = platformMover.PlatformVisualMover;
            transform.SetParent(m_CurrentPlatform.transform, worldPoitionStays);
        }
        else if (transform.parent != m_OriginalParent && platformMover == null)
        {
            m_CurrentPlatformMover = null;
            m_CurrentPlatform = null;
            transform.SetParent(m_OriginalParent, true);
        }
    }

    private void NotifyPlatformParentChanged(NetworkBehaviourReference previous, NetworkBehaviourReference next)
    {
        UpdatePlayerPlatformParenting();
    }

    protected override void Update()
    {
        base.Update();
        if (!IsSpawned || !CanCommitToTransform)
        {
            // A quick handler for late joining players 
            if (IsSpawned && m_LateJoinedClientPlatformSynchronize)
            {
                UpdatePlayerPlatformParenting();
                if (m_IsAssignedPlatform.Value && transform.parent != m_OriginalParent)
                {
                    m_LateJoinedClientPlatformSynchronize = false;
                }
            }
            return;
        }

        if (m_CurrentPlatformMover != null && m_ThirdPersonController != null)
        {
            m_ThirdPersonController.WorldVelocity = (m_TickFrequency * m_CurrentPlatformMover.PlatformSpeed) * m_CurrentPlatformMover.MovementVector.Value;
        }
        else if (m_ThirdPersonController != null)
        {
            m_ThirdPersonController.WorldVelocity = Vector3.zero;
        }

        if (m_CurrentPlatform == null)
        {
            return;
        }
    }

    private Transform GetMoverTransform<T>(Collider other) where T : MonoBehaviour
    {
        var mover = other.gameObject.GetComponent<T>();
        if (mover == null)
        {
            if (other.transform.parent != null)
            {
                mover = other.transform.parent.gameObject.GetComponent<T>();
                if (mover != null)
                {
                    return mover.gameObject.transform;
                }
            }
        }
        else
        {
            return mover.gameObject.transform;
        }
        return null;
    }

    private void HandleEnterVisualMover(Collider other)
    {
        // Exit early for Non-authority
        if (!CanCommitToTransform)
        {
            return;
        }

        var platformVisualMover = other.transform.parent.GetComponent<PlatformVisualMover>();
        // Just quick POC, exit early if the thing triggered doesn't have a PlatformMover component
        // You could use tags to more easily determine collider interest 
        if (platformVisualMover == null)
        {
            return;
        }
        m_CurrentPlatform = platformVisualMover;
        Debug.Log($"Entered Plat: {platformVisualMover.name}");
    }

    private void HandleEnterMover(Collider other)
    {
        // Exit early for Non-authority
        if (!CanCommitToTransform)
        {
            return;
        }
        var moverTransform = GetMoverTransform<PlatformVisualMover>(other);
        if (moverTransform == null)
        {
            return;
        }

        Debug.Log($"[Client-{NetworkObject.OwnerClientId}] Entered Plat: {moverTransform.gameObject.name}");

        if (transform.parent == m_OriginalParent && m_CurrentPlatform != null)
        {
            transform.SetParent(m_CurrentPlatform.transform, true);
            InLocalSpace = true;
            m_CurrentPlatformMover = moverTransform.GetComponent<PlatformVisualMover>().PlatformMover;
            var bRef = new NetworkBehaviourReference(m_CurrentPlatformMover);
            m_AssignedPlatform.Value = bRef;
            m_IsAssignedPlatform.Value = true;
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        HandleEnterVisualMover(other);
        HandleEnterMover(other);
    }

    private void HandleExitVisualMover(Collider other)
    {
        // Exit early for Non-authority
        if (!CanCommitToTransform)
        {
            return;
        }

        var platformVisualMover = other.transform.parent.GetComponent<PlatformVisualMover>();
        // Just quick POC, exit early if the thing triggered doesn't have a PlatformMover component
        // You could use tags to more easily determine collider interest 
        if (platformVisualMover == null)
        {
            return;
        }

        if (platformVisualMover == m_CurrentPlatform)
        {
            m_CurrentPlatform = null;
        }
        Debug.Log($"Exited Platform!");
    }

    private void HandleExitMover(Collider other)
    {
        // Exit early for Non-authority
        if (!CanCommitToTransform)
        {
            return;
        }
        var moverTransform = GetMoverTransform<PlatformVisualMover>(other);
        if (moverTransform == null)
        {
            return;
        }

        Debug.Log($"[Client-{NetworkObject.OwnerClientId}] Exited Plat: {moverTransform.gameObject.name}");

        if (transform.parent != m_OriginalParent)
        {
            transform.SetParent(m_OriginalParent, true);
            InLocalSpace = false;
            m_CurrentPlatformMover = null;
            m_AssignedPlatform.Value = new NetworkBehaviourReference();
            m_IsAssignedPlatform.Value = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        HandleExitVisualMover(other);
        HandleExitMover(other);
    }

}
