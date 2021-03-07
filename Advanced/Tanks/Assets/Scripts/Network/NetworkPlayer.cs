using System;
using UnityEngine;
using Tanks.Data;
using Tanks.TankControllers;
using Tanks.UI;
using TanksNetworkManager = Tanks.Networking.NetworkManager;
using Tanks.Map;
using MLAPI;
using MLAPI.NetworkVariable;
using MLAPI.Messaging;

namespace Tanks.Networking
{
	public class NetworkPlayer : NetworkBehaviour
	{
		public event Action<NetworkPlayer> syncVarsChanged;
		// Server only event
		public event Action<NetworkPlayer> becameReady;

		public event Action gameDetailsReady;

		[SerializeField]
		protected GameObject m_TankPrefab;
		[SerializeField]
		protected GameObject m_LobbyPrefab;

		// Set by commands
		private NetworkVariableString m_PlayerName = new NetworkVariableString();
		private NetworkVariableColor m_PlayerColor = new NetworkVariableColor(Color.clear);
		private NetworkVariableInt m_PlayerTankType = new NetworkVariableInt(-1);
		private NetworkVariableInt m_PlayerTankDecoration = new NetworkVariableInt(-1);
		private NetworkVariableInt m_PlayerTankDecorationMaterial = new NetworkVariableInt(-1);
		private NetworkVariableBool m_Ready = new NetworkVariableBool(false);

		// Set on the server only
		private bool m_Initialized = false;
		private NetworkVariableInt m_PlayerId = new NetworkVariableInt();

		private IColorProvider m_ColorProvider = null;
		private TanksNetworkManager m_NetManager;
		private GameSettings m_Settings;

		private bool lateSetupOfClientPlayer = false;

        /// <summary>
        /// Gets this player's id
        /// </summary>
        public int playerId
		{
			get { return m_PlayerId.Value; }
		}

		/// <summary>
		/// Gets this player's name
		/// </summary>
		public string playerName
		{
			get { return m_PlayerName.Value; }
		}

		/// <summary>
		/// Gets this player's colour
		/// </summary>
		public Color color
		{
			get { return m_PlayerColor.Value; }
		}

		/// <summary>
		/// Gets this player's tank ID
		/// </summary>
		public int tankType
		{
			get { return m_PlayerTankType.Value; }
		}

		/// <summary>
		/// Gets this player's tank decoration ID
		/// </summary>
		public int tankDecoration
		{
			get { return m_PlayerTankDecoration.Value; }
		}

		/// <summary>
		/// Gets this player's tank material ID
		/// </summary>
		public int tankDecorationMaterial
		{
			get { return m_PlayerTankDecorationMaterial.Value; }
		}

		/// <summary>
		/// Gets whether this player has marked themselves as ready in the lobby
		/// </summary>
		public bool ready
		{
			get { return m_Ready.Value; }
		}

		/// <summary>
		/// Gets the tank manager associated with this player
		/// </summary>
		public TankManager tank
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the lobby object associated with this player
		/// </summary>
		public LobbyPlayer lobbyObject
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the local NetworkPlayer object
		/// </summary>
		public static NetworkPlayer s_LocalPlayer
		{
			get;
			private set;
		}

		/// <summary>
		/// Set initial values
		/// </summary>
		private void OnStartLocalPlayer()
		{
			if (!IsClient)
				throw new Exception("Can only be called by a client!");

			if (m_Settings == null)
			{
				m_Settings = GameSettings.s_Instance;
			}

			Debug.Log("Local Network Player start");
			UpdatePlayerSelections();

			s_LocalPlayer = this;
		}

		/// <summary>
		/// Register us with the NetworkManager
		/// </summary>
		private void OnStartClient()
		{
			if (!IsClient)
				throw new Exception("Can only be called by a client!");

			DontDestroyOnLoad(this);

			if (m_Settings == null)
			{
				m_Settings = GameSettings.s_Instance;
			}
			if (m_NetManager == null)
			{
				m_NetManager = TanksNetworkManager.s_Instance;
			}

			Debug.Log("Client Network Player start");

			m_NetManager.RegisterNetworkPlayer(this);
		}

        public override void NetworkStart()
        {
			if (IsClient)
            {
				OnStartClient();
				if (IsLocalPlayer)
                {
					OnStartLocalPlayer();
                }
            }

            base.NetworkStart();
        }

        public NetworkPlayer()
        {
	        m_PlayerName.OnValueChanged += OnMyName;
	        m_PlayerColor.OnValueChanged += OnMyColor;
	        m_PlayerTankType.OnValueChanged += OnMyTank;
	        m_PlayerTankDecoration.OnValueChanged += OnMyDecoration;
	        m_PlayerTankDecorationMaterial.OnValueChanged += OnMyDecorationMaterial;
	        m_Ready.OnValueChanged += OnReadyChanged;
        }

