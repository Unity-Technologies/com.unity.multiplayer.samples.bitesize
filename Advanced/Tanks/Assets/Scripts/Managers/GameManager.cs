using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MLAPI;
using MLAPI.Messaging;
using Tanks.TankControllers;
using Tanks.Pickups;
using Tanks.Data;
using Tanks.Rules;
using Tanks.UI;
using Tanks.Map;
using Tanks.Hazards;
using Tanks.Explosions;
using Tanks.Analytics;
using Tanks.Rules.SinglePlayer;
using Tanks.Networking;
using TanksNetworkManager = Tanks.Networking.NetworkManager;
using TanksNetworkPlayer = Tanks.Networking.NetworkPlayer;
using Tanks.Audio;
using System;
using MLAPI.NetworkVariable;
using MLAPI.Connection;

namespace Tanks
{
	/// <summary>
	/// Game state.
	/// </summary>
	public enum GameState
	{
		Inactive,
		TimedTransition,
		StartUp,
		Preplay,
		Preround,
		Playing,
		RoundEnd,
		EndGame,
		PostGame,
		EveryoneBailed
	}

	/// <summary>
	/// Game manager - handles game state and passes state to rules processor
	/// </summary>
	public class GameManager : NetworkBehaviour
	{
		//Singleton reference
		static public GameManager s_Instance;

		//This list is ordered descending by player score.
		static public List<TankManager> s_Tanks = new List<TankManager>();

		//The explosion manager prefab
		[SerializeField]
		protected ExplosionManager m_ExplosionManagerPrefab;

		//Reference to the prefab the players will control.
		[SerializeField]
		protected GameObject m_TankPrefab;

		//End game modal references - set up in editor
		[SerializeField]
		protected EndGameModal m_MultiplayerGameModal, m_DefaultSinglePlayerModal;

		//Editor reference to the KillLogPhrases scriptable object
		[SerializeField]
		protected KillLogPhrases m_KillLogPhrases;

		//This is the game object that end game modal is instantiated under
		[SerializeField]
		protected Transform m_EndGameUiParent;

		//The singler player HUD
		[SerializeField]
		protected HUDSinglePlayer m_SinglePlayerHud;

		//Prefab of the object that handles respawning of a tank
		[SerializeField]
		protected RespawningTank m_RespawningTankPrefab;

		[SerializeField]
		protected FadingGroup m_EndScreen;

		//Caching the persistent singleton of game settings
		protected GameSettings m_GameSettings;

		//Current game state - starts inactive
		protected GameState m_State = GameState.Inactive;
	
		//Getter of current game state
		public GameState state
		{
			get { return m_State; }
		}

		//Transition state variables
		private float m_TransitionTime = 0f;
		private GameState m_NextState;
		
		//synced variable for the game being finished
		[HideInInspector]
		protected NetworkVariableBool m_GameIsFinished = new NetworkVariableBool(false);

		//Various UI references to hide the screen between rounds.
		private FadingGroup m_LoadingScreen;

		//The local player
		private TankManager m_LocalPlayer;

		public TankManager localPlayer
		{
			get
			{
				return m_LocalPlayer;
			}
		}

		private int m_LocalPlayerNumber = 0;

		//Crate spawners
		private List<CrateSpawner> m_CrateSpawnerList;
		//Pickups
		private List<PickupBase> m_PowerupList;
		//Hazards
		private List<LevelHazard> m_HazardList;
		//if the tanks are active
		private bool m_HazardsActive;

		//The rules processor being used
		private RulesProcessor m_RulesProcessor;

		public RulesProcessor rulesProcessor
		{
			get { return m_RulesProcessor; }
		}

		//The end game modal that is actually used
		protected EndGameModal m_EndGameModal;

		public EndGameModal endGameModal
		{
			get
			{
				return m_EndGameModal;
			}
		}
		
		//Number of players in game
		private int m_NumberOfPlayers = 0;

		//if everyone bailing has been handled
		private bool m_AllBailHandled = false;
		
		//if everyone has bailed
		private bool m_HasEveryoneBailed = false;

		public bool hasEveryoneBailed
		{
			get
			{
				return m_HasEveryoneBailed;
			}
		}
		
		//The score display for multiplayer
		private HUDMultiplayerScore m_MpScoreDisplay;

		public HUDMultiplayerScore mpScoreDisplay
		{
			get
			{
				return m_MpScoreDisplay;
			}
		}
		
		//The modal displayed at the beginning of the game
		protected StartGameModal m_StartGameModal;

		//Cached network manager
		private TanksNetworkManager m_NetManager;

		//Round number. Non-round based games only have one round. Zero indexed
		private int m_Round = 0;

