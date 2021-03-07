using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Tanks.Map;
using Tanks.UI;
using MLAPI.Connection;
using MLAPI;
using MLAPI.SceneManagement;
using MLAPI.Transports.UNET;

namespace Tanks.Networking
{
	public enum SceneChangeMode
	{
		None,
		Game,
		Menu
	}

	public enum NetworkState
	{
		Inactive,
		Pregame,
		Connecting,
		InLobby,
		InGame
	}

	public enum NetworkGameType
	{
		Matchmaking,
		Direct,
		Singleplayer
	}

	public class NetworkManager : MLAPI.NetworkManager
	{
		#region Constants

		private static readonly string s_LobbySceneName = "LobbyScene";

		#endregion


		#region Events

		/// <summary>
		/// Called on all clients when a player joins
		/// </summary>
		public event Action<NetworkPlayer> playerJoined;
		/// <summary>
		/// Called on all clients when a player leaves
		/// </summary>
		public event Action<NetworkPlayer> playerLeft;

		private Action m_NextHostStartedCallback;

		/// <summary>
		/// Called on a host when their server starts
		/// </summary>
		public event Action hostStarted;
		/// <summary>
		/// Called when the server is shut down
		/// </summary>
		public event Action serverStopped;
		/// <summary>
		/// Called when the client is shut down
		/// </summary>
		public event Action clientStopped;
		/// <summary>
		/// Called on a client when they connect to a game
		/// </summary>
		public event Action<ulong> clientConnected;
		/// <summary>
		/// Called on a client when they disconnect from a game
		/// </summary>
		public event Action<ulong> clientDisconnected;
		/// <summary>
		/// Called on a client when there is a networking error
		/// </summary>
		public event Action<ulong, int> clientError;
		/// <summary>
		/// Called on the server when there is a networking error
		/// </summary>
		public event Action<ulong, int> serverError;
		/// <summary>
		/// Called on clients and server when the scene changes
		/// </summary>
		public event Action<bool, string> sceneChanged;
		/// <summary>
		/// Called on the server when all players are ready
		/// </summary>
		public event Action serverPlayersReadied;
		/// <summary>
		/// Called on the server when a client disconnects
		/// </summary>
		public event Action serverClientDisconnected;
		///// <summary>
		///// Called when we've created a match
		///// </summary>
		//public event Action<bool, MatchInfo> matchCreated;
		/// <summary>
		/// Called when game mode changes
		/// </summary>
		public event Action gameModeUpdated;

		//private Action<bool, MatchInfo> m_NextMatchCreatedCallback;

		///// <summary>
		///// Called when we've joined a matchMade game
		///// </summary>
		//public event Action<bool, MatchInfo> matchJoined;

		///// <summary>
		///// Called when we've been dropped from a matchMade game
		///// </summary>
		//public event Action matchDropped;

		//private Action<bool, MatchInfo> m_NextMatchJoinedCallback;

		#endregion


		#region Fields

		/// <summary>
		/// Maximum number of players in a multiplayer game
		/// </summary>
		[SerializeField]
		protected int m_MultiplayerMaxPlayers = 4;
		/// <summary>
		/// Prefab that is spawned for every connected player
		/// </summary>
		[SerializeField]
		protected NetworkPlayer m_NetworkPlayerPrefab;

		protected GameSettings m_Settings;

		private SceneChangeMode m_SceneChangeMode;

		SceneSwitchProgress m_SceneSwitchProgress;

		#endregion


		#region Properties

		/// <summary>
		/// Gets whether we're in a lobby or a game
		/// </summary>
		public NetworkState state
		{
			get;
			protected set;
		}

		/// <summary>
		/// Gets whether we're a multiplayer or single player game
		/// </summary>
		public NetworkGameType gameType
		{
			get;
			protected set;
		}

		/// <summary>
		/// Gets whether or not we're a server
		/// </summary>
		public static bool s_IsServer
		{
			get
			{
				return s_Instance.IsServer;
			}
		}

		/// <summary>
		/// Collection of all connected players
		/// </summary>
		public List<NetworkPlayer> connectedPlayers
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets current number of connected player
		/// </summary>
		public int playerCount
		{
			get
			{
				return connectedPlayers.Count;
			}
		}

