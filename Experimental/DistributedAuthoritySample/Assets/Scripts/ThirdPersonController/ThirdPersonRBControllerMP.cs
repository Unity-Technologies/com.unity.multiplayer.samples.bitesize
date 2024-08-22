using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;


public class ThirdPersonRBControllerMP : NetworkBehaviour
{
    public float moveSpeed = 20f;
    public float turnSpeed = 7f;
    public float jumpForce = 4f;
    public float gravityScale = 2f;
    public float pickupRange = 1f; // Maximum distance to pick up an item
    public float maximumThrowTime = 5f;
    public float minimumThrowForce = 5f;
    public float maximumThrowForce = 25f;
    public GameObject pickupLocChild;
    public GameObject leftHandContact;
    public GameObject rightHandContact;
    public float pickupAngleThreshold = 0.342f; // Cosine of 70 degrees for a 140-degree cone
    public LayerMask groundLayer;
    [Range(0.01f, 1.0f)]
    public float MouseZoomSpeed = 0.5f;
    [Range(0.1f, 10.0f)]
    public float MouseFreeLookSpeed = 2.0f;
    private NetworkRigidbody m_NetworkRigidbody;
    private Rigidbody rb;
    private Animator animator;
    private new Collider collider;
    private GameObject currentPickupItem;
    private CarryableObjectMP m_CarryableObject;
    private FixedJoint pickupLocfixedJoint;
    private float dropTime = .2f;
    private float heldTime = 0f;

    private ulong m_CurrentCameraTarget;
    private float m_CameraDistance;
    private float m_MaxCameraDistance;
    private Vector3 m_OriginalCameraPosition;
    private Quaternion m_OriginalCameraRotation;

    private NetworkVariable<NetworkBehaviourReference> m_CurrentPickupItem = new NetworkVariable<NetworkBehaviourReference>(new NetworkBehaviourReference());
    void Start()
    {
        pickupLocfixedJoint = pickupLocChild.GetComponent<FixedJoint>();
        rb = gameObject.GetComponent<Rigidbody>();
        animator = gameObject.GetComponentInChildren<Animator>();
        collider = gameObject.GetComponent<Collider>();
        m_NetworkRigidbody = GetComponent<NetworkRigidbody>();
    }

    protected override void OnNetworkPostSpawn()
    {
        if (HasAuthority)
        {
            m_OriginalCameraPosition = Camera.main.transform.position;
            m_OriginalCameraRotation = Camera.main.transform.rotation;
            m_CurrentCameraTarget = OwnerClientId;
            var position = Camera.main.transform.position;
            var yPosition = position.y;
            m_MaxCameraDistance = m_CameraDistance = Vector3.Distance(transform.position, position);

            position = transform.position + (transform.forward * m_CameraDistance);
            position.y = yPosition;
            Camera.main.transform.position = position;
            Camera.main.transform.SetParent(transform);
            Camera.main.transform.LookAt(transform);
        }

        base.OnNetworkPostSpawn();
    }

    protected override void OnNetworkSessionSynchronized()
    {
        // Synchronize late joining players with the item being carried
        if (!HasAuthority)
        {
            if (m_CurrentPickupItem.Value.TryGet(out m_CarryableObject))
            {
                currentPickupItem = m_CarryableObject.gameObject;
                OnPickupAction(OwnerClientId);
            }
        }
        base.OnNetworkSessionSynchronized();
    }

    public override void OnNetworkDespawn()
    {
        if (HasAuthority && Camera.main != null)
        {
            Camera.main.transform.SetParent(null);
            Camera.main.transform.position = m_OriginalCameraPosition;
            Camera.main.transform.rotation = m_OriginalCameraRotation;
        }
        base.OnNetworkDespawn();
    }


    /// <summary>
    /// Handles camera rotation and zoom
    /// </summary>

