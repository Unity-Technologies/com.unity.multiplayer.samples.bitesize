using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Used with <see cref="TargetingSystem"/>, this provides a wider BoxCollider region to determine if 
/// an existing targeted object is no longer within the "targeting frustum". Having a wider area for
/// exiting allows one to "lead" an object when shooting.
/// TODO: The <see cref="TargetingSystem.TargetMarker"/> needs some additional code to adjust itself 
/// for leading shots still.
/// </summary>
public class TargetingSystemRelease : NetworkBehaviour
{
    [HideInInspector]
    public BoxCollider TargetingReleaseCollider;
    private TargetingSystem m_TargetingSystem;


    private void Awake()
    {
        m_TargetingSystem = transform.parent.GetComponent<TargetingSystem>();
        TargetingReleaseCollider = m_TargetingSystem.GetComponent<BoxCollider>();
    }

    private void OnTriggerExit(Collider collider)
    {
        if (!IsOwner || !IsSpawned)
        {
            return;
        }
        var rootObject = BaseObjectMotionHandler.GetRootParent(collider.gameObject);
        if (rootObject == m_TargetingSystem.ClosestTarget)
        {
            m_TargetingSystem.UpdateTargetAndMarker();
        }
    }
}