		/// <summary>
		/// Gets whether we're playing in single player
		/// </summary>
		public bool isSinglePlayer
		{
			get
			{
				return gameType == NetworkGameType.Singleplayer;
			}
		}

		/// <summary>
		/// Gets whether we've currently got enough players to start a game
		/// </summary>
		public bool hasSufficientPlayers
		{
			get
			{
				return isSinglePlayer ? playerCount >= 1 : playerCount >= 2;
			}
		}

		#endregion

		#region Singleton

		/// <summary>
		/// Gets the NetworkManager instance if it exists
		/// </summary>
		public static NetworkManager s_Instance
		{
			get;
			protected set;
		}

		public static bool s_InstanceExists
		{
			get { return s_Instance != null; }
		}

		#endregion


		#region Unity Methods

		/// <summary>
		/// Initialize our singleton
		/// </summary>
		protected virtual void Awake()
		{
			if (s_Instance != null)
			{
				Destroy(gameObject);
			}
			else
			{
				s_Instance = this;

				connectedPlayers = new List<NetworkPlayer>();

				OnClientConnectedCallback += OnClientConnect;
				OnClientDisconnectCallback += OnClientDisconnect;

				NetworkConfig.ConnectionApproval = true;
				ConnectionApprovalCallback += ConnectionApprovalCheck;
				OnClientConnectedCallback += OnServerConnect;
				OnClientDisconnectCallback += OnServerDisconnect;

				NetworkSceneManager.OnSceneSwitched += OnServerSceneChanged;
				NetworkSceneManager.OnSceneSwitched += OnClientSceneChanged;
				OnServerStarted += OnStartServer;
				OnServerStarted += OnStartHost;

				OnClientConnectedCallback += OnServerAddPlayer;
				OnClientDisconnectCallback += OnServerRemovePlayer;
			}
		}

		protected virtual void Start()
		{
			m_Settings = GameSettings.s_Instance;
		}

		/// <summary>
		/// Progress to game scene when in transitioning state
		/// </summary>
		protected virtual void Update()
		{
			if (m_SceneChangeMode != SceneChangeMode.None)
			{
				LoadingModal modal = LoadingModal.s_Instance;

				bool ready = true;
				if (modal != null)
				{
					ready = modal.readyToTransition;

					if (!ready && modal.fader.currentFade == Fade.None)
					{
						modal.FadeIn();
					}
				}

				if (ready)
				{
					if (m_SceneChangeMode == SceneChangeMode.Menu)
					{
						if (state != NetworkState.Inactive)
						{
							MLAPI.SceneManagement.NetworkSceneManager.SwitchScene(s_LobbySceneName);
							if (gameType == NetworkGameType.Singleplayer)
							{
								state = NetworkState.Pregame;
							}
							else
							{
								state = NetworkState.InLobby;
							}
						}
						else
						{
							SceneManager.LoadScene(s_LobbySceneName);
						}
					}
					else
					{
						MapDetails map = GameSettings.s_Instance.map;

						m_SceneSwitchProgress = NetworkSceneManager.SwitchScene(map.sceneName);
						m_SceneSwitchProgress.OnComplete += OnAllClientsLoadedScene;

						state = NetworkState.InGame;
					}

					m_SceneChangeMode = SceneChangeMode.None;
				}
			}
		}

		/// <summary>
		/// Clear the singleton
		/// </summary>
		protected virtual void OnDestroy()
		{
			if (s_Instance == this)
			{
				s_Instance = null;
			}
		}

		protected void OnApplicationQuit()
		{
			if (IsHost)
			{
				StopHost();
			}
			else if(IsClient)
			{
				StopClient();
			}
			else if (IsServer)
			{
				StopServer();
			}
		}

		#endregion


		#region Methods

		/// <summary>
		/// Causes the network manager to disconnect
		/// </summary>
		public void Disconnect()
		{
			switch (gameType)
			{
				case NetworkGameType.Direct:
					StopDirectMultiplayerGame();
					break;
				case NetworkGameType.Matchmaking:
					StopMatchmakingGame();
					break;
				case NetworkGameType.Singleplayer:
					StopSingleplayerGame();
					break;
			}
		}

