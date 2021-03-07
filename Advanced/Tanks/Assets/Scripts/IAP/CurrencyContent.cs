using UnityEngine;
using Tanks.Data;

namespace Tanks.IAP
{
	/// <summary>
	/// Scriptable object to contain information about an IAP item
	/// </summary>
	[CreateAssetMenu(fileName = "CurrencyItem", menuName = "Currency IAP item", order = 1)]
	public class CurrencyContent : IAPContent
	{
		#region Fields
		[SerializeField]
		protected int m_Gears;
		#endregion


		/// <summary>
		/// Called when the user buys this item
		/// </summary>
		public override void Award()
		{
			PlayerDataManager playerData = PlayerDataManager.s_Instance;

			if (playerData != null)
			{
				playerData.AddCurrency(m_Gears);
			}
		}
	}
}