		//Cached reference to singleton InGameLeaderboardModal
		protected InGameLeaderboardModal m_Leaderboard;

		//Cached reference to singleton AnnouncerModal
		protected AnnouncerModal m_Announcer;

		//Dictionary used for reconciling score and color
		protected Dictionary<Color,int> m_ColorScoreDictionary = new Dictionary<Color, int>();

		public Dictionary<Color, int> colorScoreDictionary
		{
			get
			{
				return m_ColorScoreDictionary;
			}
		}

		#region Initialisation

		/// <summary>
		/// Unity message: Awake
		/// </summary>
		private void Awake()
		{
			//Sets up the singleton instance
			s_Instance = this;

			//Sets up the lists
			m_CrateSpawnerList = new List<CrateSpawner>();
			m_PowerupList = new List<PickupBase>();
			m_HazardList = new List<LevelHazard>();
    
			//Handles instantiating the endgamemodal
			InstantiateEndGameModal(m_MultiplayerGameModal); 

			//Cache the NetworkManager instance
			m_NetManager = TanksNetworkManager.s_Instance;

			//Subscribe to events on the Network Manager
			if (m_NetManager != null)
			{
				m_NetManager.clientDisconnected += OnDisconnect;
				m_NetManager.clientError += OnError;
				m_NetManager.serverError += OnError;
			}
		}

		/// <summary>
		/// Unity message: OnDestroy
		/// </summary>
		private void OnDestroy()
		{
			//Unsubscribe
			if (m_NetManager != null)
			{
				m_NetManager.clientDisconnected -= OnDisconnect;
				m_NetManager.clientError -= OnError;
				m_NetManager.serverError -= OnError;
			}

			s_Tanks.Clear();
		}

		//Cache the game setting
		private void SetGameSettings()
		{
			m_GameSettings = GameSettings.s_Instance;
		}

		/// <summary>
		/// Unity message: Start
		/// Only called on server
		/// </summary>
		private void Start()
		{
			if (!IsServer)
				return;

			//Set the state to startup
			m_State = GameState.StartUp;

			SetGameSettings();
			
			//Instantiate the rules processor
			m_RulesProcessor = Instantiate<RulesProcessor>(m_GameSettings.mode.rulesProcessor);
			m_RulesProcessor.SetGameManager(this);
			
			//Instantiate the explosion manager
			if (m_ExplosionManagerPrefab != null)
			{
				ExplosionManager explosionManager = Instantiate<ExplosionManager>(m_ExplosionManagerPrefab);

				explosionManager.NetworkObject.Spawn();
			}

			if (m_GameSettings.isSinglePlayer)
			{
				//Single player level has started
				AnalyticsHelper.SinglePlayerLevelStarted(m_GameSettings.map.id);
				//Set up single player modal
				SetupSinglePlayerModals();
			}
			else
			{
				//Multiplayer game has started
				AnalyticsHelper.MultiplayerGameStarted(m_GameSettings.map.id, m_GameSettings.mode.id, m_NetManager.playerCount);
			}
		}

		/// <summary>
		/// Setups the single player modals.
		/// </summary>
		private void SetupSinglePlayerModals()
		{
			//Cache the offline rules processor
			OfflineRulesProcessor offlineRulesProcessor = m_RulesProcessor as OfflineRulesProcessor;
			//Get the end game modal
			EndGameModal endGame = offlineRulesProcessor.endGameModal;

			//If an end game modal is not specified then use the default
			if (endGame == null)
			{
				endGame = m_DefaultSinglePlayerModal;
			}

			InstantiateEndGameModal(endGame);

			if (m_EndGameModal != null)
			{
				m_EndGameModal.SetRulesProcessor(m_RulesProcessor);
			}
			
			//Handle start game modal	
			if (offlineRulesProcessor.startGameModal != null)
			{
				m_StartGameModal = Instantiate(offlineRulesProcessor.startGameModal);
				m_StartGameModal.transform.SetParent(m_EndGameUiParent, false);
				m_StartGameModal.gameObject.SetActive(false);
				m_StartGameModal.Setup(offlineRulesProcessor);
				m_StartGameModal.Show();
				LazyLoadLoadingPanel();
				//The loading screen must always be the last sibling
				m_LoadingScreen.transform.SetAsLastSibling();
			}
		}

		/// <summary>
		/// Instantiates the end game modal.
		/// </summary>
		/// <param name="endGame">End game.</param>
		private void InstantiateEndGameModal(EndGameModal endGame)
		{
			if (endGame == null)
			{
				return;
			}

			if (m_EndGameModal != null)
			{
				Destroy(m_EndGameModal);
				m_EndGameModal = null;
			}

			m_EndGameModal = Instantiate<EndGameModal>(endGame);
			m_EndGameModal.transform.SetParent(m_EndGameUiParent, false);
			m_EndGameModal.gameObject.SetActive(false);
		}

