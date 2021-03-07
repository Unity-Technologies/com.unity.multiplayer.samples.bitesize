using System.Collections.Generic;
using UnityEngine;
using Tanks.IAP;

namespace Tanks.UI
{
	/// <summary>
	/// Shop screen modal
	/// </summary>
	public class ShopScreen : Modal
	{
		#region Fields

		#if ENABLE_IAP
		[SerializeField]
		protected ShopItem m_ItemPrefab;
		#endif

		[SerializeField]
		protected Transform m_ItemParent;

		#endregion


		#region Unity Methods

		/// <summary>
		/// Populate the screen
		/// </summary>
		protected void OnEnable()
		{
			PopulateScreen();
		}

		/// <summary>
		/// Clear items from shop screen and unload resources
		/// </summary>
		protected void OnDisable()
		{
			foreach (Transform child in m_ItemParent)
			{
				Destroy(child.gameObject);
			}
		}

		#endregion


		#region Methods

		private void PopulateScreen()
		{
			#if ENABLE_IAP
			IAPManager iapManager = IAPManager.Instance;

			if (iapManager == null || !iapManager.Initialized)
			{
				return;
			}

			if (m_ItemPrefab != null && m_ItemParent != null)
			{
				List<IAPItem> availableItems = iapManager.AvailableItems;

				for (int i = 0; i < availableItems.Count; ++i)
				{
					IAPItem item = availableItems[i];
					ShopItem spawnedItem = Instantiate<ShopItem>(m_ItemPrefab);

					spawnedItem.ShowItem(item);
					spawnedItem.transform.SetParent(m_ItemParent, false);
				}
			}
			#endif
		}

		#endregion
	}
}