using UnityEngine;
using UnityEngine.Serialization;

public class ThirdPersonRBController : MonoBehaviour
{
    public float moveSpeed = 20f;
    public float turnSpeed = 7f;
    public float jumpForce = 4f;
    public float gravityScale = 2f;
    public float pickupRange = 2f;  // Maximum distance to pick up an item
    [FormerlySerializedAs("pickupLocChildTransform")]
    public GameObject pickupLocChild;

    private Rigidbody rb;
    private Animator animator;
    private new Collider collider;
    private GameObject currentPickupItem;
    private FixedJoint pickupLocfixedJoint;

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
            PickupAction();
        }
    }

    private void PickupAction()
    {
        // Find closest pickup item within range
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupRange);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("PickUpItem"))
            {
                // Execute logic for picking up the item
                currentPickupItem = hitCollider.gameObject;

                // Create FixedJoint and connect it to the player's hand
                currentPickupItem.GetComponent<Rigidbody>().detectCollisions = false;
                currentPickupItem.transform.position = pickupLocChild.transform.position;
                currentPickupItem.transform.rotation = pickupLocChild.transform.rotation;
                /*var currentRotation = currentPickupItem.transform.localEulerAngles;
                currentPickupItem.transform.localEulerAngles = new Vector3(currentRotation.x + 90, currentRotation.y, currentRotation.z);*/
                pickupLocfixedJoint.connectedBody = currentPickupItem.GetComponent<Rigidbody>();

                Debug.Log("Picked up: " + currentPickupItem.name);
                animator.SetTrigger("Pickup");
                break;
            }
        }
    }

    void OnDrawGizmos()
    {
        // Visualize the pickup range in the editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}

