using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Samples;
using UnityEngine;

/// <summary>
/// Server side script to do some movements that can only be done server side with Netcode. In charge of spawning (which happens server side in Netcode)
/// and picking up objects
/// </summary>
[DefaultExecutionOrder(0)] // before client component
public class ServerPlayerMove : ClientServerBaseNetworkBehaviour
{
    private ClientPlayerMove m_Client;

    [SerializeField]
    private Camera m_Camera;

    public NetworkVariable<bool> ObjPickedUp = new NetworkVariable<bool>();

    private void Awake()
    {
        m_Client = GetComponent<ClientPlayerMove>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!enabled) return;

        // the following two lines should work, yet they don't. The second client connecting won't receive this position set and will spawn at the wrong position
        var spawnPoint = ServerPlayerSpawnPoints.Instance.ConsumeNextSpawnPoint();
        m_Client.SetSpawnClientRpc(spawnPoint.transform.position, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = new []{OwnerClientId}}});
    }

    private NetworkObject m_PickedUpObj;

    [ServerRpc]
    public void PickupObjServerRpc(ulong objToPickupID)
    {
        var objToPickup = NetworkManager.SpawnManager.SpawnedObjects[objToPickupID];
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
}
