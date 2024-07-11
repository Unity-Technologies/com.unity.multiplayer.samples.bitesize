using Unity.Netcode;
using UnityEngine;

/// <summary>
/// The targeting system is a child GameObject that consist of:
/// - Child-Root: a kinematic Rigidbody with a targeting BoxCollider to detect objects to be targeted
/// - Child of the Child-Root: a kinematic Rigidbody with a targeting BoxCollider to detect when a 
/// targeted object is out of the "targeting area".
/// 
/// Using layers and filtering layers, this is the most cost effective way to accomplish targeting.
/// </summary>
public class TargetingSystem : NetworkBehaviour
{
    public GameObject TargetMarker;

    [Range(1.0f, 10.0f)]
    public float TargetingAssist = 5.0f;

    public float CrossHairOffset = 5.0f;

    [HideInInspector]
    public GameObject ClosestTarget;

    [HideInInspector]
    public PhysicsObjectMotion ClosestTargetPhysicsObject;

    private float ClosestTargetDistance = float.MaxValue;
    private GameObject m_RootParent;

    private void Awake()
    {
        m_RootParent = BaseObjectMotionHandler.GetRootParent(gameObject);
    }

    private void OnEnable()
    {
        TargetMarker.SetActive(true);
    }

    /// <summary>
    /// When an object first intersects/collides with targeting BoxCollider (trigger)
    /// </summary>
    private void OnTriggerEnter(Collider collider)
    {
        UpdateTrigger(collider);
    }

    /// <summary>
    /// Provides continual monitoring of potential targets
    /// </summary>
    private void OnTriggerStay(Collider collider)
    {
        UpdateTrigger(collider);
    }

    private void UpdateTrigger(Collider collider)
    {
        if (!IsOwner || !IsSpawned)
        {
            return;
        }

        var rootColliderObject = BaseObjectMotionHandler.GetRootParent(collider.gameObject);

        // Do not target ourselves
        if (rootColliderObject == m_RootParent)
        {
            if (ClosestTarget != null)
            {
                UpdateTargetAndMarker();
            }
            return;
        }

        // Ignore something we are already targeting
        if (rootColliderObject == ClosestTarget)
        {
            // Ignor things we picked up (i.e. if we get the root of something we picked up that root could be the player)
            if (ClosestTarget.transform.parent == m_RootParent.transform)
            {

                return;
            }
            ClosestTargetDistance = Vector3.Distance(rootColliderObject.transform.position, m_RootParent.transform.position);
            return;
        }

        var motionHandler = rootColliderObject.GetComponent<BaseObjectMotionHandler>();
        if (motionHandler != null && (motionHandler.CollisionType == CollisionTypes.Asteroid || motionHandler.CollisionType == CollisionTypes.Ship || motionHandler.CollisionType == CollisionTypes.Mine))
        {
            var distance = Vector3.Distance(rootColliderObject.transform.position, m_RootParent.transform.position);
            if (distance < ClosestTargetDistance)
            {
                UpdateTargetAndMarker(rootColliderObject, distance);
            }
        }
    }

    /// <summary>
    /// Updates the status of the current target and does some house keeping
    /// </summary>
    public void UpdateTargetAndMarker(GameObject targetObject = null, float distance = 0.0f)
    {
        var isActive = targetObject != null;
        TargetMarker.SetActive(isActive);

        if (isActive)
        {
            ClosestTarget = targetObject;
            ClosestTargetPhysicsObject = ClosestTarget.GetComponent<PhysicsObjectMotion>();
            ClosestTargetDistance = distance;
            TargetMarker.transform.position = ClosestTarget.transform.position;
            // Get a notification when/if the target despawns
            ClosestTarget.GetComponent<BaseObjectMotionHandler>().OnNetworkObjectDespawned -= TargetDespawned;
            ClosestTarget.GetComponent<BaseObjectMotionHandler>().OnNetworkObjectDespawned += TargetDespawned;
        }
        else
        {
            if (ClosestTarget != null)
            {
                ClosestTarget.GetComponent<BaseObjectMotionHandler>().OnNetworkObjectDespawned -= TargetDespawned;
                ClosestTarget = null;
                ClosestTargetPhysicsObject = null;
                ClosestTargetDistance = float.MaxValue;
            }
            TargetMarker.transform.position = m_RootParent.transform.position;
        }
    }

