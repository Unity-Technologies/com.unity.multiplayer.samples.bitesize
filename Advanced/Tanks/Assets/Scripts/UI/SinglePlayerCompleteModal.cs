using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Tanks.Rules.SinglePlayer.Objectives;
using System.Collections.Generic;
using Tanks.Rules.SinglePlayer;
using Tanks.Audio;
using Tanks.Data;

namespace Tanks.UI
{
	/// <summary>
	/// Script that sets up and controls the single player complete modal.
	/// </summary>
	public class SinglePlayerCompleteModal : OfflineEndGameModal
	{
		[SerializeField]
		/// <summary>
		/// The name of the achievement animator's animations we want to fire off.
		/// </summary>
		protected string m_AchieveAnimationName = "Achieve", m_AchievedAnimationName = "Achieved";
		[SerializeField]
		/// <summary>
		/// The achievement object prefab, used to display achievement data.
		/// </summary>
		protected SinglePlayerCompleteModalAchievement m_AchievementObjectsPrefab, m_PrimaryAchievementObjectsPrefab;
		[SerializeField]
		/// <summary>
		/// The achievement heading prefab, a simple gameobject with a Text component used as a heading in the list.
		/// </summary>
		protected GameObject m_AchievementHeadingPrefab;
		[SerializeField]
		/// <summary>
		/// The main panel of the modal, resized for failed mission state.
		/// </summary>
		protected RectTransform m_MainPanel;
		[SerializeField]
		/// <summary>
		/// The achievement objects parent, a grid layout. Used for parenting list and heading items.
		/// </summary>
		protected Transform m_AchievementObjectsParent;
		[SerializeField]
		/// <summary>
		/// The currency amount display, used to start the currency lerp
		/// </summary>
		protected NumberDisplay m_CurrencyAmountDisplay;
		[SerializeField]
		/// <summary>
		/// The currency display object, parent of the currencyAmountDisplay. Turned off during the failed sequence.
		/// </summary>
		protected GameObject m_CurrencyDisplayObject;
		[SerializeField]
		/// <summary>
		/// The height of the failed panel. Failed panel only diplays primary objective so needs to be shrunk.
		/// </summary>
		protected float m_FailedPanelHeight = 600;

		[SerializeField]
		/// <summary>
		/// Reference to the label of the continue button for manipulation based on outcome.
		/// </summary>
		protected Text m_ContinueButtonPrompt;
		[SerializeField]
		protected string m_ContinueButtonFailMessage = "MENU";

		[Header("Title messages")]
		[SerializeField]
		/// <summary>
		/// The text used for the modal heading.
		/// </summary>
		protected string m_SuccessText = "MISSION SUCCESSFUL";
		[SerializeField]
		protected string m_FailText = "MISSION FAILED";


		/// <summary>
		/// A flag used to ensure the modal has been set up.
		/// </summary>
		private bool m_HasBeenSetUp = false;
		/// <summary>
		/// A list of the achievements that have been achieved and thus need to be animated.
		/// </summary>
		private List<SinglePlayerCompleteModalAchievement> m_AchievementsToPlay;
		/// <summary>
		/// A local reference to the current single player rules processor.
		/// </summary>
		private SinglePlayerRulesProcessor m_SinglePlayerRulesProcessor;

		/// <summary>
		/// Sets up the modal using the objectives data.
		/// </summary>
		/// <param name="objectives">Objectives of the current single player mission.</param>
		public void SetUp(Objective[] objectives)
		{
			GetComponentInParent<Canvas>().worldCamera = Camera.main;
			int length = objectives.Length;

			m_AchievementsToPlay = new List<SinglePlayerCompleteModalAchievement>();

			bool success = m_SinglePlayerRulesProcessor.missionSuccessfullyCompleted;
			SetTitleText(success ? m_SuccessText : m_FailText);

			MusicManager.s_Instance.StopMusic();

			if (success)
			{
				//We reset the selected level count so that it will snap to the next incomplete level on return.
				PlayerDataManager.s_Instance.lastLevelSelected = -1;

				UIAudioManager.s_Instance.PlayVictorySound();
				m_CurrencyDisplayObject.SetActive(true);
				//create primary heading
				CreateHeading("PRIMARY");
			}
			else
			{
				UIAudioManager.s_Instance.PlayFailureSound();
				m_CurrencyDisplayObject.SetActive(false);
				m_ContinueButtonPrompt.text = m_ContinueButtonFailMessage;
			}
				
			m_CurrencyAmountDisplay.GetComponent<Text>().text = PlayerDataManager.s_Instance.currency.ToString();

			int currentCurrency = PlayerDataManager.s_Instance.currency;

			for (int i = 0; i < length; i++)
			{
				Objective objective = objectives[i];
				if (i == 1)
				{
					CreateHeading("SECONDARY");
				}

				SinglePlayerCompleteModalAchievement achievementObject = Instantiate(i == 0 ? m_PrimaryAchievementObjectsPrefab : m_AchievementObjectsPrefab) as SinglePlayerCompleteModalAchievement;
				achievementObject.transform.SetParent(m_AchievementObjectsParent, false);

				achievementObject.textbox.text = objective.objectiveDescription;

				if (!success)
				{
					achievementObject.SetToFailedState();
					Vector2 newPanelSize = m_MainPanel.sizeDelta;
					newPanelSize.y = m_FailedPanelHeight;
					m_MainPanel.sizeDelta = newPanelSize;
					break;
				}
				if (success && objective.lockState == LockState.NewlyUnlocked)
				{
					m_AchievementsToPlay.Add(achievementObject);
					achievementObject.SetUpCurrencyReward(objective.currencyReward, m_CurrencyAmountDisplay, currentCurrency);
					currentCurrency += objective.currencyReward;
				}
				else if (objective.lockState == LockState.PreviouslyUnlocked)
				{
					achievementObject.AlreadyAchieved();
					achievementObject.animator.Play(m_AchievedAnimationName);
				}
				else
				{
					achievementObject.SetCurrencyRewardText(objective.currencyReward);
				}
			}
			m_HasBeenSetUp = true;
		}

		/// <summary>
		/// Show this modal.
		/// </summary>
		public override void Show()
		{
			base.Show();
			m_SinglePlayerRulesProcessor = m_RulesProcessor as SinglePlayerRulesProcessor;
			if (m_SinglePlayerRulesProcessor != null)
			{
				SetUp(m_SinglePlayerRulesProcessor.objectiveInstances);
			}


			if (!m_HasBeenSetUp)
			{
				Debug.LogError("SPCompleteModal not set up!");
				return;
			}
			DoAchievement();
		}

		/// <summary>
		/// Starts the achievement achieved animation
		/// </summary>
		public void DoAchievement()
		{
			if (m_AchievementsToPlay.Count > 0)
			{
				m_AchievementsToPlay[0].animator.Play(m_AchieveAnimationName);
				m_AchievementsToPlay.RemoveAt(0);
			}
		}

		/// <summary>
		/// Creates a heading entry in the achievements list.
		/// </summary>
		private void CreateHeading(string text)
		{
			GameObject achievementHeading = Instantiate(m_AchievementHeadingPrefab);
			achievementHeading.GetComponentInChildren<Text>().text = text;
			achievementHeading.transform.SetParent(m_AchievementObjectsParent, false);
		}
	}
}
