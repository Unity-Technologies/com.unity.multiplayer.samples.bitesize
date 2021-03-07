//IAP has been disabled
#if ENABLE_IAP

using UnityEngine;
using UnityEngine.UI;
using Tanks.IAP;

namespace Tanks.UI
{	
	//UI (view) for IAP item	
	public class ShopItem : MonoBehaviour
	{
#region Fields
		//UI references
		[SerializeField]
		protected RawImage sprite;
		[SerializeField]
		protected Text nameLabel;
		[SerializeField]
		protected Text costLabel;
#endregion


#region Properties
		public IAPItem displayedItem
		{
			get;
			protected set;
		}
#endregion


#region Method
		//Converts model (IAPItem) to view 
		public void ShowItem(IAPItem itemToShow)
		{
			displayedItem = itemToShow;

			nameLabel.text = itemToShow.Name;
			costLabel.text = itemToShow.PriceString;
			sprite.texture = itemToShow.GetThumbnail();
		}

		//Buy logic
		public void Buy()
		{
			IAPManager manager = IAPManager.Instance;

			if (manager != null)
			{
				manager.PurchaseItem(displayedItem.ItemID);
			}
		}
#endregion
	}
}

#endif