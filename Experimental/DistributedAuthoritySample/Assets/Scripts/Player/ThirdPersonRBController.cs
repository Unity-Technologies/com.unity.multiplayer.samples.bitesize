using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class ThirdPersonRBController : MonoBehaviour
{
    public float moveSpeed = 20f;
    public float turnSpeed = 7f;
    public float jumpForce = 4f;
    public float gravityScale = 2f;
    public float pickupRange = 1f;  // Maximum distance to pick up an item
    public float maximumThrowTime = 5f;
    public float minimumThrowForce = 5f;
    public float maximumThrowForce = 25f;
    public GameObject pickupLocChild;
    public GameObject leftHandContact;
    public GameObject rightHandContact;
    public float pickupAngleThreshold = 0.342f; // Cosine of 70 degrees for a 140-degree cone

    private Rigidbody rb;
    private Animator animator;
    private new Collider collider;
    private GameObject currentPickupItem;
    private FixedJoint pickupLocfixedJoint;
    private float dropTime = .2f;
    private float heldTime = 0f;

    void Start()
    {
        pickupLocfixedJoint = pickupLocChild.GetComponent<FixedJoint>();
        rb = gameObject.GetComponent<Rigidbody>();
        animator = gameObject.GetComponentInChildren<Animator>();
        collider = gameObject.GetComponent<Collider>();
    }

    void LateUpdate()
    {
        // player input
        Vector2 moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (Input.GetKey(KeyCode.LeftShift))
        {
            moveInput.y = 2f;
        }

        // check grounded
        bool grounded = Physics.CheckCapsule(collider.bounds.center,
            new Vector3(collider.bounds.center.x, collider.bounds.min.y - 0.1f, collider.bounds.center.z), 0.02f);
        animator.SetBool("Grounded", grounded);

        // forward movement
        animator.SetFloat("Move", moveInput.y);
        rb.MovePosition(transform.position + transform.forward * moveInput.y * moveSpeed * Time.deltaTime);

        // turning
        Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, moveInput.x * turnSpeed * 30f, 0) * Time.deltaTime);
        rb.MoveRotation(rb.rotation * deltaRotation);

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
            Collider[] hitColliders = Physics.OverlapBox(new Vector3(transform.position.x, transform.position.y, transform.position.z + 0.5f), new Vector3(pickupRange/2, pickupRange/2, pickupRange/2));
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
                        // Rotate the player to face the item smoothly
                        StartCoroutine(SmoothLookAt(currentPickupItem.transform));
                        animator.SetTrigger("Pickup");
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
    }

    void OnDrawGizmos()
    {
        // Visualize the pickup range in the editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3(transform.position.x, transform.position.y, transform.position.z + 0.5f), new Vector3(pickupRange, pickupRange, pickupRange));
    }

    public void PickupAction()
    {
        // Create FixedJoint and connect it to the player's hand
        currentPickupItem.GetComponent<Rigidbody>().detectCollisions = false;
        currentPickupItem.transform.position = pickupLocChild.transform.position;
        currentPickupItem.transform.rotation = pickupLocChild.transform.rotation;
        pickupLocfixedJoint.connectedBody = currentPickupItem.GetComponent<Rigidbody>();
        // get prop hands location
        CarryableObject carryableObject = currentPickupItem.GetComponent<CarryableObject>();
        var leftHand = carryableObject.LeftHand;
        var rightHand = carryableObject.RightHand;
        // align hand contacts with prop hands
        leftHandContact.transform.position = leftHand.transform.position;
        rightHandContact.transform.position = rightHand.transform.position;
        leftHandContact.transform.rotation = leftHand.transform.rotation;
        rightHandContact.transform.rotation = rightHand.transform.rotation;

        Debug.Log("Picked up: " + currentPickupItem.name);
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
                DropAction();
            }

            // If throwTriggered is true, Activate ThrowAction here
            else
            {
                ThrowAction();
            }
        }

    }

    private void DropAction()
        {
            animator.SetTrigger("Drop");
            pickupLocfixedJoint.connectedBody = null;
            currentPickupItem.GetComponent<Rigidbody>().detectCollisions = true;
            currentPickupItem = null;
        }

        private void ThrowAction()
        {
            animator.SetTrigger("ThrowRelease");
            pickupLocfixedJoint.connectedBody = null;
            Rigidbody itemRb = currentPickupItem.GetComponent<Rigidbody>();
            itemRb.detectCollisions = true;
            float throwForce = Mathf.Lerp(minimumThrowForce, maximumThrowForce, heldTime / maximumThrowTime);
            itemRb.AddForce(transform.forward * throwForce, ForceMode.Impulse);
            currentPickupItem = null;
        }
}


