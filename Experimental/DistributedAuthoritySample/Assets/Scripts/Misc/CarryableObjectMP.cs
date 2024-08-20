using Unity.Netcode;
using UnityEngine;

public class CarryableObjectMP : NetworkBehaviour
{
    public GameObject LeftHand;
    public GameObject RightHand;

    public bool PickUp()
    {
        if (!IsSpawned)
        {
            return false;
        }

        if (HasAuthority)
        {
            return true;
        }

        if (NetworkObject.IsOwnershipLocked)
        {
            return false;
        }

        NetworkObject.ChangeOwnership(NetworkManager.LocalClientId);

        return true;
    }
}
