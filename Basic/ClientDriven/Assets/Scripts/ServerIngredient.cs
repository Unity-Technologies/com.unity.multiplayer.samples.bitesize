using Unity.Netcode;
using Unity.Netcode.Samples;
using UnityEngine;

public class ServerIngredient : ServerObjectWithIngredientType
{
    protected override bool Both { get; } = true;
}

public enum IngredientType
{
    red,
    blue,
    purple,
    max // should be always last
}

public class ServerObjectWithIngredientType : ClientServerBaseNetworkBehaviour
{
    [SerializeField]
    public NetworkVariable<IngredientType> CurrentIngredientType;
}
