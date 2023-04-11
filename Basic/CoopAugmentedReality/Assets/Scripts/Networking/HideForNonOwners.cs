using Unity.Netcode;
using UnityEngine;

public class HideForNonOwners : NetworkBehaviour 
{
    void Start()
    {
		Debug.Log($"TABLE IS OWNER {IsOwner}");

		// Only start as active if this is the owner.
		if (!IsOwner)
		{
			gameObject.SetActive(false);
		}
    }
}
