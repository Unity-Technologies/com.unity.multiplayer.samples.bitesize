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
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            enabled = false;
            return;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

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

        ingredient.GetComponent<Rigidbody>().isKinematic = true;
        ingredient.transform.position = m_IngredientCookingLocation.position;
        StartCoroutine(StartCooking(ingredient));
    }

    IEnumerator StartCooking(ServerIngredient ingredient)
    {
        yield return new WaitForSeconds(m_CookingTime);

        ingredient.currentIngredientType.Value = currentIngredientType.Value;
        ingredient.GetComponent<Rigidbody>().isKinematic = false;
    }
}
