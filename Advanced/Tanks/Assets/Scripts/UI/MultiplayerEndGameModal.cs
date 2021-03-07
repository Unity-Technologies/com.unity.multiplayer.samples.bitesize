using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Tanks.Rules;
using Tanks.TankControllers;
using System.Collections.Generic;
using Tanks.Data;
using Tanks.Audio;
using Tanks.Networking;

namespace Tanks.UI
{
	/// <summary>
	/// Multiplayer end game modal - announces winner and awards
	/// </summary>
	public class MultiplayerEndGameModal : EndGameModal
	{
		#region UI

		[SerializeField]
		protected Text m_AwardText, m_AwardAmountText;

		[SerializeField]
		protected EndGameCountDown m_CountDown;

		[SerializeField]
		protected NumberDisplay m_NumberDisplay, m_CurrencyDisplay;
		[SerializeField]
		protected ParticleSystem m_RewardParticleSystem;

		[SerializeField]
		protected GameObject m_RewardParent;

		[SerializeField]
		protected LeaderboardUI m_Leaderboard;

		#endregion

		//Anim delay
		[SerializeField]
		protected float m_DelayBeforeCurrencyAnim = 2.0f;

		//Cached reference to game manager
		protected GameManager m_GameManager;

		//Cache reference to the local player tank
		protected TankManager m_LocalTank;


		private float m_DelayCounter = 0.0f;
		private bool m_IsDelaying = false, m_UsedEvent = false, m_HasAwardedCurrency = false;
		private int m_AwardCurrency, m_TotalCurrency = -1;

		//Shows the modal, starts the count down and calls Setup()
		public override void Show()
		{
			base.Show();
			m_RewardParent.SetActive(false);
			GetComponentInParent<Canvas>().worldCamera = Camera.main;
			m_CountDown.StartCountDown(RulesProcessor.s_EndGameTime, FinishedCountDown);
			Setup();
		}

		/// <summary>
		/// Setup this instance.
		/// </summary>
		protected virtual void Setup()
		{
			//Lazy load the game manager
			LazyLoad();
			
			//cache game settings
			GameSettings settings = GameSettings.s_Instance;
			
			//get game manager from settings - remember that the game manager will not have a rules processor on clients
			RulesProcessor rules = settings.mode.rulesProcessor;
			//ensure the rules processor knows about the rules processor
			rules.SetGameManager(m_GameManager);
			SetRulesProcessor(rules);

			m_LocalTank = m_GameManager.localPlayer;
			m_LocalTank.awardCurrencyChanged += AwardTheCurrencyAndUnsubscribe;

			m_Leaderboard.Setup(rules.GetLeaderboardElements());
			
			//If this is the server then the player rank is correct and the currency can be awarded immediately
			if (NetworkManager.s_IsServer)
			{
				AwardTheCurrency();
			}
			
			//if the local rank is still invalid then subscribe to event
			if (m_LocalTank.rank < 1)
			{
				m_LocalTank.rankChanged += SetupAwardDisplay;
				m_UsedEvent = true;
			}
			//otherwise just run the event
			else
			{
				SetupAwardDisplay();
			}	
		}

		/// <summary>
		/// Awards the currency - only once
		/// </summary>
		private void AwardTheCurrency()
		{
			if (!m_HasAwardedCurrency)
			{
				m_TotalCurrency = PlayerDataManager.s_Instance.currency;
				m_GameManager.AssignMoney();
				m_HasAwardedCurrency = true;
			}
		}

		/// <summary>
		/// Awards the currency and unsubscribe from the event
		/// </summary>
		private void AwardTheCurrencyAndUnsubscribe()
		{
			AwardTheCurrency();
			m_LocalTank.awardCurrencyChanged -= AwardTheCurrencyAndUnsubscribe;	
		}

		//Sets up the award display
		private void SetupAwardDisplay()
		{
			m_RewardParent.SetActive(true);
			m_AwardText.text = m_RulesProcessor.GetAwardText(m_LocalTank.rank);
			m_AwardCurrency = m_RulesProcessor.GetAwardAmount(m_LocalTank.rank);
			m_AwardAmountText.text = m_AwardCurrency.ToString();

			if (m_TotalCurrency < 0)
			{
				m_TotalCurrency = PlayerDataManager.s_Instance.currency;
			}
			
			m_CurrencyDisplay.GetComponent<Text>().text = m_TotalCurrency.ToString();
			m_IsDelaying = true;

			if (m_UsedEvent)
			{
				m_LocalTank.rankChanged -= SetupAwardDisplay;
			}
		}
		
		//Handles delayed amimation call
		private void Update()
		{
			if (m_IsDelaying)
			{
				m_DelayCounter += Time.deltaTime;
				if (m_DelayCounter >= m_DelayBeforeCurrencyAnim)
				{
					CurrencyAnimation();
					m_IsDelaying = false;
				}
			}
		}
		
		//Plays the currency animation - namely particles and the incremental number display
		private void CurrencyAnimation()
		{
			m_CurrencyDisplay.SetTargetValue(m_TotalCurrency, m_TotalCurrency + m_AwardCurrency, 1.5f);
			m_RewardParticleSystem.Play();

			UIAudioManager.s_Instance.PlayCoinSound();
		}

		//Fired by the countdown event
		private void FinishedCountDown()
		{
			LazyLoad();
			RulesProcessor rules = m_GameManager.rulesProcessor;
			AwardTheCurrency();
			if (rules != null)
			{
				rules.CompleteGame();
			}
		}

		//Lazy load the game manager
		private void LazyLoad()
		{
			if (m_GameManager == null)
			{
				m_GameManager = GameManager.s_Instance;
			}
		}
	}
}
