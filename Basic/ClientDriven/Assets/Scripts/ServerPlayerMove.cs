using System;
using Unity.Netcode;
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

    public NetworkVariable<Vector3> spawnPosition;

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
        // Note: specific to Netcode for GameObjects v1.8.0 & v1.8.1
        // Previous versions of this sample utilizing Netcode for GameObjects <v1.8.0 had set the player's position at
        // this point. However, a regression with these two particular versions forced the new pattern for modifying a
        // player's initial position, which is:
        // we store the server-determined spawn position inside of a NetworkVariable, and have that be consumed by the
        // owning client, inside ClientDrivenNetworkTransform. This approach navigates potential OnNetworkSpawn race
        // conditions that popped up in these two Netcode versions, and is now a recommended approach for setting spawn
        // positions on OnNetworkSpawn for owner-authoritative NetworkTransforms.

        // this is done server side, so we have a single source of truth for our spawn point list
        var spawnPoint = ServerPlayerSpawnPoints.Instance.ConsumeNextSpawnPoint();
        spawnPosition.Value = spawnPoint != null ? spawnPoint.transform.position : Vector3.zero;

        // A note specific to owner authority (see the note above why this would not work for Netcode for GameObjects
        // v1.8.0 & 1.8.1):
        // Setting the position works as and can be set in OnNetworkSpawn server-side unless there is a
        // CharacterController that is enabled by default on the authoritative side. With CharacterController, it
        // needs to be disabled by default (i.e. in Awake), the server applies the position (OnNetworkSpawn), and then
        // the owner of the NetworkObject should enable CharacterController during OnNetworkSpawn. Otherwise,
        // CharacterController will initialize itself with the initial position (before synchronization) and updates the
        // transform after synchronization with the initial position, thus overwriting the synchronized position.
    }

    [Rpc(SendTo.Server)]
    public void ServerPickupObjectRpc(ulong objToPickupID)
    {
        NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objToPickupID, out var objectToPickup);
        if (objectToPickup == null || objectToPickup.transform.parent != null)
        {
            // object already picked up, server authority says no
            return;
        }

        if (objectToPickup.TrySetParent(transform))
        {
            m_PickedUpObject = objectToPickup;
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

    [Rpc(SendTo.Server)]
    public void ServerDropObjectRpc()
    {
        if (m_PickedUpObject != null)
        {
            m_PickedUpObject.GetComponent<ServerIngredient>().ingredientDespawned -= IngredientDespawned;
            // can be null if enter drop zone while carrying
            m_PickedUpObject.TrySetParent(parent: (Transform)null);
            m_PickedUpObject = null;
        }

        isObjectPickedUp.Value = false;
    }
    // DOC END HERE
}
