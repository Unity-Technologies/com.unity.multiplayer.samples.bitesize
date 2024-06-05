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
    [HideInInspector]    
    public NetworkVariable<IngredientType> currentIngredientType = new NetworkVariable<IngredientType>();

    public IngredientType IngredientType;

    public event Action ingredientDespawned;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            enabled = false;
            return;
        }
        else
        {
            currentIngredientType.Value = IngredientType;           
        }
    }

    public override void OnNetworkDespawn()
    {
        ingredientDespawned?.Invoke();
    }
}
