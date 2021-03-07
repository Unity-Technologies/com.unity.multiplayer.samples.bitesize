#if ENABLE_IAP

using System;
using UnityEngine;


using UnityEngine.Purchasing;

namespace Tanks.IAP
{
	[Serializable]
	public class IAPItemDefinition
	{
		[Header("Presentation & Data")]
		public string imagePath;
		public IAPContent content;
		[Header("Store IDs")]
		public string baseID;
		public string macOSXID;
		public string iosID;
		public string windowsID;
		public string googleID;
	}

	[Serializable]
	public struct IAPItem
	{
		public IAPItemDefinition definition;
		public Product internalProduct;

		public string CurrencyCode
		{
			get { return internalProduct.metadata.isoCurrencyCode; }
		}
		public string Description
		{
			get { return internalProduct.metadata.localizedDescription; }
		}
		public Decimal Price
		{
			get { return internalProduct.metadata.localizedPrice; }
		}
		public string PriceString
		{
			get { return internalProduct.metadata.localizedPriceString; }
		}
		public string Name
		{
			get { return internalProduct.metadata.localizedTitle; }
		}
		public string ItemID
		{
			get { return definition.baseID; }
		}

		public Texture2D GetThumbnail()
		{
			return Resources.Load<Texture2D>(definition.imagePath);
		}

		public void Award()
		{
			definition.content.Award();
		}
	}
}

#endif