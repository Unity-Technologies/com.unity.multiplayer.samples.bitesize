using Unity.Netcode;
using Unity.Netcode.Samples;
using UnityEngine;

/// <summary>
/// Assumes client authority
/// </summary>
[RequireComponent(typeof(ServerPlayerMove))]
[DefaultExecutionOrder(1)] // after server component
public class ClientPlayerMove : NetworkBehaviour
{
    private ServerPlayerMove m_Server;

    [SerializeField]
    private float m_Speed = 5;

    [SerializeField]
    private float m_RotSpeed = 5;

    [SerializeField]
    private Camera m_Camera;

    private CharacterController m_CharacterController;

    private void Awake()
    {
        m_Server = GetComponent<ServerPlayerMove>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        enabled = IsClient;
        if (!IsOwner)
        {
            m_Camera.gameObject.SetActive(false);
            enabled = false;
            return;
        }

        m_CharacterController = GetComponent<CharacterController>();
    }

    [ClientRpc]
    public void SetSpawnClientRpc(Vector3 position, ClientRpcParams clientRpcParams = default)
    {
        m_CharacterController.enabled = false;
        transform.position = position;
        m_CharacterController.enabled = true;
        gameObject.SetActive(true);
    }

    // DOC START HERE
    void FixedUpdate()
    {
        // enabled = false if we're not the owner, so no need to guard the following code if isOwner check
        // move client authoritative object. Those moves will be replicated to other players using ClientNetworkTransform (taken from Netcode's samples)
        Vector3 move = Input.GetAxis("Horizontal") * transform.right + Input.GetAxis("Vertical") * transform.forward;
        m_CharacterController.SimpleMove(move * Time.deltaTime * m_Speed);
        transform.Rotate(0, (Input.GetAxis("Mouse X")) * Time.deltaTime * m_RotSpeed, 0);
    }
    // DOC END HERE

    void Update()
    {
        if (Input.GetKeyDown("e"))
        {
            if (m_Server.ObjPickedUp.Value)
            {
                m_Server.DropObjServerRpc();
            }
            else
            {
                // todo use non-alloc, don't do the below at home
                // detect nearby ingredients
                var hit = Physics.OverlapSphere(transform.position, 5, LayerMask.GetMask(new[] {"PickupItems"}), QueryTriggerInteraction.Ignore);
                if (hit.Length > 0)
                {
                    var ingredient = hit[0].gameObject.GetComponent<ServerIngredient>();
                    if (ingredient != null)
                    {
                        var netObj = ingredient.NetworkObjectId;
                        // Netcode is a server driven SDK. Shared objects like ingredients need to be interacted with using ServerRPCs. Therefore, there
                        // will be a delay between the button press and the reparenting.
                        // This delay could be hidden with some animations/sounds/VFX that would be triggered here.
                        m_Server.PickupObjServerRpc(netObj);
                    }
                }
            }
        }
    }
}
