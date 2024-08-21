using Unity.Netcode;
using UnityEngine;

public class CarryableObjectMP : NetworkBehaviour
{
    public GameObject LeftHand;
    public GameObject RightHand;

    public Transform LeftHandContact;
    public Transform RightHandContact;

    public bool IfPickedUp;

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

    public void OnUpdate()
    {
        if (!IsSpawned || !IfPickedUp)
        {
            return;
        }

        LeftHandContact.position = LeftHand.transform.position;
        RightHandContact.position = RightHand.transform.position;
    }
}