        /// <summary>
        /// Get network manager
        /// </summary>
        protected virtual void Start()
		{
			if (m_NetManager == null)
			{
				m_NetManager = TanksNetworkManager.s_Instance;
			}
		}

		/// <summary>
		/// Fired when we enter the game scene
		/// </summary>
		public void OnEnterGameScene()
		{
			Debug.Log("OnEnterGameScene");
			if (!IsClient)
				throw new Exception("Can only be called by a client!");

			if (IsOwner)
			{
				CmdClientReadyInGameSceneServerRpc();
			}
		}

		/// <summary>
		/// Fired when we return to the lobby scene, or are first created in the lobby
		/// </summary>
		public void OnEnterLobbyScene()
		{
			if (!IsClient)
				throw new Exception("Can only be called by a client!");

			Debug.Log("OnEnterLobbyScene");
			if (m_Initialized && lobbyObject == null)
			{
				CreateLobbyObject();
			}
		}


		public void ClearReady()
		{
			if (!IsServer)
				throw new Exception("Can only be called by the server!");

			m_Ready.Value = false;
		}


		public void SetPlayerId(int playerId)
		{
			if (!IsServer)
				throw new Exception("Can only be called by the server!");

			this.m_PlayerId.Value = playerId;
		}

		/// <summary>
		/// Deregister us with the manager
		/// </summary>
		protected virtual void OnDestroy()
        {
			if (lobbyObject != null)
			{
				Destroy(lobbyObject.gameObject);
			}

			if (m_NetManager != null)
			{
				m_NetManager.DeregisterNetworkPlayer(this);
			}
        }

		/// <summary>
		/// Create our lobby object
		/// </summary>
		private void CreateLobbyObject()
		{
			lobbyObject = Instantiate(m_LobbyPrefab).GetComponent<LobbyPlayer>();
			lobbyObject.Init(this);
		}


		/// <summary>
		/// Set up our player choices, changing local values too
		/// </summary>
		private void UpdatePlayerSelections()
		{
			if (!IsClient)
				throw new Exception("Can only be called by a client!");


			// TODO: The UNET implementation used to write directly to the syncvars here, which you shouldn't do... UNET apparently "allowed" it, but MLAPI complains.
			// Removed the local writes and things work fine with MLAPI... may have just been a responsiveness "hack" in the UNET implementation to avoid the round-trip delay?
			Debug.Log("UpdatePlayerSelections");
			PlayerDataManager dataManager = PlayerDataManager.s_Instance;
			if (dataManager != null)
			{
				CmdSetInitialValuesServerRpc(dataManager.selectedTank, dataManager.selectedDecoration, dataManager.GetSelectedMaterialForDecoration(dataManager.selectedDecoration), dataManager.playerName);
			}
		}

		private void LazyLoadColorProvider()
		{
			if (!IsServer)
				throw new Exception("Can only be called by the server!");

			if (m_ColorProvider != null)
			{
				return;
			}

			if (m_Settings.mode == null)
			{
				Debug.Log("Missing mode - assigning PlayerColorProvider by default");
				m_ColorProvider = new PlayerColorProvider();
				return;
			}

			m_ColorProvider = m_Settings.mode.rulesProcessor.GetColorProvider();
		}

		private void SelectColour()
		{
			if (!IsServer)
				throw new Exception("Can only be called by the server!");

			LazyLoadColorProvider();

			if (m_ColorProvider == null)
			{
				Debug.LogWarning("Could not find color provider");
				return;
			}

			Color newPlayerColor = m_ColorProvider.ServerGetColor(this);

			m_PlayerColor.Value = newPlayerColor;
		}

		[ClientRpc]
		public void RpcSetGameSettingsClientRpc(int mapIndex, int modeIndex)
		{
			GameSettings settings = GameSettings.s_Instance;
			if (!IsServer)
			{
				settings.SetMapIndex(mapIndex);
				settings.SetModeIndex(modeIndex);
			}
			if (gameDetailsReady != null && IsLocalPlayer)
			{
				gameDetailsReady();
			}
		}

		[ClientRpc]
		public void RpcPrepareForLoadClientRpc()
		{
			if (IsLocalPlayer)
			{
				// Show loading screen
				LoadingModal loading = LoadingModal.s_Instance;

				if (loading != null)
				{
					loading.FadeIn();
				}
			}
		}

