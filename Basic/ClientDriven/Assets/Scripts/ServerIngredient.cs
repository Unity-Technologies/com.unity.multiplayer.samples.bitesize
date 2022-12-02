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
    // NOTE: this is a workaround on an issue NGO-side. NetworkVariables should be able to be set pre-spawn.
    [SerializeField]
    IngredientType m_StartingIngredientType;
    
    public NetworkVariable<IngredientType> currentIngredientType;

    public event Action ingredientDespawned;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        currentIngredientType.Value = m_StartingIngredientType;
    }

    public override void OnNetworkDespawn()
    {
        ingredientDespawned?.Invoke();
    }
}
