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
    public float pickupRange = 2f;  // Maximum distance to pick up an item
    public float maximumThrowTime = 5f;
    public float minimumThrowForce = 5f;
    public float maximumThrowForce = 15f;
    public GameObject pickupLocChild;
    public GameObject leftHandContact;
    public GameObject rightHandContact;

    private Rigidbody rb;
    private Animator animator;
    private new Collider collider;
    private GameObject currentPickupItem;
    private FixedJoint pickupLocfixedJoint;
    private float dropTime = .1f;

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
        if (Input.GetButtonDown("Fire1"))
        {
            // Find closest pickup item within range
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupRange);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("PickUpItem"))
                {
                    // Execute logic for picking up the item
                    currentPickupItem = hitCollider.gameObject;
                    // Rotate the player to face the item smoothly
                    StartCoroutine(SmoothLookAt(currentPickupItem.transform));
                    animator.SetTrigger("Pickup");
                }
            }
        }

        // throwing or dropping item
        if (currentPickupItem != null && Input.GetButtonDown("Fire2"))
        {
            StartCoroutine(ThrowOrDropItem());
        }
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
        yield return new WaitUntil(() => Input.GetButtonUp("Fire2"));
        float heldTime = Time.time - startTime;
        if (heldTime <= dropTime)
        {
            DropItem();
        }
        else
        {
            float throwForce = Mathf.Lerp(minimumThrowForce, maximumThrowForce, heldTime / maximumThrowTime);
            ThrowItem(throwForce);
        }
    }

    private void DropItem()
    {
        animator.SetTrigger("Drop");
        pickupLocfixedJoint.connectedBody = null;
        currentPickupItem.GetComponent<Rigidbody>().detectCollisions = true;
        currentPickupItem = null;
    }

    private void ThrowItem(float force)
    {
        animator.SetTrigger("Throw");
        pickupLocfixedJoint.connectedBody = null;
        Rigidbody itemRb = currentPickupItem.GetComponent<Rigidbody>();
        itemRb.detectCollisions = true;
        itemRb.AddForce(transform.forward * force, ForceMode.Impulse);
        currentPickupItem = null;
    }

    void OnDrawGizmos()
    {
        // Visualize the pickup range in the editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}

