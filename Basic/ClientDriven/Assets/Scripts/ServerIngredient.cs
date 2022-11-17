using System;
using Unity.Netcode;
using UnityEngine;

public class ServerIngredient : ServerObjectWithIngredientType
{
}

public enum IngredientType
{
    Red,
    Blue,
    Purple,
    MAX // should be always last
}

public class ServerObjectWithIngredientType : NetworkBehaviour
{
    [SerializeField]
    public NetworkVariable<IngredientType> CurrentIngredientType;

    public event Action ingredientDespawned;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            enabled = false;
            return;
        }
    }

    public override void OnNetworkDespawn()
    {
        ingredientDespawned?.Invoke();
    }
}
