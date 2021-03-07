using UnityEngine;
using Tanks.TankControllers;
using Tanks.SinglePlayer;
using Tanks.Rules.SinglePlayer.Objectives;
using Tanks.Data;
using Tanks.Networking;
using Tanks.Analytics;

namespace Tanks.Rules.SinglePlayer
{
	/// <summary>
	/// Single player rules processor - the base of all single player missions.
	/// </summary>
	public class SinglePlayerRulesProcessor : OfflineRulesProcessor
	{
		/// <summary>
		/// The objective prefabs used in configuration
		/// </summary>
		[SerializeField]
		protected Objective[] m_Objectives;

		/// <summary>
		/// The length of objectives array - cached for optimization
		/// </summary>
		protected int m_LengthOfObjectivesArray = 0;

		/// <summary>
		/// The LevelData persistence object associated with the single player rule processor instance
		/// </summary>
		protected LevelData m_LevelData;

		/// <summary>
		/// Has the single player HUD been setup?
		/// </summary>
		protected bool m_HasSetupHud = false;

		/// <summary>
		/// Gets a value indicating whether this <see cref="Tanks.Rules.SinglePlayer.SinglePlayerRulesProcessor"/> mission
		/// successfully completed.
		/// </summary>
		/// <value><c>true</c> if mission successfully completed; otherwise, <c>false</c>.</value>
		public bool missionSuccessfullyCompleted
		{
			get { return !m_MissionFailed && m_MatchOver; }
		}

		/// <summary>
		/// Gets Objectives
		/// </summary>
		public Objective[] objectives
		{
			get { return m_Objectives; }
		}

		protected GameSettings m_Settings;

		/// <summary>
		/// Gets the number of configured objectives.
		/// </summary>
		/// <value>The number of configured objectives.</value>
		public int numberOfObjectives
		{
			get { return m_Objectives.Length; }
		}

		/// <summary>
		/// The objective instances - the actual prefabs that have been instantiated.
		/// </summary>
		protected Objective[] m_ObjectiveInstances;

		/// <summary>
		/// Gets the objective instances.
		/// </summary>
		/// <value>The objective instances.</value>
		public Objective[] objectiveInstances
		{
			get
			{
				return m_ObjectiveInstances;
			}
		}

		/// <summary>
		/// Gets the round message - this is used by the GameManager to display information at the beginning of the game.
		/// </summary>
		/// <returns>The round message.</returns>
		public override string GetRoundMessage()
		{
			return m_Settings.map.name;
		}

		/// <summary>
		/// Called on MatchEnd - save if the mission is a success
		/// </summary>
		public override void MatchEnd()
		{
			if (!m_MissionFailed)
			{
				SaveObjectives();
			}	
		}

		/// <summary>
		/// Used to handle player death. There should only be the player tank
		/// </summary>
		/// <param name="tank">Tank.</param>
		public override void TankDies(TankManager tank)
		{
			if (m_MatchOver)
			{
				return;
			}

			base.TankDies(tank);
			m_MatchOver = true;
			m_Winner = null;
			m_MissionFailed = true;
			HeatmapsHelper.SinglePlayerDeath(m_Settings.map.id, tank.transform.position);
		}

		/// <summary>
		/// Convenience function for handling an NPC being destroyed in the single player level - passed to the objectives for the level
		/// </summary>
		public override void DestroyNpc(Npc npc)
		{
			for (int i = 0; i < m_LengthOfObjectivesArray; i++)
			{
				Objective objective = m_ObjectiveInstances[i];
				objective.DestroyNpc(npc);
			}
		}

		/// <summary>
		/// Convenience function for handling a game object (NPC/player) entering the zone - passed to the objectives for the level
		/// </summary>
		public override void EntersZone(GameObject zoneObject, TargetZone zone)
		{
			for (int i = 0; i < m_LengthOfObjectivesArray; i++)
			{
				Objective objective = m_ObjectiveInstances[i];
				objective.EntersZone(zoneObject, zone);
			}
		}

		/// <summary>
		/// Method for handling if an objective is achieved - called by the objective
		/// </summary>
		public void ObjectiveAchieved(Objective objective)
		{
			//End the game if the objective is a primary objective
			if (objective.isPrimaryObjective)
			{
				LazyLoadPlayerTank();
				m_MatchOver = true;
				m_Winner = m_PlayerTank;
				m_MissionFailed = false;	
			}
		}

		/// <summary>
		/// Method for handling if an objective is failed - called by the objective
		/// </summary>
		public void ObjectiveFailed(Objective objective)
		{
			//End the game if the objective is a primary objective
			if (objective.isPrimaryObjective)
			{
				m_MatchOver = true;
				m_Winner = null;
				m_MissionFailed = true;
			}
		}

		/// <summary>
		/// Saves the objective
		/// </summary>
		protected virtual void SaveObjectives()
		{   
			for (int i = 0; i < m_LengthOfObjectivesArray; i++)
			{
				Objective objective = m_ObjectiveInstances[i];
				if (objective.lockState == LockState.NewlyUnlocked)
				{
					m_LevelData.objectivesAchieved[i] = true;
				}
			}
            
			PlayerDataManager.s_Instance.SaveData();
		}

		/// <summary>
		/// Initial setup including instantiating the Objective instances, tying them up to the data persistence framework and sending the rule processor to the objective
		/// </summary>
		protected virtual void Awake()
		{
			bool levelDataChanged = false;
			m_Settings = GameSettings.s_Instance;
			string levelId = m_Settings.map.id;
			
			//Get the level data from the data store
			m_LevelData = PlayerDataManager.s_Instance.GetLevelData(levelId);
			int numberOfObjectivesInDataStore = m_LevelData.objectivesAchieved.Count;
			m_LengthOfObjectivesArray = m_Objectives.Length;
			m_ObjectiveInstances = new Objective[m_LengthOfObjectivesArray];

			//iterate through the list of objective prefabs and instantiate and setup objectives
			for (int i = 0; i < m_LengthOfObjectivesArray; i++)
			{
				Objective objective = Instantiate<Objective>(m_Objectives[i]);
				objective.transform.SetParent(transform);
				objective.SetRulesProcessor(this);
				m_ObjectiveInstances[i] = objective;
    
				//Has the objective been saved before?
				//No
				if (i >= numberOfObjectivesInDataStore)
				{
					m_LevelData.objectivesAchieved.Add(false);
					levelDataChanged = true;
				}
				//Yes
				else
				{
					bool hasBeenAchieved = m_LevelData.objectivesAchieved[i];
					//Has it previously been achieved
					if (hasBeenAchieved)
					{
						objective.SetPreviouslyUnlocked();
					}
				}
			}

			//Persist if there is new data
			if (levelDataChanged)
			{
				PlayerDataManager.s_Instance.SaveData();
			}          
		}

		/// <summary>
		/// Used for lazy setup of the HUD
		/// </summary>
		protected virtual void Update()
		{
			SetupHud();
		}

		/// <summary>
		/// Used for lazy setup of the HUD
		/// </summary>
		protected void SetupHud()
		{
			if (m_HasSetupHud)
			{
				return;
			}
            
			if (m_GameManager != null)
			{
				m_GameManager.SetupSinglePlayerHud();
				m_HasSetupHud = true;
			}
		}

		/// <summary>
		/// Logic fired on reset of game
		/// </summary>
		public override void ResetGame()
		{
			NetworkManager.s_Instance.ProgressToGameScene();
		}
	}
}