#if ENABLE_IAP

using System;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Purchasing;

using Tanks.Utilities;

namespace Tanks.IAP 
{
	public class IAPManager : Singleton<IAPManager>, IStoreListener
	{
#region Fields
		[SerializeField]
		protected IAPItemDefinition[] definitions;

		private IStoreController controller;
		private IAppleExtensions appleExtensions;

		private bool purchasing;
#endregion


#region Properties
		/// <summary>
		/// Gets whether IAP has been initialized
		/// </summary>
		public bool Initialized
		{
			get;
			protected set;
		}
		/// <summary>
		/// Gets a collection of all available items
		/// </summary>
		public List<IAPItem> AvailableItems
		{
			get;
			private set;
		}
#endregion


#region Unity Methods
		protected override void Awake()
		{
			base.Awake();
			GameObject.DontDestroyOnLoad(this);

#if ENABLE_IAP
			var module = StandardPurchasingModule.Instance();
			module.useFakeStoreUIMode = FakeStoreUIMode.Default;

			var builder = ConfigurationBuilder.Instance(module);
#if DEVELOPMENT_BUILD
			builder.Configure<IMicrosoftConfiguration>().useMockBillingSystem = true;
#endif

			PopulateProducts(builder);

			UnityPurchasing.Initialize(this, builder);
#endif
		}
#endregion


#region Methods
		public IAPItem GetItemByID(string id)
		{
			for (int i = 0; i < AvailableItems.Count; ++i)
			{
				IAPItem item = AvailableItems[i];
				if (item.ItemID == id)
				{
					return item;
				}
			}
			
			throw new InvalidOperationException("Cannot find an item with that ID");
		}


		public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
		{
			this.controller = controller;
			this.appleExtensions = extensions.GetExtension<IAppleExtensions>();

			// Populate available items list
			AvailableItems = new List<IAPItem>();

			for (int i = 0; i < definitions.Length; ++i)
			{
				IAPItemDefinition definition = definitions[i];
				Product storeProduct = controller.products.WithID(definition.baseID);
				if (storeProduct != null && storeProduct.availableToPurchase)
				{
					AvailableItems.Add(new IAPItem
					{
						definition = definition,
						internalProduct = storeProduct
					});
				}
			}

			Initialized = true;
		}

		public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
		{
			// Reset purchasing flag
			purchasing = false;

			Debug.Log("Purchased item " + e.purchasedProduct.definition.id);

			RewardItem(e.purchasedProduct.definition.id);

			return PurchaseProcessingResult.Complete;
		}

		public void OnPurchaseFailed(Product item, PurchaseFailureReason r)
		{
			purchasing = false;
		}

		public void OnInitializeFailed(InitializationFailureReason error)
		{
			Debug.LogError("Failed to init billing");

			switch (error)
			{
				case InitializationFailureReason.AppNotKnown:
					Debug.LogError("App missing?");
					break;
				case InitializationFailureReason.PurchasingUnavailable:
					Debug.Log("Billing disabled!");
					break;
				case InitializationFailureReason.NoProductsAvailable:
					Debug.Log("No products available for purchase");
					break;
			}
		}

		public void PurchaseItem(string id)
		{
			if (purchasing)
			{
				Debug.LogWarning("Trying to initiate multiple purchases at once");
				return;
			}

#if SKIP_IAP
			RewardItem(id);
#else
			purchasing = true;
			controller.InitiatePurchase(id);
#endif
		}

		private void RewardItem(string itemID)
		{
			IAPItem item = GetItemByID(itemID);
			item.Award();
		}

		private void PopulateProducts(ConfigurationBuilder builder)
		{
			foreach (IAPItemDefinition item in definitions)
			{
				builder.AddProduct(item.baseID, ProductType.Consumable, new IDs()
				{
					{item.macOSXID, MacAppStore.Name},
					{item.iosID, AppleAppStore.Name},
					{item.windowsID, WindowsStore.Name},
					{item.googleID, GooglePlay.Name}
				});
			}
		}
#endregion
	}
}

#endif
