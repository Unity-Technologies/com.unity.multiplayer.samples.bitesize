using System;
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
    [SerializeField]
    ClientPlayerMove m_ClientPlayerMove;

    public NetworkVariable<bool> isObjectPickedUp = new NetworkVariable<bool>();
    
    NetworkObject m_PickedUpObject;

    [SerializeField]
    Vector3 m_LocalHeldPosition;

    // DOC START HERE
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        OnServerSpawnPlayer();
    }

    void OnServerSpawnPlayer()
    {
        // this is done server side, so we have a single source of truth for our spawn point list
        var spawnPoint = ServerPlayerSpawnPoints.Instance.ConsumeNextSpawnPoint();
        var spawnPosition = spawnPoint ? spawnPoint.transform.position : Vector3.zero;
        // using client RPC since ClientNetworkTransform can only be modified by owner (which is client side)
        m_ClientPlayerMove.SetSpawnClientRpc(spawnPosition, 
            new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = new []{OwnerClientId}}});
    }

    [ServerRpc]
    public void PickupObjectServerRpc(ulong objToPickupID)
    {        
        NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objToPickupID, out var objToPickup);
        if (objToPickup == null || objToPickup.transform.parent != null) return; // object already picked up, server authority says no

        if (objToPickup.TryGetComponent(out NetworkObject networkObject) && networkObject.TrySetParent(transform))
        {
            objToPickup.GetComponent<Rigidbody>().isKinematic = true;
            objToPickup.GetComponent<NetworkTransform>().InLocalSpace = true;
            objToPickup.transform.localPosition = m_LocalHeldPosition;
            objToPickup.GetComponent<ServerIngredient>().ingredientDespawned += IngredientDespawned;
            isObjectPickedUp.Value = true;
            m_PickedUpObject = objToPickup;
        }
    }

    void IngredientDespawned()
    {
        m_PickedUpObject = null;
        isObjectPickedUp.Value = false;
    }
    
    [ServerRpc]
    public void DropObjectServerRpc()
    {
        if (m_PickedUpObject != null)
        {
            // can be null if enter drop zone while carrying
            m_PickedUpObject.transform.parent = null;
            m_PickedUpObject.GetComponent<Rigidbody>().isKinematic = false;
            m_PickedUpObject.GetComponent<NetworkTransform>().InLocalSpace = false;
            m_PickedUpObject = null;
        }

        isObjectPickedUp.Value = false;
    }
    // DOC END HERE
}
