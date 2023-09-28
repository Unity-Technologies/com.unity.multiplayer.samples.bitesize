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
    public NetworkVariable<bool> isObjectPickedUp = new NetworkVariable<bool>();
    
    NetworkObject m_PickedUpObject;

    [SerializeField]
    Vector3 m_LocalHeldPosition;

    // DOC START HERE
    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        OnServerSpawnPlayer();
        
        base.OnNetworkSpawn();
    }

    void OnServerSpawnPlayer()
    {
        // this is done server side, so we have a single source of truth for our spawn point list
        var spawnPoint = ServerPlayerSpawnPoints.Instance.ConsumeNextSpawnPoint();
        var spawnPosition = spawnPoint ? spawnPoint.transform.position : Vector3.zero;
        transform.position = spawnPosition;
        
        // A note specific to owner authority:
        // Side Note:  Specific to Owner Authoritative
        // Setting the position works as and can be set in OnNetworkSpawn server-side unless there is a
        // CharacterController that is enabled by default on the authoritative side. With CharacterController, it
        // needs to be disabled by default (i.e. in Awake), the server applies the position (OnNetworkSpawn), and then
        // the owner of the NetworkObject should enable CharacterController during OnNetworkSpawn. Otherwise,
        // CharacterController will initialize itself with the initial position (before synchronization) and updates the
        // transform after synchronization with the initial position, thus overwriting the synchronized position.
    }

    [ServerRpc]
    public void PickupObjectServerRpc(ulong objToPickupID)
    {        
        NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objToPickupID, out var objectToPickup);
        if (objectToPickup == null || objectToPickup.transform.parent != null) return; // object already picked up, server authority says no

        if (objectToPickup.TryGetComponent(out NetworkObject networkObject) && networkObject.TrySetParent(transform))
        {
            m_PickedUpObject = networkObject;
            objectToPickup.transform.localPosition = m_LocalHeldPosition;
            objectToPickup.GetComponent<ServerIngredient>().ingredientDespawned += IngredientDespawned;
            isObjectPickedUp.Value = true;
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
            m_PickedUpObject.GetComponent<ServerIngredient>().ingredientDespawned -= IngredientDespawned;
            // can be null if enter drop zone while carrying
            m_PickedUpObject.transform.parent = null;
            m_PickedUpObject = null;
        }

        isObjectPickedUp.Value = false;
    }
    // DOC END HERE
}