		/// <summary>
		/// Add a tank from the lobby hook
		/// </summary>
		static public void AddTank(TankManager tank)
		{
			if (s_Tanks.IndexOf(tank) == -1)
			{
				s_Tanks.Add(tank);
				tank.MoveToSpawnLocation(SpawnManager.s_Instance.GetSpawnPointTransformByIndex(tank.playerNumber));
			}
		}

		/// <summary>
		/// Removes the tank.
		/// </summary>
		/// <param name="tank">Tank.</param>
		public void RemoveTank(TankManager tank)
		{
			Debug.Log("Removing tank");

			int tankIndex = s_Tanks.IndexOf(tank);

			if (tankIndex >= 0)
			{
				s_Tanks.RemoveAt(tankIndex);
				if (m_RulesProcessor != null)
				{
					m_RulesProcessor.TankDisconnected(tank);
				}

				m_NumberOfPlayers--;
			}

			if (s_Tanks.Count == 1 && !m_GameIsFinished.Value && !m_AllBailHandled)
			{
				HandleEveryoneBailed();
			}
		}

		#endregion

		/// <summary>
		/// Handles everyone bailed.
		/// </summary>
		public void HandleEveryoneBailed()
		{
			if (!TanksNetworkManager.s_IsServer)
			{
				return;
			}
			
			if (TanksNetworkManager.s_Instance.state != NetworkState.Inactive)
			{
				m_AllBailHandled = true;
				RpcDisplayEveryoneBailedClientRpc();
				m_HasEveryoneBailed = true;
				SetTimedTransition(GameState.EveryoneBailed, 3f);
			}
		}

		/// <summary>
		/// Rpcs the display everyone bailed.
		/// </summary>
		[ClientRpc]
		private void RpcDisplayEveryoneBailedClientRpc()
		{
			SetMessageText("GAME OVER", "Everyone left the game");
		}


		/// <summary>
		/// Exits the game.
		/// </summary>
		/// <param name="returnPage">Return page.</param>
		public void ExitGame(MenuPage returnPage)
		{	
			for (int i = 0; i < s_Tanks.Count; i++)
			{
				TankManager tank = s_Tanks[i];
				if (tank != null)
				{
					Debug.Log("Destroying tank!!!");
					TanksNetworkPlayer player = tank.player;
					if (player != null)
					{
						player.tank = null;
					}

					Destroy(s_Tanks[i].gameObject);
				}
			}

			s_Tanks.Clear();
			m_NetManager.ReturnToMenu(returnPage);
		}

		/// <summary>
		/// Convenience function wrapping the announcer modal
		/// </summary>
		/// <param name="heading">Heading.</param>
		/// <param name="body">Body.</param>
		private void SetMessageText(string heading, string body)
		{
			LazyLoadAnnouncer();
			m_Announcer.Show(heading, body);
		}

		/// <summary>
		/// Adds the crate spawner.
		/// </summary>
		/// <param name="newCrate">New crate.</param>
		public void AddCrateSpawner(CrateSpawner newCrate)
		{
			m_CrateSpawnerList.Add(newCrate);
		}

		/// <summary>
		/// Adds the powerup.
		/// </summary>
		/// <param name="powerUp">Power up.</param>
		public void AddPowerup(PickupBase powerUp)
		{
			m_PowerupList.Add(powerUp);
		}

		/// <summary>
		/// Removes the powerup.
		/// </summary>
		/// <param name="powerup">Powerup.</param>
		public void RemovePowerup(PickupBase powerup)
		{
			m_PowerupList.Remove(powerup);
		}

		/// <summary>
		/// Adds the hazard.
		/// </summary>
		/// <param name="hazard">Hazard.</param>
		public void AddHazard(LevelHazard hazard)
		{
			m_HazardList.Add(hazard);
		}

		/// <summary>
		/// Removes the hazard.
		/// </summary>
		/// <param name="hazard">Hazard.</param>
		public void RemoveHazard(LevelHazard hazard)
		{
			m_HazardList.Remove(hazard);
		}

		/// <summary>
		/// Gets the local player ID.
		/// </summary>
		/// <returns>The local player ID.</returns>
		public int GetLocalPlayerId()
		{
			return m_LocalPlayerNumber;
		}

		/// <summary>
		/// Unity message: Update
		/// Runs only on server
		/// </summary>
		protected void Update()
		{
			if (!IsServer)
				return;

			HandleStateMachine();		
		}

