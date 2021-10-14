using System;
using System.Collections;
using System.Collections.Generic;
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
    max
}

public class ServerObjectWithIngredientType : SamNetworkBehaviour
{
    [SerializeField]
    public NetworkVariable<IngredientType> CurrentIngredientType;
}
