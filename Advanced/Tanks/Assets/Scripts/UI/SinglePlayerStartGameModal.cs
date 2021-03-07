using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Tanks.Rules.SinglePlayer;
using Tanks.Rules.SinglePlayer.Objectives;

namespace Tanks.UI
{
	/// <summary>
	/// Modal displayed at the beginning of a game
	/// </summary>
	public class SinglePlayerStartGameModal : StartGameModal
	{
		//Reference to the level heading
		[SerializeField]
		protected Text m_LevelName;
		//UI references
		[SerializeField]
		protected ObjectiveUI m_ObjectiveUiElement, m_PrimaryObjectiveUiElement;
		[SerializeField]
		protected GameObject m_AchievementHeadingPrefab;
		[SerializeField]
		protected Transform m_ObjectiveList;

		//Cached rules processor
		protected SinglePlayerRulesProcessor m_SinglePlayerRulesProcessor;

		public override void Setup(OfflineRulesProcessor rulesProcessor)
		{
			base.Setup(rulesProcessor);

			if (this.m_RulesProcessor != null)
			{
				m_SinglePlayerRulesProcessor = this.m_RulesProcessor as SinglePlayerRulesProcessor;

				Objective[] objectives = m_SinglePlayerRulesProcessor.objectiveInstances;

				int lengthOfArray = objectives.Length;
				//Display the objectives: Primary first then Secondary
				CreateHeading("PRIMARY");
				for (int i = 0; i < lengthOfArray; i++)
				{
					if (i == 1)
					{
						CreateHeading("SECONDARY");
					}
					ObjectiveUI newObjectiveUi = Instantiate<ObjectiveUI>(i == 0 ? m_PrimaryObjectiveUiElement : m_ObjectiveUiElement);
					newObjectiveUi.transform.SetParent(m_ObjectiveList, false);
					newObjectiveUi.Setup(objectives[i], false);
				}

				m_LevelName.text = m_SinglePlayerRulesProcessor.GetRoundMessage().ToUpper();
			}
		}

		//Insert Heading into objective list
		private void CreateHeading(string text)
		{
			GameObject achievementHeading = Instantiate(m_AchievementHeadingPrefab);
			achievementHeading.GetComponentInChildren<Text>().text = text;
			achievementHeading.transform.SetParent(m_ObjectiveList, false);
		}
	}
}
