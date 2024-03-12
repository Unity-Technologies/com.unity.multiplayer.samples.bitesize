using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

public class NetworkObjectSpawner : NetworkBehaviour
{
    public NetworkObject prefabReference;
    Random m_RandomGenerator = new Random();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        if (IsServer)
        {
            var instantiatedNetworkObject = Instantiate(prefabReference, transform.position, transform.rotation, null);
            var ingredient = instantiatedNetworkObject.GetComponent<ServerIngredient>();
            ingredient.NetworkObject.Spawn();
            ingredient.currentIngredientType.Value = (IngredientType)m_RandomGenerator.Next((int)IngredientType.MAX);
            enabled = false;
        }
    }
}
