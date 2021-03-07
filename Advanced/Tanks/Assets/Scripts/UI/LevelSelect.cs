using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Tanks.Map;
using Tanks.Networking;
using Tanks.Data;
using Tanks.Rules.SinglePlayer;
using Tanks.Rules;
using Tanks.Rules.SinglePlayer.Objectives;

namespace Tanks.UI
{
	/// <summary>
	/// Single player map selection menu screen control class.
	/// </summary>
	public class LevelSelect : Select
	{
		//Reference to ScriptableObject containing single player map list data.
		[SerializeField]
		protected SinglePlayerMapList m_MapList;

		//Default image to display if map has no preview.
		[SerializeField]
		protected Sprite m_NullImage;

		//Background image for the screen.
		[SerializeField]
		protected Image m_BgImage;

		//Background tint colours for when snow or desert maps are selected.
		[SerializeField]
		protected Color m_SnowBgColour, m_DesertBgColour;

		//Text object references for map descriptions and unlock-related information.
		[SerializeField]
		protected Text m_LevelName, m_Description, m_MedalCount, m_MedalRequirementText;

		//Reference to start button on screen.
		[SerializeField]
		protected Button m_StartGame;

		//Image for map preview.
		[SerializeField]
		protected Image m_MapPreview;

		//Parent transform to attach achievement badges to.
		[SerializeField]
		protected Transform m_AchievementsParent;

		//Prefab references for medal graphics, and reference to locked screen overlay.
		[SerializeField]
		protected GameObject m_PrimaryAchievedPrefab, m_AchievedPrefab, m_UnachievedPrefab, m_LockOverlay;

		private const string LOCKED_NAME = "LOCKED", LOCKED_RATING = "???", LOCKED_DESCRIPTION = "This level is locked - get {0} medals to unlock.", DEBUG_LEVEL = "SPTestScene1";

		//Internal references to netManager and menu controller.
		private NetworkManager m_NetManager;
		private MainMenuUI m_MenuUi;
    
		//Total number of medals earned.
		private int m_TotalMedalCount = 0;

		private void Awake()
		{
			m_ListLength = m_MapList.Count;
		}

		protected virtual void OnEnable()
		{
			//Get fresh references to controllers
			m_NetManager = NetworkManager.s_Instance;
			m_MenuUi = MainMenuUI.s_Instance;

			//Get current medals earned from player data and assign to the counter on screen.
			m_TotalMedalCount = PlayerDataManager.s_Instance.GetTotalMedalCount();
			m_MedalCount.text = m_TotalMedalCount.ToString();
		
			if (PlayerDataManager.s_Instance.lastLevelSelected < 0)
			{
				//If our last level selected is -1, scan for the first unlocked level without medals earned.
				//(This will be on first run and when returning from a completed mission).
				m_CurrentIndex = GetFirstUnplayedLevelIndex();
			}
			else
			{
				//Otherwise set the current index to the last mission we had selected.
				m_CurrentIndex = PlayerDataManager.s_Instance.lastLevelSelected;
			}

			AssignByIndex();
		}

		protected override void AssignByIndex()
		{
			//Set last selected level as current index.
			PlayerDataManager.s_Instance.lastLevelSelected = m_CurrentIndex;

			//Remove achievement medals from menu.
			ClearAchievements();

			//Get level data for the current index.
			SinglePlayerMapDetails details = m_MapList[m_CurrentIndex];

			//Get preview image for the level.
			SetImage(details.image);

			// Set BG depending on level biome.
			if (m_BgImage != null)
			{
				m_BgImage.color = details.effectsGroup == MapEffectsGroup.Snow ? m_SnowBgColour : m_DesertBgColour;
			}

			//If the required medal quantity to unlock this mission haven't been collected, the level is indicated as being locked and is not selectable.
			if (details.medalCountRequired > m_TotalMedalCount)
			{
				SetLevelName(LOCKED_NAME);
				m_Description.text = string.Format(LOCKED_DESCRIPTION, details.medalCountRequired);
				m_LockOverlay.SetActive(true);
				m_MedalRequirementText.text = details.medalCountRequired.ToString();
				m_StartGame.interactable = false;
				return;
			}

			//If not locked, populate all the level info on the screen.

			m_LockOverlay.gameObject.SetActive(false);
			SetLevelName(details.name);
			m_Description.text = details.description;
			m_StartGame.interactable = true;
			Objective[] objectives = (details.rulesProcessor as SinglePlayerRulesProcessor).objectives;

			SetAchievements(details.id, objectives);
		}