		/// <summary>
		/// Disconnect and return the game to the main menu scene
		/// </summary>
		public void DisconnectAndReturnToMenu()
		{
			Disconnect();
			ReturnToMenu(MenuPage.Home);
		}

		/// <summary>
		/// Initiate single player mode
		/// </summary>
		public void StartSinglePlayerMode(Action callback)
		{
			if (state != NetworkState.Inactive)
			{
				throw new InvalidOperationException("Network currently active. Disconnect first.");
			}

			// minPlayers = 1;
			// maxPlayers = 1;

			m_NextHostStartedCallback = callback;
			state = NetworkState.Pregame;
			gameType = NetworkGameType.Singleplayer;
			StartHost();
		}

		/// <summary>
		/// Initiate direct multiplayer mode
		/// </summary>
		public void StartMultiplayerServer(Action callback)
		{
			if (state != NetworkState.Inactive)
			{
				throw new InvalidOperationException("Network currently active. Disconnect first.");
			}

			// minPlayers = 2;
			// maxPlayers = multiplayerMaxPlayers;

			m_NextHostStartedCallback = callback;
			state = NetworkState.InLobby;
			gameType = NetworkGameType.Direct;
			StartHost();
		}

		/// <summary>
		/// Directly connect to a server by IP
		/// </summary>
		public void JoinMultiplayerGame(string address, int port)
		{
			if (NetworkConfig.NetworkTransport is UNetTransport transport)
			{
				transport.ConnectAddress = address;
				transport.ConnectPort = port;
				StartClient();
			}
		}

		///// <summary>
		///// Create a matchmaking game
		///// </summary>
		//public void StartMatchmakingGame(string gameName, Action<bool, MatchInfo> onCreate)
		//{
		//	if (state != NetworkState.Inactive)
		//	{
		//		throw new InvalidOperationException("Network currently active. Disconnect first.");
		//	}

		//	// minPlayers = 2;
		//	// maxPlayers = multiplayerMaxPlayers;

		//	state = NetworkState.Connecting;
		//	gameType = NetworkGameType.Matchmaking;

		//	StartMatchMaker();
		//	m_NextMatchCreatedCallback = onCreate;

		//	matchMaker.CreateMatch(gameName, (uint)m_MultiplayerMaxPlayers, true, string.Empty, string.Empty, string.Empty, 0, 0, OnMatchCreate);
		//}

		///// <summary>
		///// Initialize the matchmaking client to receive match lists
		///// </summary>
		//public void StartMatchingmakingClient()
		//{
		//	if (state != NetworkState.Inactive)
		//	{
		//		throw new InvalidOperationException("Network currently active. Disconnect first.");
		//	}

		//	// minPlayers = 2;
		//	// maxPlayers = multiplayerMaxPlayers;

		//	state = NetworkState.Pregame;
		//	gameType = NetworkGameType.Matchmaking;
		//	StartMatchMaker();
		//}

		///// <summary>
		///// Join a matchmaking game
		///// </summary>
		//public void JoinMatchmakingGame(NetworkID networkId, Action<bool, MatchInfo> onJoin)
		//{
		//	if (gameType != NetworkGameType.Matchmaking ||
		//	    state != NetworkState.Pregame)
		//	{
		//		throw new InvalidOperationException("Game not in matching state. Make sure you call StartMatchmakingClient first.");
		//	}

		//	state = NetworkState.Connecting;

		//	m_NextMatchJoinedCallback = onJoin;
		//	matchMaker.JoinMatch(networkId, string.Empty, string.Empty, string.Empty, 0, 0, OnMatchJoined);
		//}

		/// <summary>
		/// Makes the server change to the correct game scene for our map, and tells all clients to do the same
		/// </summary>
		public void ProgressToGameScene()
		{
			// Clear all client's ready states
			ClearAllReadyStates();

			// Remove us from matchmaking lists
			UnlistMatch();

			// Update will change scenes once loading screen is visible
			m_SceneChangeMode = SceneChangeMode.Game;

			// Tell NetworkPlayers to show their loading screens
			for (int i = 0; i < connectedPlayers.Count; ++i)
			{
				NetworkPlayer player = connectedPlayers[i];
				if (player != null)
				{
					player.RpcPrepareForLoadClientRpc();
				}
			}
		}

