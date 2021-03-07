using UnityEngine;
using UnityEngine.UI;
using Tanks.Data;
using Tanks.Advertising;
using Tanks.Map;
using Tanks.Networking;
using Tanks.Rules;
using TanksNetworkPlayer = Tanks.Networking.NetworkPlayer;

namespace Tanks.UI
{
	/// <summary>
	/// Roulette modal - handles changing decoration, color and going to mini game
	/// </summary>
	public class RouletteModal : Modal
	{
		[SerializeField]
		protected Text m_CurrencyPrompt;

		[SerializeField]
		protected Button m_RollButton, m_UnlockButton;

		[SerializeField]
		protected LobbyCustomization m_CustomizationScreen;

		[SerializeField]
		protected Modal m_DecorationPanel;

		[SerializeField]
		protected SkinSelect m_SkinSelection;

		[SerializeField]
		protected SinglePlayerMapDetails m_ShootingRangeLevel;

		[SerializeField]
		protected AdvertUnlockModal m_AdUnlockModal;

		[SerializeField]
		protected int m_RollPrice = 100;

		private int m_PrizeIndex;
		private int m_PrizeColourIndex;

		public override void Show()
		{
			Show(-1, -1);
		}

		//Shows item of colour
		public void Show(int itemIndex, int itemColour)
		{
			base.Show();

			if (m_UnlockButton != null)
			{
				m_UnlockButton.interactable = false;
				m_UnlockButton.gameObject.SetActive(false);
			}

			if (itemIndex >= 0)
			{
				if (TanksAdvertController.s_Instance.AreAdsSupported())
				{
					m_UnlockButton.gameObject.SetActive(true);

					if (!DailyUnlockManager.s_Instance.IsUnlockActive())
					{
						if (m_UnlockButton != null)
						{
							m_UnlockButton.interactable = true;
						}
						m_PrizeIndex = itemIndex;
						m_PrizeColourIndex = itemColour;
					}
				}
			}
				
			m_CurrencyPrompt.text = m_RollPrice.ToString();

			bool canAffordRoll = PlayerDataManager.s_Instance.CanPlayerAffordPurchase(m_RollPrice);

			m_RollButton.interactable = canAffordRoll;
		}
		
		//Change the decoration
		public void ApplyChangesAndClose()
		{
			m_CustomizationScreen.ChangeDecoration(m_PrizeIndex);
			m_CustomizationScreen.ChangeCurrentDecorationColour(m_PrizeColourIndex);

			m_SkinSelection.RegenerateItems();

			m_CustomizationScreen.RefreshColourSelector();

			CloseModal();
		}
		
		//Goes to shooting range
		public void OnRollButtonClicked()
		{
			DisableInteractivity();
			PlayerDataManager.s_Instance.RemoveCurrency(m_RollPrice);

			GotoShootingRange();
		}

		//If you like prize and accept it
		public void OnAcceptButtonClicked()
		{
			UnlockPrizeDecoration();
			ApplyChangesAndClose();
		}

		//Go to shooting range
		private void GotoShootingRange()
		{
			m_DecorationPanel.CloseModal();
			CloseModal();

			LoadingModal loading = LoadingModal.s_Instance;
			if (loading != null)
			{
				loading.FadeIn();
			}

			GameSettings.s_Instance.SetupSinglePlayer(m_ShootingRangeLevel, new ModeDetails(m_ShootingRangeLevel.name, m_ShootingRangeLevel.description, m_ShootingRangeLevel.rulesProcessor));

			NetworkManager netManager = NetworkManager.s_Instance;
			netManager.playerJoined += StartGame;
			netManager.StartSinglePlayerMode(null);
		}
		
		//Start shooting range
		private void StartGame(TanksNetworkPlayer player)
		{
			NetworkManager netManager = NetworkManager.s_Instance;

			netManager.playerJoined -= StartGame;
			netManager.ProgressToGameScene();
		}

		//Advert code
		public void OnWatchAdvertClicked()
		{
			m_AdUnlockModal.ShowAdModal(UnlockTempItem, OnAdvertFailed);
		}

		//Claim prize
		private void UnlockPrizeDecoration()
		{
			if (!PlayerDataManager.s_Instance.IsDecorationUnlocked(m_PrizeIndex))
			{
				PlayerDataManager.s_Instance.SetDecorationUnlocked(m_PrizeIndex);
			}

			PlayerDataManager.s_Instance.SetDecorationColourUnlocked(m_PrizeIndex, m_PrizeColourIndex);
		}

		//Advert does not load
		private void OnAdvertFailed()
		{
			Debug.Log("Advert failure");
		}
		
		//Used for temporarily unlocking an item
		private void UnlockTempItem()
		{
			TankDecorationDefinition prizeItem = TankDecorationLibrary.s_Instance.GetDecorationForIndex(m_PrizeIndex);

			//If no colour index was specified for this item unlock, we select one at random.
			if (m_PrizeColourIndex < 0)
			{
				m_PrizeColourIndex = Random.Range(0, prizeItem.availableMaterials.Length);
			}

			DailyUnlockManager.s_Instance.SetDailyUnlock(prizeItem.id, m_PrizeColourIndex);

			ApplyChangesAndClose();
		}
	}
}
