using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Server side script to do some movements that can only be done server side with Netcode. In charge of spawning (which happens server side in Netcode)
/// and picking up objects
/// </summary>
[DefaultExecutionOrder(0)] // before client component
public class ServerPlayerMove : NetworkBehaviour
{
    private ClientPlayerMove m_Client;

    [SerializeField]
    private Camera m_Camera;

    public NetworkVariable<bool> ObjPickedUp = new NetworkVariable<bool>();

    private void Awake()
    {
        m_Client = GetComponent<ClientPlayerMove>();
    }

    // DOC START HERE
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        // this is done server side, so we have a single source of truth for our spawn point list
        var spawnPoint = ServerPlayerSpawnPoints.Instance.ConsumeNextSpawnPoint();
        // using client RPC since ClientNetworkTransform can only be modified by owner (which is client side)
        m_Client.SetSpawnClientRpc(spawnPoint.transform.position, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = new []{OwnerClientId}}});
    }

    private NetworkObject m_PickedUpObj;

    [ServerRpc]
    public void PickupObjServerRpc(ulong objToPickupID)
    {
        NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objToPickupID, out var objToPickup);
        if (objToPickup == null || objToPickup.transform.parent != null) return; // object already picked up, server authority says no

        objToPickup.GetComponent<Rigidbody>().isKinematic = true;
        objToPickup.transform.parent = transform;
        objToPickup.GetComponent<NetworkTransform>().InLocalSpace = true;
        objToPickup.transform.localPosition = Vector3.up;
        ObjPickedUp.Value = true;
        m_PickedUpObj = objToPickup;
    }

    [ServerRpc]
    public void DropObjServerRpc()
    {
        if (m_PickedUpObj != null)
        {
            // can be null if enter drop zone while carying
            m_PickedUpObj.transform.localPosition = new Vector3(0, 0, 2);
            m_PickedUpObj.transform.parent = null;
            m_PickedUpObj.GetComponent<Rigidbody>().isKinematic = false;
            m_PickedUpObj.GetComponent<NetworkTransform>().InLocalSpace = false;
            m_PickedUpObj = null;
        }

        ObjPickedUp.Value = false;
    }
    // DOC END HERE
}
