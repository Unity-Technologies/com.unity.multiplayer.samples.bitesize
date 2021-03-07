using UnityEngine;
using System.Collections;
using Tanks.TankControllers;

namespace Tanks.Pickups
{
	//This class defines the behaviour for the Nitro pickup.
	public class NitroPickup : PickupBase 
	{
		//The distance over which the Nitro is effective.
		[SerializeField]
		protected float m_EffectiveDistance = 25f;

		//The ratio to which tank speed is boosted while nitro is active.
		[SerializeField]
		protected float m_SpeedBoostRatio = 1.5f;

		//The ratio to which tank rotation rate is boosted while nitro is active.
		[SerializeField]
		protected float m_TurnBoostRatio = 1.2f;

		protected override void OnPickupCollected(GameObject targetTank)
		{
			targetTank.transform.GetComponentInParent<TankMovement>().SetMovementPowerupVariables(m_EffectiveDistance,m_SpeedBoostRatio,m_TurnBoostRatio);

			base.OnPickupCollected(targetTank);
		}
	}
}
