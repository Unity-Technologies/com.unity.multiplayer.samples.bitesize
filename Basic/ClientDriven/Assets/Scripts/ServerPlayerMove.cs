using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Samples;
using UnityEngine;
using UnityEngine.Assertions;

[DefaultExecutionOrder(0)] // before client component
public class ServerPlayerMove : SamNetworkBehaviour
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
        // m_Rigidbody.position = spawnPoint.transform.position; // this should work, yet it doesn't

        // the following two lines should work, yet they don't. The second client connecting won't receive this position set and will spawn at the wrong position
        // var spawnPoint = PlayerSpawnPoints.Instance.ConsumeNextSpawnPoint();
        // m_Client.SetSpawnClientRpc(spawnPoint.transform.position, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = new []{OwnerClientId}}});

        // this workaround works
        StartCoroutine(SendSpawnLater());
    }

    private IEnumerator SendSpawnLater()
    {
        yield return new WaitForSeconds(0.5f); // looks like if server sends client RPC before client is spawned, RPC is lost?
        var spawnPoint = PlayerSpawnPoints.Instance.ConsumeNextSpawnPoint();
        m_Client.SetSpawnClientRpc(spawnPoint.transform.position, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = new []{OwnerClientId}}});
    }



    private NetworkObject pickedUpObj;

    [ServerRpc]
    public void PickupObjServerRpc(ulong objToPickupID)
    {
        var objToPickup = NetworkManager.SpawnManager.SpawnedObjects[objToPickupID];
        objToPickup.GetComponent<Rigidbody>().isKinematic = true;
        objToPickup.transform.parent = transform;
        objToPickup.GetComponent<NetworkTransform>().InLocalSpace = true;
        objToPickup.transform.localPosition = Vector3.up;
        ObjPickedUp.Value = true;
        pickedUpObj = objToPickup;
    }

    [ServerRpc]
    public void DropObjServerRpc()
    {
        if (pickedUpObj != null)
        {
            // can be null if enter drop zone while carying
            pickedUpObj.transform.localPosition = new Vector3(0, 0, 2);
            pickedUpObj.transform.parent = null;
            pickedUpObj.GetComponent<Rigidbody>().isKinematic = false;
            pickedUpObj.GetComponent<NetworkTransform>().InLocalSpace = false;
            pickedUpObj = null;
        }

        ObjPickedUp.Value = false;
    }
}
