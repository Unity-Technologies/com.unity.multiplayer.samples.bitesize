using UnityEngine;
using System.Collections;
using Tanks.Rules.SinglePlayer.Objectives;
using UnityEngine.UI;

namespace Tanks.UI
{
	//UI element for displaying an objective
	public class ObjectiveUI : MonoBehaviour
	{
		[SerializeField]
		protected Text m_SummaryText, m_RewardText;
		[SerializeField]
		protected GameObject m_AchievedImage;

		//Sets up the various UI elements in the objective
		public void Setup(Objective objective, bool useSummary = true)
		{
			if (useSummary)
			{
				m_SummaryText.text = objective.objectiveSummary;
			}
			else
			{	
				m_SummaryText.text = objective.objectiveDescription;
			}

			if (m_RewardText != null && m_AchievedImage != null)
			{
				bool alreadyAchieved = objective.lockState == LockState.PreviouslyUnlocked;
				m_AchievedImage.SetActive(alreadyAchieved);
				m_RewardText.text = alreadyAchieved ? "0" : objective.currencyReward.ToString();
				m_RewardText.transform.parent.gameObject.SetActive(!alreadyAchieved);
			}
		}
	}
}
