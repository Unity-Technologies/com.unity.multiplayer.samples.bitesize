using System.Collections;
using UnityEngine;

public class ServerStove : ServerObjectWithIngredientType
{
    [SerializeField]
    private int m_CookingTime = 1;

    [SerializeField]
    private Transform m_IngredientCookingLocation;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            enabled = false;
            return;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        var ingredient = other.gameObject.GetComponent<ServerIngredient>();
        if (ingredient == null)
        {
            return;
        }

        if (ingredient.CurrentIngredientType.Value == CurrentIngredientType.Value)
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

    private IEnumerator StartCooking(ServerIngredient ingredient)
    {
        yield return new WaitForSeconds(m_CookingTime);

        ingredient.CurrentIngredientType.Value = CurrentIngredientType.Value;
        ingredient.GetComponent<Rigidbody>().isKinematic = false;
    }
}
