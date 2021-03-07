using UnityEngine;
using UnityEngine.UI;
using Tanks.Data;

namespace Tanks.UI
{
	/// <summary>
	/// Controls customization screen.
	/// </summary>
	public class LobbyCustomization : TankSelector
	{
		//References to description and cost text fields.
		[SerializeField]
		protected Text m_TankDescription, m_Cost;

		//Reference to parent node for tank locked overlay.
		[SerializeField]
		protected GameObject m_LockedPanel;

		//Reference to selection confirmation button.
		[SerializeField]
		protected Button m_OkButton;

		//Reference to input field for player name.
		[SerializeField]
		protected InputField m_PlayerNameInput;

		//Reference to modal to purchase tanks.
		[SerializeField]
		protected BuyModal m_BuyModal;

		//Reference to controller for decoration colour swatches.
		[SerializeField]
		protected SkinColourSelector m_SkinColourSelection;

		protected override void OnEnable()
		{
			base.OnEnable();

			if (PlayerDataManager.s_InstanceExists)
			{
				m_PlayerNameInput.text = PlayerDataManager.s_Instance.playerName;
			}

			RefreshColourSelector();
		}

		/// <summary>
		/// Designed for OK button. Saves selected tank and decorations to persistent data and returns to menu.
		/// </summary>
		public void OKButton()
		{
			PlayerDataManager dataManager = PlayerDataManager.s_Instance;
			if (dataManager != null)
			{
				dataManager.playerName = m_PlayerNameInput.text;
				dataManager.selectedTank = m_CurrentIndex;
				dataManager.selectedDecoration = m_CurrentDecoration;
				dataManager.SetSelectedMaterialForDecoration(m_CurrentDecoration, m_CurrentDecorationMaterial);
			}
			MainMenuUI.s_Instance.ShowDefaultPanel();
		}

		/// <summary>
		/// Designed for Back button. Returns to main menu without saving to persistent data, resets to last known settings.
		/// </summary>
		public void OnBackClicked()
		{
			MainMenuUI.s_Instance.ShowDefaultPanel();
			ResetSelections();
		}

		/// <summary>
		/// On Disable, set the visible tank to reflect whatever our persistent settings are, whether they've changed or not.
		/// </summary>
		private void OnDisable()
		{
			PlayerDataManager dataManager = PlayerDataManager.s_Instance;
			if (dataManager != null && TankRotator.s_InstanceExists)
			{
				TankRotator.s_Instance.LoadModelForTankIndex(dataManager.selectedTank);
				TankRotator.s_Instance.LoadDecorationForIndex(dataManager.selectedDecoration, dataManager.GetSelectedMaterialForDecoration(dataManager.selectedDecoration));
			}
		}

		/// <summary>
		/// Changes the star ratings for different tank statistics based on the tank index.
		/// </summary>
		/// <param name="index">Index of the selected tank in the TankLibrary.</param>
		protected override void UpdateTankStats(int index)
		{
			TankTypeDefinition tankData = TankLibrary.s_Instance.GetTankDataForIndex(index);

			m_TankDescription.text = tankData.description;
			m_Cost.text = tankData.cost.ToString();

			bool isLocked = (!PlayerDataManager.s_Instance.IsTankUnlocked(index) && !DailyUnlockManager.s_Instance.IsItemTempUnlocked(tankData.id));
			
			m_LockedPanel.SetActive(isLocked);
			m_OkButton.interactable = !isLocked;

			base.UpdateTankStats(index);
		}

		public void UnlockCurrentTank()
		{
			PlayerDataManager.s_Instance.SetTankUnlocked(m_CurrentIndex, true);
		}

		/// <summary>
		/// Brings up the Buy modal, which will determine whether the tank can be purchased with in-game currency.
		/// </summary>
		public void TryBuyCurrentTank()
		{
			TankTypeDefinition tankData = TankLibrary.s_Instance.GetTankDataForIndex(m_CurrentIndex);
			m_BuyModal.OpenBuyModal(tankData.name, tankData.cost, BuyCurrentTank, TempUnlockTank);
		}

		/// <summary>
		/// Unlocks the selected tank and selects it.
		/// </summary>
		public void BuyCurrentTank()
		{
			UnlockCurrentTank();
			UpdateTankStats(m_CurrentIndex);
		}

		/// <summary>
		/// DEPRECATED: Temporarily unlocks this tank in response to watching an advert.
		/// </summary>
		private void TempUnlockTank()
		{
			TankTypeDefinition tankData = TankLibrary.s_Instance.GetTankDataForIndex(m_CurrentIndex);
			DailyUnlockManager.s_Instance.SetDailyUnlock(tankData.id);
			UpdateTankStats(m_CurrentIndex);
		}

		public void RefreshColourSelector()
		{
			// Turn on the colour selection if it's off
			if (m_SkinColourSelection != null)
			{
				if (!m_SkinColourSelection.gameObject.activeSelf)
				{
					m_SkinColourSelection.gameObject.SetActive(true);
				}
				else
				{
					m_SkinColourSelection.RefreshAvailableColours();
				}
			}
		}
	}
}