		/// <summary>
		/// Unity message: OnApplicationPause
		/// </summary>
		#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
		protected void OnApplicationPause(bool paused)
		{
			if (paused)
			{
				Time.timeScale = 1f;
				m_NetManager.DisconnectAndReturnToMenu();
			}
		}
		#endif

		#region STATE HANDLING

		/// <summary>
		/// Handles the state machine.
		/// </summary>
		protected void HandleStateMachine()
		{
			switch (m_State)
			{
				case GameState.StartUp:
					StartUp();
					break;
				case GameState.TimedTransition:
					TimedTransition();
					break;
				case GameState.Preplay:
					Preplay();
					break;
				case GameState.Playing:
					Playing();
					break;
				case GameState.RoundEnd:
					RoundEnd();
					break;
				case GameState.EndGame:
					EndGame();
					break;
				case GameState.EveryoneBailed:
					EveryoneBailed();
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// State up state function
		/// </summary>
		protected void StartUp()
		{
			if (m_GameSettings.isSinglePlayer)
			{
				LazyLoadLoadingPanel();
				m_LoadingScreen.StartFade(Fade.Out, 0.5f, SinglePlayerLoadedEvent);
				m_State = GameState.Inactive;
			}
			else
			{
				if (m_NetManager.AllPlayersReady())
				{
					m_State = GameState.Preplay;
					RpcInstantiateHudScoreClientRpc();
					RpcGameStartedClientRpc();

					// Reset all ready states for players again
					m_NetManager.ClearAllReadyStates();
				}
			}
		}

		protected void SinglePlayerLoadedEvent()
		{
			m_State = GameState.Preplay;
		}

		/// <summary>
		/// Time transition state function
		/// </summary>
		protected void TimedTransition()
		{
			m_TransitionTime -= Time.deltaTime;
			if (m_TransitionTime <= 0f)
			{
				m_State = m_NextState;
			}
		}

		/// <summary>
		/// Preplay state function
		/// </summary>
		protected void Preplay()
		{
			if (!m_RulesProcessor.canStartGame)
			{
				return;
			}

			RoundStarting();

			//notify clients that the round is now started, they should allow player to move.
			RpcRoundPlayingClientRpc();
		}

		/// <summary>
		/// Playing state function
		/// </summary>
		protected void Playing()
		{
			//We want to activate hazards the second we enter the gameplay loop, no earlier (to prevent bizarre premature hazard triggering due to rubberbanding on laggy connections).
			if (!m_HazardsActive)
			{
				ActivateHazards();
				m_HazardsActive = true;
			}

			if (m_RulesProcessor.IsEndOfRound())
			{
				m_State = GameState.RoundEnd;
			}
		}

		/// <summary>
		/// RoundEnd state function
		/// </summary>
		protected void RoundEnd()
		{
			if (m_CrateSpawnerList != null && m_CrateSpawnerList.Count != 0)
			{
				m_CrateSpawnerList[0].DeactivateSpawner();
			}

			m_RulesProcessor.HandleRoundEnd();

			if (m_RulesProcessor.matchOver)
			{
				SetTimedTransition(GameState.EndGame, 1f);
			}
			else
			{	
				//notify client they should disable tank control
				RpcRoundEndingClientRpc(m_RulesProcessor.GetRoundEndText());
				
				SetTimedTransition(GameState.Preplay, 2f);
			}
		}

		/// <summary>
		/// EndGame state function
		/// </summary>
		protected void EndGame()
		{
			// If there is a game winner, wait for certain amount or all player confirmed to start a game again
			m_GameIsFinished.Value = true;

			if (!m_GameSettings.isSinglePlayer)
			{
				//Ensure tanks are sorted correctly
				s_Tanks.Sort(TankSort);
				//Cache the length of the list
				int count = s_Tanks.Count;
				//iterate
				for (int i = 0; i < count; i++)
				{
					//Cache tank element
					TankManager tank = s_Tanks[i];
					//Set the rank - this will be the same for all non-team based games
					int rank = m_RulesProcessor.GetRank(i);
					tank.SetRank(rank);
					//Add currency - NB! this is based on rank
					tank.SetAwardCurrency(m_RulesProcessor.GetAwardAmount(rank));
				}
			}

			RpcGameEndClientRpc();

			m_RulesProcessor.MatchEnd();

			if (m_GameSettings.isSinglePlayer)
			{
				if (m_RulesProcessor.hasWinner)
				{
					AnalyticsHelper.SinglePlayerLevelCompleted(m_GameSettings.map.id, 3);
				}
				else
				{
					AnalyticsHelper.SinglePlayerLevelFailed(m_GameSettings.map.id);
				}
			}
			else
			{
				AnalyticsHelper.MultiplayerGameCompleted(m_GameSettings.map.id, m_GameSettings.mode.id, m_NumberOfPlayers, Mathf.RoundToInt(Time.timeSinceLevelLoad), m_RulesProcessor.winnerId);
			}

			m_State = GameState.PostGame;
		}

		/// <summary>
		/// EveryoneBailed state function
		/// </summary>
		protected void EveryoneBailed()
		{
			m_NetManager.DisconnectAndReturnToMenu();
			
			m_State = GameState.Inactive;
		}

		/// <summary>
		/// Sets the timed transition
		/// </summary>
		/// <param name="nextState">Next state</param>
		/// <param name="transitionTime">Transition time</param>
		protected void SetTimedTransition(GameState nextState, float transitionTime)
		{
			this.m_NextState = nextState;
			this.m_TransitionTime = transitionTime;
			m_State = GameState.TimedTransition;
		}

		#endregion

		/// <summary>
		/// Starts the round
		/// </summary>
		private void RoundStarting()
		{
			//we notify all clients that the round is starting
			m_RulesProcessor.StartRound();
			RpcRoundStartingClientRpc(m_GameSettings.isSinglePlayer);

			//Destroy any existing powerups
			CleanupPowerups();

			//Run round start reset code on all registered hazards
			ResetHazards();

			m_HazardsActive = false;

			if (m_CrateSpawnerList != null && m_CrateSpawnerList.Count != 0)
			{
				m_CrateSpawnerList[0].ActivateSpawner();
			}
		
			SetTimedTransition(GameState.Playing, 2f);
		}

		/// <summary>
		/// Cleanups the powerups
		/// </summary>
		private void CleanupPowerups()
		{
			for (int i = (m_PowerupList.Count - 1); i >= 0; i--)
			{
				if (m_PowerupList[i] != null)
				{
					Destroy(m_PowerupList[i].gameObject);
				}
			}
		}

		/// <summary>
		/// Resets the hazards
		/// </summary>
		private void ResetHazards()
		{
			for (int i = 0; i < m_HazardList.Count; i++)
			{
				m_HazardList[i].ResetHazard();
			}
		}

		/// <summary>
		/// Activates the hazards
		/// </summary>
		private void ActivateHazards()
		{
			for (int i = 0; i < m_HazardList.Count; i++)
			{
				m_HazardList[i].ActivateHazard();
			}
		}

		/// <summary>
		/// Rpc for game started
		/// </summary>
		[ClientRpc]
		void RpcGameStartedClientRpc()
		{
			if (PlayerDataManager.s_InstanceExists && PlayerDataManager.s_Instance.everyplayEnabled)
			{
				// Start recording!
				if (Everyplay.IsRecordingSupported())
				{
					SetGameSettings();
					Everyplay.StartRecording();
					if (m_GameSettings.mode != null)
					{
						Everyplay.SetMetadata("game_mode", m_GameSettings.mode.modeName);
					}
					if (m_GameSettings.map != null)
					{
						Everyplay.SetMetadata("level", m_GameSettings.map.name);
					}
				}
			}
		}

		/// <summary>
		/// Rpcs for round started
		/// </summary>
		/// <param name="isSinglePlayer">If set to <c>true</c> is single player</param>
		[ClientRpc]
		void RpcRoundStartingClientRpc(bool isSinglePlayer)
		{
			// As soon as the round starts reset the tanks and make sure they can't move
			if (m_Round == 0)
			{
				ResetAllTanks();
			}

			DisableTankControl();

			InitHudAndLocalPlayer();
			m_Round++;

			if (isSinglePlayer)
			{
				EnableHUD();
			}
			else
			{
				UIAudioManager.s_Instance.PlayRoundStartSound();

				LazyLoadLoadingPanel();
				m_LoadingScreen.StartFadeOrFireEvent(Fade.Out, 0.5f, EnableHUD);
			}
		}

		/// <summary>
		/// Enables the HUD
		/// </summary>
		void EnableHUD()
		{
			HUDController.s_Instance.SetHudEnabled(true);
		}

		/// <summary>
		/// Rpc for Round Playing
		/// </summary>
		[ClientRpc]
		void RpcRoundPlayingClientRpc()
		{
			// As soon as the round begins playing let the players control the tanks
			EnableTankControl();
			LazyLoadAnnouncer();
			m_Announcer.Hide();
		}

		/// <summary>
		/// Rpc for Round Ending
		/// </summary>
		/// <param name="winnerText">Winner text</param>
		[ClientRpc]
		private void RpcRoundEndingClientRpc(string winnerText)
		{
			HUDController.s_Instance.SetHudEnabled(false);
			DisableTankControl();
			m_EndScreen.StartFade(Fade.In, 2f, FadeOutEndRoundScreen);
			SetMessageText("ROUND END", winnerText);
		}

		/// <summary>
		/// Fades the out end round screen
		/// </summary>
		private void FadeOutEndRoundScreen()
		{
			m_EndScreen.StartFade(Fade.Out, 2f);
		}

		/// <summary>
		/// Rpc for Game End
		/// </summary>
		[ClientRpc]
		private void RpcGameEndClientRpc()
		{
			HUDController.s_Instance.SetHudEnabled(false);
			DisableTankControl();
			m_GameIsFinished.Value = true;
			if (m_EndGameModal != null)
			{
				m_EndGameModal.Show();
			}

			if (Everyplay.IsRecording())
			{
				int tankIndex = s_Tanks.IndexOf(m_LocalPlayer);
				if (tankIndex >= 0)
				{
					Everyplay.SetMetadata("final_position", tankIndex + 1);
				}
				Everyplay.StopRecording();
			}

			// Tell menu UI that we'll be returning to the lobby scene
			MainMenuUI.s_ReturnPage = MenuPage.Lobby;

			LazyLoadLoadingPanel();
			m_LoadingScreen.transform.SetAsLastSibling();
		}

		/// <summary>
		/// Assigns the money
		/// </summary>
		public void AssignMoney()
		{
			//Iterate through tanks and let them decide whether to assign their collected currency to the local player.
			for (int i = 0; i < s_Tanks.Count; i++)
			{
				s_Tanks[i].AssignMoneyToPlayerData();
			}
		}

		/// <summary>
		/// Handles the kill
		/// </summary>
		/// <param name="killed">Killed</param>
		public void HandleKill(TankManager killed)
		{
			TankManager killer = GetTankByPlayerNumber(killed.health.lastDamagedByPlayerNumber);
			string explosionId = killed.health.lastDamagedByExplosionId;
			if (killer != null)
			{
				if (killer.playerNumber == killed.playerNumber)
				{
					m_RulesProcessor.HandleSuicide(killer);
					if (m_GameSettings.isSinglePlayer)
					{
						AnalyticsHelper.SinglePlayerSuicide(m_GameSettings.map.id, explosionId);
					}
					else
					{
						RpcAnnounceKillClientRpc(m_KillLogPhrases.GetRandomSuicidePhrase(killer.playerName, killer.playerColor));
						AnalyticsHelper.MultiplayerSuicide(m_GameSettings.map.id, m_GameSettings.mode.id, killer.playerTankType.id, explosionId);
						HeatmapsHelper.MultiplayerSuicide(m_GameSettings.map.id, m_GameSettings.mode.id, killer.playerTankType.id, explosionId, killer.transform.position);
					}

				}
				else
				{
					m_RulesProcessor.HandleKillerScore(killer, killed);
					if (!m_GameSettings.isSinglePlayer)
					{
						RpcAnnounceKillClientRpc(m_KillLogPhrases.GetRandomKillPhrase(killer.playerName, killer.playerColor, killed.playerName, killed.playerColor));
						AnalyticsHelper.MultiplayerTankKilled(m_GameSettings.map.id, m_GameSettings.mode.id, killed.playerTankType.id, killer.playerTankType.id, explosionId);
						HeatmapsHelper.MultiplayerDeath(m_GameSettings.map.id, m_GameSettings.mode.id, killed.playerTankType.id, killer.playerTankType.id, explosionId, killed.transform.position);
						HeatmapsHelper.MultiplayerKill(m_GameSettings.map.id, m_GameSettings.mode.id, killed.playerTankType.id, killer.playerTankType.id, explosionId, killer.transform.position);
					}
				}
			}

			s_Tanks.Sort(TankSort);

			m_RulesProcessor.RegenerateHudScoreList();
		}

		/// <summary>
		/// Sort for tanks list
		/// </summary>
		/// <returns>The sort.</returns>
		/// <param name="tank1">Tank1</param>
		/// <param name="tank2">Tank2</param>
		private int TankSort(TankManager tank1, TankManager tank2)
		{
			return tank2.score - tank1.score;
		}

		/// <summary>
		/// Rpc wrapper for InGameNotificationManager
		/// </summary>
		/// <param name="msg">Message</param>
		[ClientRpc]
		private void RpcAnnounceKillClientRpc(string msg)
		{
			InGameNotificationManager.s_Instance.Notify(msg);
		}

		/// <summary>
		/// Gets the local player position
		/// </summary>
		/// <returns>The local player position</returns>
		public int GetLocalPlayerPosition()
		{
			return GetPlayerPosition(m_LocalPlayer);
		}

		/// <summary>
		/// Gets the player position
		/// </summary>
		/// <returns>The player position</returns>
		/// <param name="tank">Tank</param>
		public int GetPlayerPosition(TankManager tank)
		{
			if (!IsServer)
			{
				s_Tanks.Sort(TankSort);
			}

			int index = s_Tanks.IndexOf(tank);
			return index + 1;
		}

		/// <summary>
		/// Resets all the tanks on the server
		/// </summary>
		public void ServerResetAllTanks()
		{
			if (!IsServer)
				return;

			Debug.Log("ServerResetAllTanks");
			SpawnManager.s_Instance.CleanupSpawnPoints();
			for (int i = 0; i < s_Tanks.Count; i++)
			{
				RespawnTank(s_Tanks[i].playerNumber, false);
			}
		}

		// This function is used to turn all the tanks back on and reset their positions and properties
		private void ResetAllTanks()
		{
			for (int i = 0; i < s_Tanks.Count; i++)
			{
				s_Tanks[i].Reset(SpawnManager.s_Instance.GetSpawnPointTransformByIndex(s_Tanks[i].playerNumber));
			}
		}

		#region Respawn

		/// <summary>
		/// Respawns the tank
		/// </summary>
		/// <param name="playerNumber">Player number</param>
		/// <param name="showLeaderboard">If set to <c>true</c> show leaderboard</param>
		public void RespawnTank(int playerNumber, bool showLeaderboard = true)
		{
			if (!m_RulesProcessor.matchOver)
			{
				RpcRespawnTankClientRpc(playerNumber, showLeaderboard, SpawnManager.s_Instance.GetRandomEmptySpawnPointIndex());
			}
		}

		/// <summary>
		/// Rpc for respawning the tank
		/// </summary>
		/// <param name="playerNumber">Player number</param>
		/// <param name="showLeaderboard">If set to <c>true</c> show leaderboard</param>
		/// <param name="spawnPointIndex">Spawn point index</param>
		[ClientRpc]
		public void RpcRespawnTankClientRpc(int playerNumber, bool showLeaderboard, int spawnPointIndex)
		{
			TankManager tank = GetTankByPlayerNumber(playerNumber);

			if (tank == null)
			{
				return;
			}
            
			LocalRespawn(tank, showLeaderboard, SpawnManager.s_Instance.GetSpawnPointTransformByIndex(spawnPointIndex));
		}

		/// <summary>
		/// Locals the respawn
		/// </summary>
		/// <param name="tank">Tank</param>
		/// <param name="showLeaderboard">If set to <c>true</c> show leaderboard</param>
		/// <param name="respawnPoint">Respawn point</param>
		protected void LocalRespawn(TankManager tank, bool showLeaderboard, Transform respawnPoint)
		{
			RespawningTank respawningTank = Instantiate<RespawningTank>(m_RespawningTankPrefab);
			respawningTank.StartRespawnCycle(tank, this, showLeaderboard, respawnPoint);
		}

		#endregion

		/// <summary>
		/// Convenience function for showing the leaderboard
		/// </summary>
		/// <param name="tank">Tank</param>
		/// <param name="heading">Heading</param>
		public void ShowLeaderboard(TankManager tank, string heading)
		{
			if (tank != null && !tank.removedTank && tank.IsOwner && !m_GameIsFinished.Value)
			{
				LazyLoadLeaderboard();
				m_Leaderboard.Show(heading);
			}
		}

		/// <summary>
		/// Convenience function for hiding the leaderboard
		/// </summary>
		/// <param name="tank">Tank</param>
		public void ClearLeaderboard(TankManager tank)
		{
			if (!tank.removedTank && tank.IsOwner && !m_GameIsFinished.Value)
			{
				LazyLoadLeaderboard();
				m_Leaderboard.Hide();
			}
		}

		/// <summary>
		/// Clients the ready
		/// </summary>
		public void ClientReady()
		{
			m_NumberOfPlayers++;
		}

		/// <summary>
		/// Enables the tank control
		/// </summary>
		public void EnableTankControl()
		{
			for (int i = 0; i < s_Tanks.Count; i++)
			{
				s_Tanks[i].EnableControl();
			}
		}

		/// <summary>
		/// Disables the tank control
		/// </summary>
		public void DisableTankControl()
		{
			for (int i = 0; i < s_Tanks.Count; i++)
			{
				s_Tanks[i].DisableControl();
			}
		}

		//Iterates through all tankmanagers in the player list to determine which one represents the local player
		//Once the correct tank is found, pass its tankmanager reference to the HUD for init, and store its player number for reference by other scripts
		private void InitHudAndLocalPlayer()
		{
			for (int i = 0; i < s_Tanks.Count; i++)
			{
				if (s_Tanks[i].IsOwner)
				{
					m_LocalPlayer = s_Tanks[i];
					HUDController.s_Instance.InitHudPlayer(s_Tanks[i]);
					m_LocalPlayerNumber = s_Tanks[i].playerNumber;
				}
			}   
		}

		//Instantiates the multiplayer score tracker on this client's HUD, and stores a reference to its script for later update
		[ClientRpc]
		void RpcInstantiateHudScoreClientRpc()
		{
			m_MpScoreDisplay = HUDController.s_Instance.CreateScoreDisplay();
		}

		//Called by the current Rules Manager to update the multiplayer score on all clients via RPC
		//These colours and scores are preformatted by the rules manager to suit the game type
		public void UpdateHudScore(Color[] teamColours, int[] scores)
		{
			if (!m_GameSettings.isSinglePlayer)
			{
				// TODO: MLAPI doesn't support array serialization
				//RpcUpdateHudScoreClientRpc(teamColours, scores);
			}
		}

		// TODO: MLAPI doesn't support array serialization... boo :(
		////Fired to update multiplayer score display on this client, using the reference cached during client HUD instantiation
		//[ClientRpc]
		//void RpcUpdateHudScoreClientRpc(Color[] teamColours, int[] scores)
		//{
		//	m_MpScoreDisplay.UpdateScoreDisplay(teamColours, scores);
		//	if (scores.Length != teamColours.Length)
		//	{
		//		Debug.LogWarning("Score arrays different size");
		//		return;
		//	}

		//	UpdateScoreDictionary(teamColours, scores);
		//}

		/// <summary>
		/// Updates the score dictionary
		/// </summary>
		/// <param name="teamColours">Team colours</param>
		/// <param name="scores">Scores</param>
		void UpdateScoreDictionary(Color[] teamColours, int[] scores)
		{
			for (int i = 0; i < teamColours.Length; i++)
			{
				Color color = teamColours[i];
	
				if (m_ColorScoreDictionary.ContainsKey(color))
				{
					m_ColorScoreDictionary[color] = scores[i];
				}
				else
				{
					m_ColorScoreDictionary.Add(color, scores[i]);
				}
			}
		}

		/// <summary>
		/// Setups the single player HUD
		/// </summary>
		public void SetupSinglePlayerHud()
		{
			if (m_SinglePlayerHud == null)
			{
				return;
			}

			m_SinglePlayerHud.ShowHud(m_RulesProcessor);
		}

		/// <summary>
		/// Gets the tank by player number
		/// </summary>
		/// <returns>The tank by player number</returns>
		/// <param name="playerNumber">Player number</param>
		private TankManager GetTankByPlayerNumber(int playerNumber)
		{
			int length = s_Tanks.Count;
			for (int i = 0; i < length; i++)
			{
				TankManager tank = s_Tanks[i];
				if (tank.playerNumber == playerNumber)
				{
					return tank;
				}
			}

			Debug.LogWarning("Could NOT find tank!");
			return null;
		}

		#region Networking Issues Listeners

		/// <summary>
		/// Convenience function for showing error panel
		/// </summary>
		private void ShowErrorPanel()
		{
			TimedModal.s_Instance.SetupTimer(2f, m_NetManager.DisconnectAndReturnToMenu);
			TimedModal.s_Instance.Show();
		}

		/// <summary>
		/// Raised by disconnect event
		/// </summary>
		/// <param name="connection">Connection</param>
		private void OnDisconnect(ulong connection)
		{
			ShowErrorPanel();
		}

		/// <summary>
		/// Raised by error event
		/// </summary>
		/// <param name="connection">Connection</param>
		/// <param name="errorCode">Error code</param>
		private void OnError(ulong connection, int errorCode)
		{
			ShowErrorPanel();
		}

		#endregion

		#region Lazy Loaders

		/// <summary>
		/// Lazy loads the loading panel
		/// </summary>
		public void LazyLoadLoadingPanel()
		{
			if (m_LoadingScreen != null)
			{
				return;
			}

			m_LoadingScreen = LoadingModal.s_Instance.fader;
		}

		/// <summary>
		/// Lazy loads the leaderboard
		/// </summary>
		protected void LazyLoadLeaderboard()
		{
			if (m_Leaderboard != null)
			{
				return;
			}

			m_Leaderboard = InGameLeaderboardModal.s_Instance;
		}

		/// <summary>
		/// Lazy loads the announcer
		/// </summary>
		protected void LazyLoadAnnouncer()
		{
			if (m_Announcer != null)
			{
				return;
			}

			m_Announcer = AnnouncerModal.s_Instance;
		}

		#endregion
	}
}