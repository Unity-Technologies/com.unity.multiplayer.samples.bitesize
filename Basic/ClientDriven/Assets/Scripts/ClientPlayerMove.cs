using Cinemachine;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Assumes client authority
/// </summary>
[RequireComponent(typeof(ServerPlayerMove))]
[DefaultExecutionOrder(1)] // after server component
public class ClientPlayerMove : NetworkBehaviour
{
    [SerializeField]
    ServerPlayerMove m_ServerPlayerMove;

    [SerializeField]
    CharacterController m_CharacterController;

    [SerializeField]
    ThirdPersonController m_ThirdPersonController;

    [SerializeField]
    Transform m_CameraFollow;

    [SerializeField]
    PlayerInput m_PlayerInput;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        m_ThirdPersonController.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        enabled = IsClient;
        if (!IsOwner)
        {
            m_ThirdPersonController.enabled = false;
            enabled = false;
            return;
        }

        m_PlayerInput.enabled = true;
        m_ThirdPersonController.enabled = true;

        var cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        cinemachineVirtualCamera.Follow = m_CameraFollow;
    }

    void OnPickUp()
    {
        if (m_ServerPlayerMove.ObjPickedUp.Value)
        {
            m_ServerPlayerMove.DropObjServerRpc();
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
                    m_ServerPlayerMove.PickupObjServerRpc(netObj);
                }
            }
        }
    }

    [ClientRpc]
    public void SetSpawnClientRpc(Vector3 position, ClientRpcParams clientRpcParams = default)
    {
        m_CharacterController.enabled = false;
        transform.position = position;
        m_CharacterController.enabled = true;
        gameObject.SetActive(true);
    }
}