		/// <summary>
		/// Makes the server change to the menu scene, and bring all clients with it
		/// </summary>
		public void ReturnToMenu(MenuPage returnPage)
		{
			MainMenuUI.s_ReturnPage = returnPage;

			// Update will change scenes once loading screen is visible
			m_SceneChangeMode = SceneChangeMode.Menu;

			if (s_IsServer && state == NetworkState.InGame)
			{
				// Clear all client's ready states
				ClearAllReadyStates();

				// Tell NetworkPlayers to show their loading screens
				for (int i = 0; i < connectedPlayers.Count; ++i)
				{
					NetworkPlayer player = connectedPlayers[i];
					if (player != null)
					{
						player.RpcPrepareForLoadClientRpc();
					}
				}
			}
			else
			{
				// Show loading screen
				LoadingModal loading = LoadingModal.s_Instance;

				if (loading != null)
				{
					loading.FadeIn();
				}
			}
		}

		/// <summary>
		/// Gets the NetworkPlayer object for a given connection
		/// </summary>
		public static NetworkPlayer GetPlayerForConnection(ulong clientId)
		{
			return s_Instance.ConnectedClients[clientId].PlayerObject.GetComponent<NetworkPlayer>();
		}

		/// <summary>
		/// Gets a newwork player by its index
		/// </summary>
		public NetworkPlayer GetPlayerById(int id)
		{
			return connectedPlayers[id];
		}

		/// <summary>
		/// Gets whether all players are ready
		/// </summary>
		public bool AllPlayersReady()
		{
			if (!hasSufficientPlayers)
			{
				return false;
			}

			// Check all players
			for (int i = 0; i < connectedPlayers.Count; ++i)
			{
				if (!connectedPlayers[i].ready)
				{
					return false;
				}
			}

			return true;
		}


		/// <summary>
		/// Reset the ready states for all players
		/// </summary>
		public void ClearAllReadyStates()
		{
			for (int i = 0; i < connectedPlayers.Count; ++i)
			{
				NetworkPlayer player = connectedPlayers[i];
				if (player != null)
				{
					player.ClearReady();
				}
			}
		}

		protected void StopSingleplayerGame()
		{
			switch (state)
			{
				case NetworkState.InLobby:
					Debug.LogWarning("Single player game in lobby state. This should never happen");
					break;
				case NetworkState.Connecting:
				case NetworkState.Pregame:
				case NetworkState.InGame:
					StopHost();
					break;
			}

			state = NetworkState.Inactive;
		}

		protected void StopDirectMultiplayerGame()
		{
			switch (state)
			{
				case NetworkState.Connecting:
				case NetworkState.InLobby:
				case NetworkState.InGame:
					if (s_IsServer)
					{
						StopHost();
					}
					else
					{
						StopClient();
					}
					break;
			}

			state = NetworkState.Inactive;
		}

		protected void StopMatchmakingGame()
		{
			//switch (state)
			//{
			//	case NetworkState.Pregame:
			//		if (s_IsServer)
			//		{
			//			Debug.LogError("Server should never be in this state.");
			//		}
			//		else
			//		{
			//			StopMatchMaker();
			//		}
			//		break;

			//	case NetworkState.Connecting:
			//		if (s_IsServer)
			//		{
			//			StopMatchMaker();
			//			StopHost();
			//			matchInfo = null;
			//		}
			//		else
			//		{
			//			StopMatchMaker();
			//			StopClient();
			//			matchInfo = null;
			//		}
			//		break;

			//	case NetworkState.InLobby:
			//	case NetworkState.InGame:
			//		if (s_IsServer)
			//		{
			//			if (matchMaker != null && matchInfo != null)
			//			{
			//				matchMaker.DestroyMatch(matchInfo.networkId, 0, (success, info) =>
			//					{
			//						if (!success)
			//						{
			//							Debug.LogErrorFormat("Failed to terminate matchmaking game. {0}", info);
			//						}
			//						StopMatchMaker();
			//						StopHost();

			//						matchInfo = null;
			//					});
			//			}
			//			else
			//			{
			//				Debug.LogWarning("No matchmaker or matchInfo despite being a server in matchmaking state.");

			//				StopMatchMaker();
			//				StopHost();
			//				matchInfo = null;
			//			}
			//		}
			//		else
			//		{
			//			if (matchMaker != null && matchInfo != null)
			//			{
			//				matchMaker.DropConnection(matchInfo.networkId, matchInfo.nodeId, 0, (success, info) =>
			//					{
			//						if (!success)
			//						{
			//							Debug.LogErrorFormat("Failed to disconnect from matchmaking game. {0}", info);
			//						}
			//						StopMatchMaker();
			//						StopClient();
			//						matchInfo = null;
			//					});
			//			}
			//			else
			//			{
			//				Debug.LogWarning("No matchmaker or matchInfo despite being a client in matchmaking state.");

			//				StopMatchMaker();
			//				StopClient();
			//				matchInfo = null;
			//			}
			//		}
			//		break;
			//}

			state = NetworkState.Inactive;
		}


