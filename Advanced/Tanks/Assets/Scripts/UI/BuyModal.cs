using UnityEngine;
using UnityEngine.UI;
using System;
using Tanks.Data;
using Tanks.Advertising;

namespace Tanks.UI
{
	/// <summary>
	/// This class governs the buy modal that is called by all items in the game that are purchaseable with in-game currency.
	/// Also includes deprecated Ad Unlock functionality, which was designed to allow players to unlock items temporarily as a trial by watching an ad.
	/// </summary>
	public class BuyModal : Modal
	{
		[SerializeField]
		//References to subpanels
		protected GameObject m_BuyPanel, m_InsufficientFundsPanel;

		[SerializeField]
		//References to dynamic prompt text
		protected Text m_Description, m_Cost, m_InsufficientFundsText;

		[SerializeField]
		//Deprecated references to buttons allowing Ad Unlock
		protected Button[] m_AdUnlockButtons;

		[SerializeField]
		protected string m_InsufficientFundsMessage = "You cannot afford {0} at {1} gears. Play more games to collect gears.";

		[SerializeField]
		//Deprecated internal reference to global Ad Unlock modal for temp unlock functionality.
		protected AdvertUnlockModal m_AdUnlockModal;

		private int m_CurrentCost;

		//Delegates to fire on successful purchase or unlock respectively.
		private event Action BuyAction;
		private event Action UnlockAction;

		/// <summary>
		/// Opens the buy modal.
		/// </summary>
		/// <param name="itemName">Item name to display.</param>
		/// <param name="itemCost">Item cost.</param>
		/// <param name="buyAction">Delegate to fire on successful purchase.</param>
		/// <param name="tempUnlockAction">DEPRECATED: Delegate to fire if temp ad unlock is successful.</param>
		public void OpenBuyModal(string itemName, int itemCost, Action buyAction, Action tempUnlockAction = null)
		{
			//Activate the root object, then enable/disable the relevant subpanel based on whether the player can afford the item.
			gameObject.SetActive(true);
			bool canPlayerAffordPurchase = PlayerDataManager.s_Instance.CanPlayerAffordPurchase(itemCost);
			m_InsufficientFundsPanel.SetActive(!canPlayerAffordPurchase);
			m_BuyPanel.SetActive(canPlayerAffordPurchase);

			//DEPRECATED: Activate the ad unlock button on both subpanels if possible and make interactive.
			for(int i = 0; i < m_AdUnlockButtons.Length; i++)
			{
				m_AdUnlockButtons[i].gameObject.SetActive(TanksAdvertController.s_Instance.AreAdsSupported());
				m_AdUnlockButtons[i].interactable = (!DailyUnlockManager.s_Instance.IsUnlockActive() && (tempUnlockAction != null));
			}
			//Assign the unlock delegate.
			UnlockAction = tempUnlockAction;

			if (canPlayerAffordPurchase)
			{
				if (m_Description != null)
				{
					m_Description.text = itemName;
				}
				if (m_Cost != null)
				{
					m_Cost.text = itemCost.ToString();
				}
				m_CurrentCost = itemCost;
				this.BuyAction = buyAction;
			}
			else
			{
				m_InsufficientFundsText.text = string.Format(m_InsufficientFundsMessage, itemName, itemCost);
			}
		}

		/// <summary>
		/// Tied to a button in the UI. Deducts currency and executes the successful buy delegate, then closes the modal.
		/// </summary>
		public void BuyItem()
		{
			PlayerDataManager.s_Instance.RemoveCurrency(m_CurrentCost); 

			if (BuyAction != null)
			{
				BuyAction();
			}

			CloseModal();
		}

		/// <summary>
		/// DEPRECATED: Tied to the ad unlock buttons to fire temp ad unlock functionality.
		/// </summary>
		public void OnAdToUnlockButtonClicked()
		{
			m_AdUnlockModal.ShowAdModal(TempUnlockItem, OnAdFailed);
		}

		//Delegate to the ad controller if ad is successful. Fires the unlock delegate we've been assigned.
		private void TempUnlockItem()
		{
			if(UnlockAction != null)
			{
				UnlockAction();
			}

			CloseModal();
		}

		//Delegate to the ad controller if ad fails.
		private void OnAdFailed()
		{
			Debug.Log("Ad playback failed.");
		}

		//Ensures that both subpanels are closed before formally closing modal.
		public override void CloseModal()
		{
			m_BuyPanel.SetActive(false);
			m_InsufficientFundsPanel.SetActive(false);

			base.CloseModal();
		}
	}
}