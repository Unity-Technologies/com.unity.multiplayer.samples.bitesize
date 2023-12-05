using System.Collections;
using UnityEngine;

public class ServerStove : ServerObjectWithIngredientType
{
    [SerializeField]
    int m_CookingTime = 1;

    [SerializeField]
    Transform m_IngredientCookingLocation;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            currentIngredientType.Value = IngredientType;
        }

        base.OnNetworkSpawn();

#if !NGO_DAMODE
        if (!IsServer)
        {
            enabled = false;
            return;
        }
#endif

    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsSpawned || !NetworkObject.HasAuthority)
        {
            return;
        }


        var ingredient = other.gameObject.GetComponent<ServerIngredient>();
        if (ingredient == null)
        {
            return;
        }

        if (ingredient.currentIngredientType.Value == currentIngredientType.Value)
        {
            return;
        }

        if (ingredient.transform.parent != null)
        {
            // already parented to player
            return;
        }

        if (ingredient.NetworkObject.HasAuthority)
        {
            ingredient.OnCookIngredient(currentIngredientType.Value, m_IngredientCookingLocation.position, m_CookingTime);
        }
        else
        {
            ingredient.CookIngedientServerRpc(currentIngredientType.Value, m_IngredientCookingLocation.position, m_CookingTime);
        }
    }
}