		/// <summary>
		/// Sets the current matchmaking game as unlisted
		/// </summary>
		protected void UnlistMatch()
		{
			//if (gameType == NetworkGameType.Matchmaking &&
			//    matchMaker != null)
			//{
			//	matchMaker.SetMatchAttributes(matchInfo.networkId, false, 0, (success, info) => Debug.Log("Match hidden"));
			//}
		}


		/// <summary>
		/// Causes the current matchmaking game to become listed again
		/// </summary>
		protected void ListMatch()
		{
			//if (gameType == NetworkGameType.Matchmaking &&
			//    matchMaker != null)
			//{
			//	matchMaker.SetMatchAttributes(matchInfo.networkId, true, 0, (success, info) => Debug.Log("Match shown"));
			//}
		}


		protected virtual void UpdatePlayerIDs()
		{
			for (int i = 0; i < connectedPlayers.Count; ++i)
			{
				connectedPlayers[i].SetPlayerId(i);
			}
		}


		protected void FireGameModeUpdated()
		{
			if (gameModeUpdated != null)
			{
				gameModeUpdated();
			}
		}


		/// <summary>
		/// Register network players so we have all of them
		/// </summary>
		public void RegisterNetworkPlayer(NetworkPlayer newPlayer)
		{
			MapDetails currentMap = m_Settings.map;
			Debug.Log("Player joined");

			connectedPlayers.Add(newPlayer);
			newPlayer.becameReady += OnPlayerSetReady;

			if (s_IsServer)
			{
				UpdatePlayerIDs();
			}

			// Send initial scene message
			string sceneName = SceneManager.GetActiveScene().name;
			if (currentMap != null && sceneName == currentMap.sceneName)
			{
				newPlayer.OnEnterGameScene();
			}
			else if (sceneName == s_LobbySceneName)
			{
				newPlayer.OnEnterLobbyScene();
			}

			if (playerJoined != null)
			{
				playerJoined(newPlayer);
			}

			newPlayer.gameDetailsReady += FireGameModeUpdated;
		}


		/// <summary>
		/// Deregister network players
		/// </summary>
		public void DeregisterNetworkPlayer(NetworkPlayer removedPlayer)
		{
			Debug.Log("Player left");
			int index = connectedPlayers.IndexOf(removedPlayer);

			if (index >= 0)
			{
				connectedPlayers.RemoveAt(index);
			}

			if (IsServer)
			{
				UpdatePlayerIDs();
			}

			if (playerLeft != null)
			{
				playerLeft(removedPlayer);
			}

			removedPlayer.gameDetailsReady -= FireGameModeUpdated;

			if (removedPlayer != null)
			{
				removedPlayer.becameReady -= OnPlayerSetReady;
			}
		}

		#endregion


		#region Networking events

		// TODO: Handle in MLAPI...
		//public override void OnClientError(NetworkConnection conn, int errorCode)
		//{
		//	Debug.Log("OnClientError");

		//	base.OnClientError(conn, errorCode);

		//	if (clientError != null)
		//	{
		//		clientError(conn, errorCode);
		//	}
		//}

		/// <summary>
		/// Called on the client when connected to a server.
		///
		/// The default implementation of this function sets the client as ready and adds a player. Override the function to dictate what happens when the client connects.
		/// </summary>
		private void OnClientConnect(ulong conn)
		{
			if (!IsClient || conn != LocalClientId)
				return;

			Debug.Log("OnClientConnect");

			//ClientScene.Ready(conn);
			//ClientScene.AddPlayer(0);

			if (clientConnected != null)
			{
				clientConnected(conn);
			}
		}

