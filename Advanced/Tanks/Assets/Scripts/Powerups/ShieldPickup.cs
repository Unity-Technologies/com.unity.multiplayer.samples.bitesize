using UnityEngine;
using System.Collections;
using Tanks.TankControllers;

namespace Tanks.Pickups
{
	//This class handles shield powerup collection logic.
	public class ShieldPickup : PickupBase 
	{
		//The number of shield points to increment to.
		[SerializeField]
		protected float m_ShieldHp = 25f;

		protected override void OnPickupCollected(GameObject targetTank)
		{
			targetTank.transform.GetComponentInParent<TankHealth>().SetShieldLevel(m_ShieldHp);

			base.OnPickupCollected(targetTank);
		}
	}
}