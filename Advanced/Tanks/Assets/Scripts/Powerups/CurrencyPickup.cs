using UnityEngine;
using Tanks.TankControllers;

namespace Tanks.Pickups
{
	//This class defines the currency pickup behaviour
	public class CurrencyPickup : PickupBase 
	{
		[SerializeField]
		protected int m_MinimumCurrency = 20;
		[SerializeField]
		protected int m_MaximumCurrency = 200;
		[SerializeField]
		protected int m_CurrencyIncrement = 5;

		//On pickup collection, assign a random currency bonus to the collecting tank.
		protected override void OnPickupCollected(GameObject targetTank)
		{
			//Calculate a random currency amount within bounds, rounded to the increment value specified.
			int currencyToGive = m_MinimumCurrency + Random.Range(0,((m_MaximumCurrency - m_MinimumCurrency)/m_CurrencyIncrement)) * m_CurrencyIncrement;

			m_PickupName = currencyToGive.ToString()+" Coins";

			targetTank.transform.GetComponentInParent<TankManager>().AddPickupCurrency(currencyToGive);

			base.OnPickupCollected(targetTank);
		}
	}
}