		/// <summary>
		/// Called on clients when disconnected from a server.
		///
		/// This is called on the client when it disconnects from the server. Override this function to decide what happens when the client disconnects.
		/// </summary>
		private void OnClientDisconnect(ulong conn)
		{
			if (!IsClient || conn != LocalClientId)
				return;

			Debug.Log("OnClientDisconnect");

			if (clientDisconnected != null)
			{
				clientDisconnected(conn);
			}
		}

		// TODO: Handle in MLAPI...
		//public override void OnServerError(NetworkConnection conn, int errorCode)
		//{
		//	Debug.Log("OnClientDisconnect");

		//	base.OnClientDisconnect(conn);

		//	if (serverError != null)
		//	{
		//		serverError(conn, errorCode);
		//	}
		//}

		public void OnServerSceneChanged()
		{
			if (!IsServer)
				return;

			Debug.Log("OnServerSceneChanged");

			if (sceneChanged != null)
			{
				// TODO: MLAPI???
				//sceneChanged(true, sceneName);
				sceneChanged(true, "");
			}

			// TODO: MLAPI???
			//if (sceneName == s_LobbySceneName)
			//{
			//	// Restore us to the matchmaking list
			//	ListMatch();

			//	// Reset this to prevent new clients from changing scenes when joining
			//	networkSceneName = string.Empty;
			//}
		}

		void OnAllClientsLoadedScene(bool _)
		{
			OnSceneChanged();
		}

		/// <summary>
		/// Called on clients when a Scene has completed loaded, when the Scene load was initiated by the server.
		///
		/// Scene changes can cause player objects to be destroyed. The default implementation of OnClientSceneChanged in the NetworkManager is to add a player object for the connection if no player object exists.
		/// </summary>
		public void OnClientSceneChanged()
		{
			// If we're the host, we'll call OnSceneChanged via the scenemanager all clients loaded callback, so don't double-call it here
			if (IsClient && !IsHost)
			{
				OnSceneChanged();
			}
		}

		public void OnSceneChanged()
		{
			if (!IsClient)
				return;

			if (IsHost && m_SceneSwitchProgress?.IsAllClientsDoneLoading == false)
				return;

			MapDetails currentMap = m_Settings.map;
			Debug.Log("OnClientSceneChanged");

			// TODO: Why would UNET care who triggered the scene change and bail if it's not the local player? Maybe players can scene change themselves individually?
			// UPDATE: I think this is UNET's way of saying "only the host client should do this"? - MLAPI has a check for that which is at the top of this method now
			//PlayerController pc = conn.playerControllers[0];

			//if (!pc.unetView.isLocalPlayer)
			//{
			//	return;
			//}

			string sceneName = SceneManager.GetActiveScene().name;

			if (currentMap != null && sceneName == currentMap.sceneName)
			{
				state = NetworkState.InGame;

				// Tell all network players that they're in the game scene
				for (int i = 0; i < connectedPlayers.Count; ++i)
				{
					NetworkPlayer np = connectedPlayers[i];
					if (np != null)
					{
						np.OnEnterGameScene();
					}
				}
			}
			else if (sceneName == s_LobbySceneName)
			{
				if (state != NetworkState.Inactive)
				{
					if (gameType == NetworkGameType.Singleplayer)
					{
						state = NetworkState.Pregame;
					}
					else
					{
						state = NetworkState.InLobby;
					}
				}

				// Tell all network players that they're in the lobby scene
				for (int i = 0; i < connectedPlayers.Count; ++i)
				{
					NetworkPlayer np = connectedPlayers[i];
					if (np != null)
					{
						np.OnEnterLobbyScene();
					}
				}
			}

			if (sceneChanged != null)
			{
				sceneChanged(false, sceneName);
			}
		}

		/// <summary>
		/// Called on the server when a client adds a new player with ClientScene.AddPlayer.
		///
		/// The default implementation for this function creates a new player object from the playerPrefab.
		/// </summary>
		public void OnServerAddPlayer(ulong clientId)
		{
			// Intentionally not calling base here - we want to control the spawning of prefabs
			Debug.Log("OnServerAddPlayer");

			NetworkPlayer newPlayer = Instantiate<NetworkPlayer>(m_NetworkPlayerPrefab);
			DontDestroyOnLoad(newPlayer);
			newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, destroyWithScene: false);
		}

