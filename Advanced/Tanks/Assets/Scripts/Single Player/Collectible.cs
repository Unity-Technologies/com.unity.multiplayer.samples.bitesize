using UnityEngine;
using System.Collections;
using Tanks.TankControllers;
using Tanks.Data;

namespace Tanks.SinglePlayer
{
	/// <summary>
	/// Collectible item in single player maps
	/// </summary>
	public class Collectible : TargetZone
	{
		[SerializeField]
		private GameObject collectionEffect;

		[SerializeField]
		private string pickupName;

		/// <summary>
		/// Handles triggering by checking that the zone object is a player and does the collection logic
		/// </summary>
		/// <param name="zoneObject">Zone object.</param>
		protected override void HandleTrigger(GameObject zoneObject)
		{
			TankDisplay playerTank = zoneObject.GetComponent<TankDisplay>();
			if (playerTank == null)
			{
				return;
			}

			if (collectionEffect != null)
			{
				zoneObject.GetComponentInParent<TankManager>().AddPickupName(pickupName);
				Instantiate(collectionEffect, transform.position + Vector3.up, Quaternion.LookRotation(Vector3.up));
			}
				
			Destroy(gameObject);
		}
	}
}
