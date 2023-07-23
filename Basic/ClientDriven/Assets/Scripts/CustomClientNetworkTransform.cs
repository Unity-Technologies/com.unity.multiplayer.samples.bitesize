using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
/// <summary>
/// The <see cref="CustomClientNetworkTransformEditor"/> for <see cref="CustomClientNetworkTransform"/>
/// </summary>
[CustomEditor(typeof(CustomClientNetworkTransform), true)]
public class CustomClientNetworkTransformEditor : UnityEditor.Editor
{

}
#endif

public class CustomClientNetworkTransform : NetworkTransform
{
    public bool UseVisualMover;
    public bool ParentUnderVisualMover;


    private PlatformVisualMover m_CurrentPlatform;

    private Vector3 m_LastPosition;

    private Transform m_OriginalParent;

    private NetworkVariable<NetworkBehaviourReference> m_AssignedPlatform = new NetworkVariable<NetworkBehaviourReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


    protected override void Awake()
    {
        if (!UseVisualMover)
        {
            ParentUnderVisualMover = false;
        }
        base.Awake();
        if ((UseVisualMover && ParentUnderVisualMover) || !UseVisualMover)
        {
            m_OriginalParent = transform.parent;
        }        
    }

    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!CanCommitToTransform)
        {
            var capsuleCollider = GetComponentInChildren<CapsuleCollider>();
            capsuleCollider.isTrigger = true;
            m_AssignedPlatform.OnValueChanged += NotifyPlatformParentChanged;
        }

    }

    private void NotifyPlatformParentChanged(NetworkBehaviourReference previous, NetworkBehaviourReference next)
    {
        if (transform.parent == m_OriginalParent && next.TryGet(out PlatformMover platformMover))
        {
            transform.SetParent(platformMover.PlatformVisualMover.transform, false);

            //NotifyPlatformParentChangedClientRpc(next);
        }
    }

    //[ClientRpc]
    //private void NotifyPlatformParentChangedClientRpc(NetworkBehaviourReference next)
    //{
    //    if (!IsServer && transform.parent == m_OriginalParent && next.TryGet(out PlatformMover platformMover))
    //    {
    //        transform.SetParent(platformMover.PlatformVisualMover.transform, false);
    //        if (IsOwner)
    //        {
    //            InLocalSpace = true;
    //        }
    //    }
    //}

    protected override void Update()
    {
        base.Update();
        if (!IsSpawned && !CanCommitToTransform)
        {
            return;
        }

        if (m_CurrentPlatform == null)
        {
            return;
        }

    }

    private void LateUpdate()
    {
        if (!IsSpawned || !CanCommitToTransform || !UseVisualMover)
        {
            return;
        }

        if (UseVisualMover && !ParentUnderVisualMover)
        {
            if (m_CurrentPlatform == null)
            {
                return;
            }

            var delta = m_CurrentPlatform.transform.position - m_LastPosition;
            transform.position = transform.position + delta;
            m_LastPosition = m_CurrentPlatform.transform.position;
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


    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        if (!IsOwner)
        {
            return;
        }

        if (parentNetworkObject != null)
        {
            InLocalSpace = true;
            m_LastPosition = parentNetworkObject.transform.position;
        }
        else if (parentNetworkObject == null)
        {
            InLocalSpace = false;
        }
        base.OnNetworkObjectParentChanged(parentNetworkObject);
    }


    private void HandleEnterVisualMover(Collider other)
    {
        // Exit early for Non-owners
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

        m_LastPosition = m_CurrentPlatform.transform.position;
        Debug.Log($"Entered Plat: {platformVisualMover.name}");
    }

    private void HandleEnterMover(Collider other)
    {
        if (!IsServer && (UseVisualMover || ParentUnderVisualMover) && !(UseVisualMover && ParentUnderVisualMover))
        {
            return;
        }
        var moverTransform = UseVisualMover ? GetMoverTransform<PlatformVisualMover>(other) : GetMoverTransform<PlatformMover>(other);
        if (moverTransform == null)
        {
            return;
        }

        Debug.Log($"[Client-{NetworkObject.OwnerClientId}] Entered Plat: {moverTransform.gameObject.name}");

        if (transform.parent == m_OriginalParent)
        {
            if (!UseVisualMover)
            {
                var networkObject = moverTransform.GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    return;
                }
                if (!UseVisualMover && !NetworkObject.TrySetParent(networkObject, true))
                {
                    Debug.Log($"[Client-{NetworkManager.LocalClientId}] Failed to parent to: {moverTransform.gameObject.name}");
                    return;
                }
            }
            else if (UseVisualMover && m_CurrentPlatform != null) 
            {
                transform.SetParent(m_CurrentPlatform.transform, true);
                InLocalSpace = true;
                if (IsOwner)
                {
                    var bRef = new NetworkBehaviourReference(moverTransform.GetComponent<PlatformVisualMover>().PlatformMover);
                    //if (IsServer)
                    //{
                    //    NotifyPlatformParentChangedClientRpc(bRef);
                    //}
                    //else
                    //{
                    //    m_AssignedPlatform.Value = new NetworkBehaviourReference(moverTransform.GetComponent<PlatformVisualMover>().PlatformMover);
                    //}
                    m_AssignedPlatform.Value = bRef;
                    //SetState(transform.localPosition, null, null, false);
                }
            }
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (UseVisualMover)
        {
            HandleEnterVisualMover(other);
        }
        if (ParentUnderVisualMover)
        {
            HandleEnterMover(other);
        }
    }

    private void HandleExitVisualMover(Collider other)
    {
        // Exit early for Non-owners
        if (!CanCommitToTransform)
        {
            return;
        }

        if (UseVisualMover)
        {
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
        }
        Debug.Log($"Exited Platform!");
    }

    private void HandleExitMover(Collider other)
    {
        if (!IsServer && (UseVisualMover || ParentUnderVisualMover) && !(UseVisualMover && ParentUnderVisualMover))
        {
            return;
        }
        var moverTransform = UseVisualMover ? GetMoverTransform<PlatformVisualMover>(other) : GetMoverTransform<PlatformMover>(other);
        if (moverTransform == null)
        {
            return;
        }

        Debug.Log($"[Client-{NetworkObject.OwnerClientId}] Exited Plat: {moverTransform.gameObject.name}");

        if (transform.parent != m_OriginalParent)
        {
            if (!UseVisualMover)
            {
                var networkObject = moverTransform.GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    return;
                }
                if (!UseVisualMover && !NetworkObject.TryRemoveParent(true))
                {
                    Debug.Log($"[Client-{NetworkManager.LocalClientId}] Failed to unparent from: {moverTransform.gameObject.name}");
                    return;
                }
            }
            else if (UseVisualMover)
            {
                transform.SetParent(m_OriginalParent, false);
                InLocalSpace = false;
                if (IsOwner)
                {
                    m_AssignedPlatform.Value = new NetworkBehaviourReference();
                }
            }
        }
    }

    private void OnTriggerExit(Collider other) 
    {
        if (UseVisualMover)
        {
            HandleExitVisualMover(other);
        }

        if (ParentUnderVisualMover)
        {
            HandleExitMover(other);
        }
    }

}
