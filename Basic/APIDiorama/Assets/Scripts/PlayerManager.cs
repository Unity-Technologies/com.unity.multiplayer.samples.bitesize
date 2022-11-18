using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : NetworkBehaviour
{
    [SerializeField]
    PlayerInput inputManager;


    [SerializeField]
    float moveSpeed = 1;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (inputManager)
        {
            inputManager.enabled = IsOwner;
        }
    }
}