    /// <summary>
    /// Removes a target if it is despawned
    /// </summary>
    private void TargetDespawned()
    {
        UpdateTargetAndMarker();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsLocalPlayer)
        {
            gameObject.SetActive(false);
        }
        else
        {
            TargetMarker.SetActive(false);
        }
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        // Since we pool these objects, reset the target marker and root to being active so the next time it is used the target marker's
        // NetworkBehaviour component will be included in the the spawn sequence and associated with the root NetworkObject.
        // The pool will set the root GameObject to be inactive, which will prevent the marker from rendering
        gameObject.SetActive(true);
        TargetMarker.SetActive(true);
        base.OnNetworkDespawn();
    }


    private void Update()
    {
        if (!IsSpawned || !IsOwner || ClosestTarget == null || NetworkManager.Singleton.ShutdownInProgress)
        {
            return;
        }

        UpdateTargetMarker();
    }


    private Vector2[] m_IntersectPoints = new Vector2[4];
    /// <summary>
    /// Allows one to lead a target
    /// </summary>
    void UpdateTargetMarker()
    {
        if (ClosestTarget.transform.parent == m_RootParent.transform)
        {
            UpdateTargetAndMarker();
            return;
        }

        var markerPosition = ClosestTarget.transform.position;
        var vectorTowardsMarker = (ClosestTarget.transform.position - m_RootParent.transform.position).normalized;
        var vectorTowardsShip = (m_RootParent.transform.position - TargetMarker.transform.position).normalized;
        if (vectorTowardsShip != Vector3.zero)
        {
            TargetMarker.transform.forward = vectorTowardsShip;
        }

        Vector3 crossBetweenTargetAndShip = Vector3.Cross(vectorTowardsMarker, m_RootParent.transform.forward);
        // Lead right as a default
        var markerLineProjection = 20.0f;
        if (crossBetweenTargetAndShip == Vector3.zero)
        {
            // We are in alignment, don't need to do anying
            return;
        }
        else // If we determined we are leading left, then invert markerLineProjection
        if (crossBetweenTargetAndShip.y < 0)
        {
            markerLineProjection = -20.0f;
        }

        // Create two lines to get an intersection point
        var distanceToMarker = Vector3.Distance(m_RootParent.transform.position, ClosestTarget.transform.position);
        var projectedDistance = distanceToMarker * 2.25f;
        var shipToMarkerLine = m_RootParent.transform.position + (m_RootParent.transform.forward * projectedDistance);
        var leftProjectMarker = ClosestTarget.transform.position + (m_RootParent.transform.right * markerLineProjection);
        m_IntersectPoints[0].x = m_RootParent.transform.position.x;
        m_IntersectPoints[0].y = m_RootParent.transform.position.z;
        m_IntersectPoints[1].x = shipToMarkerLine.x;
        m_IntersectPoints[1].y = shipToMarkerLine.z;
        m_IntersectPoints[2].x = ClosestTarget.transform.position.x;
        m_IntersectPoints[2].y = ClosestTarget.transform.position.z;
        m_IntersectPoints[3].x = leftProjectMarker.x;
        m_IntersectPoints[3].y = leftProjectMarker.z;

        float cross1 = Vector3.Cross(m_IntersectPoints[0] - m_IntersectPoints[2], m_IntersectPoints[3] - m_IntersectPoints[2]).z;
        float cross2 = Vector3.Cross(m_IntersectPoints[1] - m_IntersectPoints[2], m_IntersectPoints[3] - m_IntersectPoints[2]).z;

        // If lines are parallel (shouldn't ever happen for this method), then just do nothing and exit
        if (cross1 - cross2 == 0)
        {
            return;
        }

        // Get intersection and apply adusted position
        var intersect = (cross1 * m_IntersectPoints[1] - cross2 * m_IntersectPoints[0]) / (cross1 - cross2);
        markerPosition.x = intersect.x;
        markerPosition.z = intersect.y;
        var offset = Mathf.Clamp(distanceToMarker / CrossHairOffset, 2.0f, CrossHairOffset);
        TargetMarker.transform.position = Vector3.Lerp(TargetMarker.transform.position, markerPosition + vectorTowardsShip * offset, Time.deltaTime * TargetingAssist);
    }
}
