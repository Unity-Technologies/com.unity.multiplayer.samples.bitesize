using Unity.Netcode;
using UnityEngine;

public class ServerIngredient : ServerObjectWithIngredientType
{
}

public enum IngredientType
{
    red,
    blue,
    purple,
    max // should be always last
}

public class ServerObjectWithIngredientType : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            enabled = false;
            return;
        }
    }

    [SerializeField]
    public NetworkVariable<IngredientType> CurrentIngredientType;
}