		public void OnServerRemovePlayer(ulong clientId)
		{
			Debug.Log("OnServerRemovePlayer");

			NetworkPlayer connectedPlayer = GetPlayerForConnection(clientId);
			if (connectedPlayer != null)
			{
				Destroy(connectedPlayer);
				connectedPlayers.Remove(connectedPlayer);
			}
		}

		/// <summary>
		/// Called on the server when a new client connects.
		///
		/// Unity calls this on the Server when a Client connects to the Server. Use an override to tell the NetworkManager what to do when a client connects to the server.
		/// </summary>
		private void OnServerConnect(ulong clientId)
		{
			if (!IsServer)
				return;

			//Debug.LogFormat("OnServerConnect\nID {0}\nAddress {1}\nHostID {2}", conn.connectionId, conn.address, conn.hostId);

			// Reset ready flags for everyone because the game state changed
			if (state == NetworkState.InLobby)
			{
				ClearAllReadyStates();
			}
		}

		private void ConnectionApprovalCheck(byte[] payload, ulong clientid, ConnectionApprovedDelegate approvalAction)
        {
			bool approve = true;
			if (ConnectedClients.Count >= m_MultiplayerMaxPlayers)// || state != NetworkState.InLobby)
            {
				approve = false;
            }

			// Seems like Tanks probably handles spawning differently... don't do it here???
			// If approve is true, the connection gets added. If it's false. The client gets disconnected
			approvalAction(false, null, approve, null, null);
        }

		/// <summary>
		/// Called on the server when a client disconnects.
		///
		/// This is called on the Server when a Client disconnects from the Server. Use an override to decide what should happen when a disconnection is detected.
		/// </summary>
		private void OnServerDisconnect(ulong conn)
		{
			if (!IsServer)
				return;

			Debug.Log("OnServerDisconnect");

			// Reset ready flags for everyone because the game state changed
			if (state == NetworkState.InLobby)
			{
				ClearAllReadyStates();
			}

			if (serverClientDisconnected != null)
			{
				serverClientDisconnected();
			}
		}

		/// <summary>
		/// Server resets networkSceneName
		/// </summary>
		public void OnStartServer()
		{
			// TODO: May cause problems for MLAPI?
			//networkSceneName = string.Empty;
		}

		/// <summary>
		/// Server destroys NetworkPlayer objects
		/// </summary>
		public new void StopServer()
		{
			Debug.Log("StopServer");
			base.StopServer();

			for (int i = 0; i < connectedPlayers.Count; ++i)
			{
				NetworkPlayer player = connectedPlayers[i];
				if (player != null)
				{
					Destroy(player.gameObject);
				}
			}

			connectedPlayers.Clear();

			// Reset this
			// TODO: May cause problems for MLAPI?
			//networkSceneName = string.Empty;

			if (serverStopped != null)
			{
				serverStopped();
			}
		}


		/// <summary>
		/// Clients also destroy their copies of NetworkPlayer
		/// </summary>
		public new void StopClient()
		{
			Debug.Log("StopClient");
			base.StopClient();

			for (int i = 0; i < connectedPlayers.Count; ++i)
			{
				NetworkPlayer player = connectedPlayers[i];
				if (player != null)
				{
					Destroy(player.gameObject);
				}
			}

			connectedPlayers.Clear();

			if (clientStopped != null)
			{
				clientStopped();
			}
		}

		/// <summary>
		/// Fire host started messages
		/// </summary>
		public void OnStartHost()
		{
			Debug.Log("OnStartHost");

			if (m_NextHostStartedCallback != null)
			{
				m_NextHostStartedCallback();
				m_NextHostStartedCallback = null;
			}
			if (hostStarted != null)
			{
				hostStarted();
			}
		}

		/// <summary>
		/// Called on the server when a player is set to ready
		/// </summary>
		public virtual void OnPlayerSetReady(NetworkPlayer player)
		{
			if (AllPlayersReady() && serverPlayersReadied != null)
			{
				serverPlayersReadied();
			}
		}

		#endregion
	}
}