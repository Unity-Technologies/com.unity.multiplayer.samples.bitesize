using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Samples;
using UnityEngine;

/// <summary>
/// Assumes client authority
/// </summary>
[RequireComponent(typeof(ServerPlayerMove))]
[DefaultExecutionOrder(1)] // after server component
public class ClientPlayerMove : SamNetworkBehaviour
{
    protected override bool ClientOnly { get; } = true;

    private ServerPlayerMove m_Server;

    [SerializeField]
    private float Speed = 5;

    [SerializeField]
    private float RotSpeed = 5;

    [SerializeField]
    private Camera m_Camera;

    private Rigidbody m_Rigidbody;
    private NetworkTransform m_NetTransform;
    private CharacterController m_CharacterController;

    private void Awake()
    {
        m_Server = GetComponent<ServerPlayerMove>();
        m_Rigidbody = GetComponent<Rigidbody>();
        m_NetTransform = GetComponent<NetworkTransform>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void OnNetworkSpawn()
    {

        if (!IsOwner)
        {
            m_Camera.gameObject.SetActive(false);
            enabled = false;
            return;
        }

        // m_Rigidbody = GetComponent<Rigidbody>();
        m_CharacterController = GetComponent<CharacterController>();
        // gameObject.SetActive(false); // disable until we get spawned
    }

    [ClientRpc]
    public void SetSpawnClientRpc(Vector3 position, ClientRpcParams clientRpcParams = default)
    {
        m_CharacterController.enabled = false;
        transform.position = position;
        m_CharacterController.enabled = true;
        gameObject.SetActive(true);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 move = Input.GetAxis("Horizontal") * transform.right + Input.GetAxis("Vertical") * transform.forward;
        m_CharacterController.SimpleMove(move * Time.deltaTime * Speed);
        transform.Rotate(0, (Input.GetAxis("Mouse X")) * Time.deltaTime * RotSpeed, 0);


        // m_Rigidbody.MovePosition(transform.position + Input.GetAxis("Vertical") * Speed * Time.fixedDeltaTime * transform.forward);
        // m_Rigidbody.MoveRotation(Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0, Input.GetAxis("Horizontal") * RotSpeed * Time.fixedDeltaTime, 0)));
    }

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
                // todo use non-alloc
                var hit = Physics.OverlapSphere(transform.position, 5, LayerMask.GetMask(new[] {"PickupItems"}), QueryTriggerInteraction.Ignore);
                // var hasResult = Physics.BoxCast(transform.position, transform.lossyScale * 2, Vector3.zero, out var hit, transform.rotation, 0,
                // LayerMask.GetMask(new[] {"PickupItems"}), QueryTriggerInteraction.Ignore);
                if (hit.Length > 0)
                {
                    var ingredient = hit[0].gameObject.GetComponent<ServerIngredient>();
                    if (ingredient != null)
                    {
                        var netObj = ingredient.NetworkObjectId;
                        m_Server.PickupObjServerRpc(netObj);
                    }
                }
            }
        }
    }
}