		private int GetFirstUnplayedLevelIndex()
		{
			//Assume first level by default.
			int returnLevel = 0;

			//Iterate through the map list from the beginning.
			for (int i = 0; i < m_MapList.Count; i++)
			{
				SinglePlayerMapDetails details = m_MapList[i];

				returnLevel = i;

				//If the player has earned enough medals to unlock the level...
				if ((details.medalCountRequired <= m_TotalMedalCount))
				{
					LevelData levelData = PlayerDataManager.s_Instance.GetLevelData(details.id);

					//And we haven't achieved/populated any of the objectives for this mission yet, break the loop so we return this level.
					if (levelData.objectivesAchieved.Count == 0)
					{
						break;
					}
					else
					{
						int totalCount = 0;

						for (int j = 0; j < levelData.objectivesAchieved.Count; j++)
						{
							if (levelData.objectivesAchieved[j] == true)
							{
								totalCount++;
							}
						}

						if (totalCount == 0)
						{
							break;
						}
					}
				}
				else
				{
					//Else if we don't have enough medals for this mission (which means we've parsed all viable missions up until now), select the previous mission in the list regardless of achievements.
					if (returnLevel < (m_MapList.Count - 1))
					{
						returnLevel--;
					}
					break;
				}
			}

			return returnLevel;
		}

		private void ClearAchievements()
		{
			for (int i = 0; i < m_AchievementsParent.childCount; i++)
			{
				Destroy(m_AchievementsParent.GetChild(i).gameObject);
			}
		}

		/// <summary>
		/// Instantiates the achievement medal graphics based on what has been accomplished in this mission.
		/// </summary>
		/// <param name="id">Unique level ID</param>
		/// <param name="objectives">Array of objectives for this mission</param>
		private void SetAchievements(string id, Objective[] objectives)
		{
			LevelData levelData = PlayerDataManager.s_Instance.GetLevelData(id);
			int numberOfObjectives = objectives.Length;
			//if we are yet to play this level we need to set the objective achieved list
			if (levelData.objectivesAchieved == null || levelData.objectivesAchieved.Count == 0)
			{
				levelData.objectivesAchieved = new List<bool>();
				for (int i = 0; i < numberOfObjectives; i++)
				{
					levelData.objectivesAchieved.Add(false);	
				}
			}

			int count = levelData.objectivesAchieved.Count;
			for (int i = 0; i < count; i++)
			{
				GameObject gO = null;
				if (levelData.objectivesAchieved[i])
				{
					gO = Instantiate(objectives[i].isPrimaryObjective ? m_PrimaryAchievedPrefab : m_AchievedPrefab);
				}
				else
				{
					gO = Instantiate(m_UnachievedPrefab);
				}

				gO.transform.SetParent(m_AchievementsParent, false);
			}
		}

		private void SetLevelName(string name)
		{
			m_LevelName.text = string.Format("{0}. {1}", m_CurrentIndex + 1, name).ToUpperInvariant();
		}

		private void SetImage(Sprite image)
		{
			if (image == null)
			{
				if (m_NullImage == null)
				{
					Debug.LogError("Missing NULL image");
					return;
				}
				m_MapPreview.sprite = image;
				Debug.LogWarning("MISSING IMAGE - SETTING TO NULL IMAGE");
				return;
			}
			m_MapPreview.sprite = image;
		}

		//Assigned to back button. Ends SP server session and leaves the SinglePlayer menu.
		public void OnBackClick()
		{
			m_NetManager.Disconnect();
			m_MenuUi.ShowDefaultPanel();
		}

		//Assigned to Start button. Loads up gamedata and begins the mission.
		public void OnStartClick()
		{
			SinglePlayerMapDetails details = m_MapList[m_CurrentIndex];
			if (details.medalCountRequired > m_TotalMedalCount)
			{
				return;
			}
			
			GameSettings settings = GameSettings.s_Instance;
			settings.SetupSinglePlayer(m_CurrentIndex, new ModeDetails(details.name, details.description, details.rulesProcessor));

			m_NetManager.ProgressToGameScene();
		}

		//Assigned to lock button. Shows an explanatory popup.
		public void OnLockClicked()
		{
			MainMenuUI.s_Instance.ShowInfoPopup("Earn more medals to unlock this level.", null);
		}
	}
}