		protected void AddClientToServer()
		{
			Debug.Log("CmdClientReadyInScene");
			GameObject tankObject = Instantiate(m_TankPrefab);
			tankObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(OwnerClientId);
			tank = tankObject.GetComponent<TankManager>();
			tank.SetPlayerId(playerId);
			if (lateSetupOfClientPlayer)
			{
				lateSetupOfClientPlayer = false;
				SpawnManager.InstanceSet -= AddClientToServer;
			}
		}

		#region Commands

		/// <summary>
		/// Create our tank
		/// </summary>
		[ServerRpc]
		private void CmdClientReadyInGameSceneServerRpc()
		{
			if (SpawnManager.s_InstanceExists)
			{
				AddClientToServer();
			}
			else
			{
				lateSetupOfClientPlayer = true;
				SpawnManager.InstanceSet += AddClientToServer;
			}
		}

		[ServerRpc]
		private void CmdSetInitialValuesServerRpc(int tankType, int decorationIndex, int decorationMaterial, string newName)
		{
			Debug.Log("CmdChangeTank");
			m_PlayerTankType.Value = tankType;
			m_PlayerTankDecoration.Value = decorationIndex;
			m_PlayerTankDecorationMaterial.Value = decorationMaterial;
			m_PlayerName.Value = newName;
			SelectColour();
			m_Initialized = true;
		}

		[ServerRpc]
		public void CmdChangeTankServerRpc(int tankType)
		{
			Debug.Log("CmdChangeTank");
			m_PlayerTankType.Value = tankType;
		}

		[ServerRpc]
		public void CmdChangeDecorationPropertiesServerRpc(int decorationIndex, int decorationMaterial)
		{
			Debug.Log("CmdChangeDecorationProperties");
			m_PlayerTankDecoration.Value = decorationIndex;
			m_PlayerTankDecorationMaterial.Value = decorationMaterial;
		}

		[ServerRpc]
		public void CmdColorChangeServerRpc()
		{
			Debug.Log("CmdColorChange");
			SelectColour();
		}

		[ServerRpc]
		public void CmdNameChangedServerRpc(string name)
		{
			Debug.Log("CmdNameChanged");
			m_PlayerName.Value = name;
		}

		[ServerRpc]
		public void CmdSetReadyServerRpc(ServerRpcParams rpcParams = default)
		{
			Debug.Log("CmdSetReady - " + rpcParams.Receive.SenderClientId);
			if (m_NetManager.hasSufficientPlayers)
			{
				m_Ready.Value = true;

				if (becameReady != null)
				{
					becameReady(this);
				}
			}
		}

		#endregion


		#region Syncvar callbacks

		private void OnMyName(string _, string newName)
		{
			//m_PlayerName = newName;
			m_SettingsSet |= (0b1 << 3);
			OnHasInitialized();

			if (syncVarsChanged != null)
			{
				syncVarsChanged(this);
			}
		}

		private void OnMyColor(Color _, Color newColor)
		{
			//m_PlayerColor = newColor;
			m_SettingsSet |= (0b1 << 4);
			OnHasInitialized();

			if (syncVarsChanged != null)
			{
				syncVarsChanged(this);
			}
		}

		byte m_SettingsSet = 0;

		private void OnMyTank(int _, int tankIndex)
		{
			if (tankIndex != -1)
			{
				//m_PlayerTankType = tankIndex;
				m_SettingsSet |= 0b1;
				OnHasInitialized();

				if (syncVarsChanged != null)
				{
					syncVarsChanged(this);
				}
			}
		}

		private void OnMyDecoration(int _, int decorationIndex)
		{
			if (decorationIndex != -1)
			{
				//m_PlayerTankDecoration = decorationIndex;
				m_SettingsSet |= (0b1 << 1);
				OnHasInitialized();

				if (syncVarsChanged != null)
				{
					syncVarsChanged(this);
				}
			}
		}

		private void OnMyDecorationMaterial(int _, int decorationMatIndex)
		{
			if (decorationMatIndex != -1)
			{
				//m_PlayerTankDecorationMaterial = decorationMatIndex;
				m_SettingsSet |= (0b1 << 2);
				OnHasInitialized();

				if (syncVarsChanged != null)
				{
					syncVarsChanged(this);
				}
			}
		}

		private void OnReadyChanged(bool _, bool value)
		{
			//m_Ready = value;

			if (syncVarsChanged != null)
			{
				syncVarsChanged(this);
			}
		}

		private void OnHasInitialized()
		{
			if (m_SettingsSet == 0x1f && lobbyObject == null)
			{
				//m_Initialized = value;
				CreateLobbyObject();

				if (IsServer && !m_Settings.isSinglePlayer)
				{
					RpcSetGameSettingsClientRpc(m_Settings.mapIndex, m_Settings.modeIndex);
				}
			}
		}

		#endregion
	}
}