using System;
using StarterAssets;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Animations;

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
    Transform m_IngredientSocketTransform;

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
        NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objToPickupID, out var objectToPickup);
        if (objectToPickup == null || objectToPickup.transform.parent != null) return; // object already picked up, server authority says no

        if (objectToPickup.TryGetComponent(out NetworkObject networkObject) && networkObject.TrySetParent(transform))
        {
            var pickUpObjectRigidbody = objectToPickup.GetComponent<Rigidbody>();
            pickUpObjectRigidbody.isKinematic = true;
            pickUpObjectRigidbody.interpolation = RigidbodyInterpolation.None;
            objectToPickup.GetComponent<NetworkTransform>().InLocalSpace = true;
            objectToPickup.GetComponent<ServerIngredient>().ingredientDespawned += IngredientDespawned;
            isObjectPickedUp.Value = true;
            m_PickedUpObject = objectToPickup;
            var positionConstraint = objectToPickup.GetComponent<PositionConstraint>();
            positionConstraint.AddSource(new ConstraintSource()
            {
                sourceTransform = m_IngredientSocketTransform,
                weight = 1
            });
            positionConstraint.constraintActive = true;
        }
    }

    void IngredientDespawned()
    {
        DropIngredient(m_PickedUpObject.gameObject);
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
            var pickedUpObjectRigidbody = m_PickedUpObject.GetComponent<Rigidbody>();
            pickedUpObjectRigidbody.isKinematic = false;
            pickedUpObjectRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            m_PickedUpObject.GetComponent<NetworkTransform>().InLocalSpace = false;
            DropIngredient(m_PickedUpObject.gameObject);
            m_PickedUpObject = null;
        }

        isObjectPickedUp.Value = false;
    }

    void DropIngredient(GameObject ingredientToDrop)
    {
        var positionConstraint = ingredientToDrop.GetComponent<PositionConstraint>();
        positionConstraint.RemoveSource(0);
        positionConstraint.constraintActive = false;
        GetComponent<ThirdPersonController>().Holding = false;
    }
    // DOC END HERE
}