    private void LateUpdate()
    {
        if (!IsSpawned || !IsOwner || !Application.isFocused)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab) && NetworkManager.ConnectedClientsIds.Count > 1)
        {
            var index = NetworkManager.ConnectedClientsIds.ToList().IndexOf(m_CurrentCameraTarget);
            index++;
            index %= NetworkManager.ConnectedClientsIds.Count;
            m_CurrentCameraTarget = NetworkManager.ConnectedClientsIds[index];
            Camera.main.transform.SetParent(NetworkManager.ConnectedClients[m_CurrentCameraTarget].PlayerObject.transform, false);
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Camera.main.transform.parent == null || !NetworkManager.ConnectedClients.ContainsKey(m_CurrentCameraTarget))
        {
            m_CurrentCameraTarget = OwnerClientId;
            Camera.main.transform.SetParent(NetworkManager.ConnectedClients[m_CurrentCameraTarget].PlayerObject.transform, false);
        }

        var parent = Camera.main.transform.parent;
        if (Input.GetMouseButton(3) || Input.GetMouseButton(2))
        {
            var mouseMotion = Input.mousePositionDelta.normalized;
            if (Mathf.Abs(mouseMotion.x) > 0.0f)
            {
                Camera.main.transform.RotateAround(parent.position, parent.up, mouseMotion.x * MouseFreeLookSpeed);
            }
            if (Mathf.Abs(mouseMotion.y) > 0.0f)
            {
                Camera.main.transform.RotateAround(parent.position, Camera.main.transform.right, mouseMotion.y * MouseFreeLookSpeed);
            }
        }
        if (Mathf.Abs(Input.mouseScrollDelta.y) > 0.0f)
        {
            var distance = Camera.main.transform.localPosition.magnitude;
            var positionVector = Camera.main.transform.localPosition.normalized;
            m_CameraDistance = Mathf.Clamp((Input.mouseScrollDelta.y * MouseZoomSpeed) + distance, 2.0f, m_MaxCameraDistance);
            Camera.main.transform.localPosition = positionVector * m_CameraDistance;
        }
        Camera.main.transform.LookAt(parent);
    }

    /// <summary>
    /// Handles motion related actions
    /// </summary>
    private void FixedUpdate()
    {
        // Exit early if not spawned
        if (!IsSpawned)
        {
            return;
        }

        // If the instance is carrying something, then invoke its OnUpdate to keep
        // connection points synchronized with object being carried
        if (m_CarryableObject != null && m_CarryableObject.IfPickedUp)
        {
            m_CarryableObject.OnUpdate();
        }

        // If:
        // - we aren't the owner
        // - we aren't the owner of our current camera target
        // - the application has no focus
        // Then exit early
        if (!IsOwner || m_CurrentCameraTarget != OwnerClientId || !Application.isFocused)
        {
            return;
        }

        // player input
        Vector2 moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (Input.GetKey(KeyCode.LeftShift))
        {
            moveInput.y *= 2f;
        }

        // check grounded
        Vector3 capsuleBottom = new Vector3(collider.bounds.center.x, collider.bounds.min.y, collider.bounds.center.z);
        Vector3 capsuleTop = new Vector3(collider.bounds.center.x, collider.bounds.min.y + 0.1f, collider.bounds.center.z);
        bool grounded = Physics.CheckCapsule(capsuleBottom, capsuleTop, 0.1f, groundLayer);
        animator.SetBool("Grounded", grounded);


        // forward movement
        animator.SetFloat("Move", moveInput.y);
        if (moveInput.y < 0)
        {
            animator.SetBool("Backwards", true);
        }
        else
        {
            animator.SetBool("Backwards", false);
        }

        rb.MovePosition(transform.position + transform.forward * moveInput.y * moveSpeed * Time.fixedDeltaTime);

        // turning
        if (Mathf.Abs(moveInput.x) > 0.01f) // Only rotate if moveInput.x is significantly non-zero
        {
            Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, moveInput.x * turnSpeed * 30f, 0) * Time.fixedDeltaTime);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }

        // jumping
        if (Input.GetButtonDown("Jump") && grounded)
        {
            animator.SetTrigger("Jump");
            rb.AddForce(new Vector3(0, 1, 0) * jumpForce * 100f);
        }

        rb.AddForce(new Vector3(0, -1, 0) * gravityScale);

        // grabbing item
        if (Input.GetButtonDown("Fire1") && currentPickupItem == null)
        {
            // Find closest pickup item within range
            Collider[] hitColliders = Physics.OverlapBox(new Vector3(transform.position.x, transform.position.y, transform.position.z + 0.5f), new Vector3(pickupRange / 2, pickupRange / 2, pickupRange / 2));
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("PickUpItem"))
                {
                    // Calculate direction to the item
                    Vector3 directionToItem = (hitCollider.transform.position - transform.position).normalized;
                    float dotProduct = Vector3.Dot(transform.forward, directionToItem);

                    // Check if the item is within the field of view
                    if (dotProduct > pickupAngleThreshold)
                    {
                        // Execute logic for picking up the item
                        currentPickupItem = hitCollider.gameObject;

                        m_CarryableObject = currentPickupItem.GetComponent<CarryableObjectMP>();
                        if (m_CarryableObject.PickUp())
                        {
                            // For late joining players
                            m_CurrentPickupItem.Value = new NetworkBehaviourReference(m_CarryableObject);

                            // Lock the NetworkObject while we are carrying it
                            m_CarryableObject.NetworkObject.SetOwnershipLock(true);

                            // For immediate notification
                            OnObjectPickedUpRpc(m_CurrentPickupItem.Value);

                            // Rotate the player to face the item smoothly
                            StartCoroutine(SmoothLookAt(currentPickupItem.transform));
                            animator.SetTrigger("Pickup");
                        }
                        else
                        {
                            // Maybe animation or notification you can't pick it up right now
                        }
                        break; // Exit the loop after picking up the first item
                    }
                }
            }
        }

        // throwing or dropping item
        if (currentPickupItem != null && Input.GetButtonDown("Fire2"))
        {
            StartCoroutine(ThrowOrDropItem());
        }

        // Apply a dead zone to the angular velocity to zero out small values
        if (Mathf.Abs(rb.angularVelocity.y) < 0.7f)
        {
            rb.angularVelocity = new Vector3(rb.angularVelocity.x, 0, rb.angularVelocity.z);
        }
    }

    [Rpc(SendTo.NotAuthority)]
    private void OnObjectPickedUpRpc(NetworkBehaviourReference networkBehaviourReference, RpcParams rpcParams = default)
    {
        if (networkBehaviourReference.TryGet(out m_CarryableObject, NetworkManager))
        {
            currentPickupItem = m_CarryableObject.gameObject;
            OnPickupAction(rpcParams.Receive.SenderClientId);
        }
    }

    void OnDrawGizmos()
    {
        // Visualize the pickup range in the editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3(transform.position.x, transform.position.y, transform.position.z + 0.5f), new Vector3(pickupRange, pickupRange, pickupRange));
    }

    private void OnPickupAction(ulong clientId)
    {
        // Create FixedJoint and connect it to the player's hand
        currentPickupItem.transform.position = pickupLocChild.transform.position;
        currentPickupItem.transform.rotation = pickupLocChild.transform.rotation;
        var rigidbody = currentPickupItem.GetComponent<Rigidbody>();
        rigidbody.detectCollisions = false;
        if (HasAuthority)
        {
            rigidbody.useGravity = false;
            pickupLocfixedJoint.connectedBody = rigidbody;
        }

        // get prop hands location
        var leftHand = m_CarryableObject.LeftHand;
        var rightHand = m_CarryableObject.RightHand;

        m_CarryableObject.IfPickedUp = true;
        m_CarryableObject.RightHandContact = rightHandContact.transform;
        m_CarryableObject.LeftHandContact = leftHandContact.transform;

        // align hand contacts with prop hands
        leftHandContact.transform.position = leftHand.transform.position;
        rightHandContact.transform.position = rightHand.transform.position;
        leftHandContact.transform.rotation = leftHand.transform.rotation;
        rightHandContact.transform.rotation = rightHand.transform.rotation;

        Debug.Log($"[Client-{clientId}] Picked up: " + currentPickupItem.name);
    }

    /// <summary>
    /// Authority invokes this via animation
    /// </summary>
    public void PickupAction()
    {
        if (!HasAuthority)
        {
            return;
        }
        OnPickupAction(OwnerClientId);
    }

    private IEnumerator SmoothLookAt(Transform target)
    {
        Quaternion initialRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);
        var elapsedTime = 0f;
        var duration = 0.23f; // Duration of the rotation in seconds
        var rotation = transform.rotation;
        while (elapsedTime < duration)
        {
            rotation = Quaternion.Slerp(initialRotation, targetRotation, elapsedTime / duration);
            rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0); // Keep only the y-axis rotation
            transform.rotation = rotation;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the final rotation is exactly towards the target
        rotation = targetRotation;
        rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0); // Keep only the y-axis rotation
    }

    private IEnumerator ThrowOrDropItem()
    {
        float startTime = Time.time;
        bool throwTriggered = false;

        // Coroutine to handle the button release scenario
        StartCoroutine(HandleButtonRelease(startTime, throwTriggered));

        // Continuously check if heldTime reaches dropTime
        while (!throwTriggered)
        {
            heldTime = Time.time - startTime;
            if (heldTime >= dropTime)
            {
                animator.SetTrigger("Throw");
                throwTriggered = true;
            }

            if (currentPickupItem == null)
            {
                break;
            }

            yield return null; // Wait for the next frame
        }
    }

    private IEnumerator HandleButtonRelease(float startTime, bool throwTriggered)
    {
        yield return new WaitUntil(() => Input.GetButtonUp("Fire2") || throwTriggered);
        if (!throwTriggered)
        {
            heldTime = Time.time - startTime;
            if (heldTime < dropTime)
            {
                OnObjectDroppedRpc(false);
                DropAction();
            }

            // If throwTriggered is true, Activate ThrowAction here
            else
            {
                heldTime = Time.time - startTime;
                OnObjectDroppedRpc(true);
                ThrowAction();
            }
        }
    }

    [Rpc(SendTo.NotAuthority)]
    private void OnObjectDroppedRpc(bool isThrowing, RpcParams rpcParams = default)
    {
        if (isThrowing)
        {
            OnThrowAction();
        }
        else
        {
            OnDropAction();
        }
    }

    private void OnDropAction()
    {
        m_CarryableObject.IfPickedUp = false;
        var rigidbody = currentPickupItem.GetComponent<Rigidbody>();
        rigidbody.detectCollisions = true;
        rigidbody.useGravity = true;
        currentPickupItem = null;
    }

    private void DropAction()
    {
        if (!HasAuthority)
        {
            return;
        }
        animator.SetTrigger("Drop");
        pickupLocfixedJoint.connectedBody = null;
        m_CurrentPickupItem.Value = new NetworkBehaviourReference();
        // Unlock the object when we drop it
        m_CarryableObject.NetworkObject.SetOwnershipLock(false);
        OnDropAction();
    }

    private void OnThrowAction()
    {
        m_CarryableObject.IfPickedUp = false;
        var rigidbody = currentPickupItem.GetComponent<Rigidbody>();
        rigidbody.detectCollisions = true;
        rigidbody.useGravity = true;
        currentPickupItem = null;
        m_CarryableObject = null;
    }

    private void ThrowAction()
    {
        if (!HasAuthority)
        {
            return;
        }
        animator.SetTrigger("ThrowRelease");
        pickupLocfixedJoint.connectedBody = null;
        // Unlock the object when we drop it
        m_CarryableObject.NetworkObject.SetOwnershipLock(false);
        var rigidbody = currentPickupItem.GetComponent<Rigidbody>();
        rigidbody.detectCollisions = true;
        rigidbody.useGravity = true;
        float throwForce = Mathf.Lerp(minimumThrowForce, maximumThrowForce, heldTime / maximumThrowTime);
        rigidbody.AddForce(transform.forward * throwForce, ForceMode.Impulse);
        currentPickupItem = null;
        m_CarryableObject = null;
        m_CurrentPickupItem.Value = new NetworkBehaviourReference();
    }
}
