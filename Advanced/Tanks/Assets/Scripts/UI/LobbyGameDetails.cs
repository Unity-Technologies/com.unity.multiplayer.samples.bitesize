using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Tanks.Map;
using Tanks.Rules;
using Tanks.Networking;
using TanksNetworkManager = Tanks.Networking.NetworkManager;
using MLAPI.Connection;

namespace Tanks.UI
{
	/// <summary>
	/// This class handles the display of all game details (game mode, map) in the game lobby and allows the host to change the active map.
	/// </summary>
	public class LobbyGameDetails : Select
	{
		//Reference to ScriptableObject containing multiplayer map data.
		[SerializeField]
		protected MapList m_MapList;
        
		//Reference to ScriptableObject containing multiplayer game mode data.
		[SerializeField]
		protected ModeList m_ModeList;
        
		//References to name and description text fields.
		[SerializeField]
		protected Text m_MapName, m_MapDescription, m_ModeName, m_ModeDescription;
        
		//Map preview image.
		[SerializeField]
		protected Image m_Preview;

		//Array of mode and map specific objects, to be enabled dynamically.
		[SerializeField]
		protected GameObject[] m_ModeObjects, m_MapObjects;

		//Array of map change buttons, to be enabled/disabled dynamically.
		[SerializeField]
		protected GameObject[] m_MapButtons;

		//Internal flags determining whether info has been updated.
		protected bool m_HasUpdateMode = false, m_HasUpdateMap = false;

		//Internal references to game settings and network manager.
		protected GameSettings m_Settings;
		protected TanksNetworkManager m_NetManager;

		/// <summary>
		/// Uses provided map data to populate backgrounds, descriptions, etc. on this screen.
		/// </summary>
		/// <param name="map">MapDetails from which to draw data</param>
		public void UpdateMapDetails(MapDetails map)
		{
			if (m_MapName != null)
			{
				m_MapName.text = map.name.ToUpperInvariant();
			}
			if (m_MapDescription != null)
			{
				m_MapDescription.text = map.description;
			}
			if (m_Preview != null)
			{
				m_Preview.enabled = true;
				m_Preview.sprite = map.image;
			}
			EnableArrayOfGameObjects(m_MapObjects, true);
			m_HasUpdateMap = true;
		}

		/// <summary>
		/// Uses provided mode data to populate game mode descriptions, etc. on this screen.
		/// </summary>
		/// <param name="mode">ModeDetails from which to draw data</param>
		public void UpdateModeDetails(ModeDetails mode)
		{
			if (m_ModeName != null)
			{
				m_ModeName.text = mode.modeName;
			}
			if (m_ModeDescription != null)
			{
				m_ModeDescription.text = mode.description;
			}
			EnableArrayOfGameObjects(m_ModeObjects, true);
			m_HasUpdateMode = true;
		}


		private void EnableArrayOfGameObjects(GameObject[] gameObjectArray, bool isEnabled)
		{
			int length = gameObjectArray.Length;

			for (int i = 0; i < length; i++)
			{
				gameObjectArray[i].SetActive(isEnabled);
			}
		}

		//On selection changed, select a new map and ripple the selection data out to all members of the lobby.
		protected override void AssignByIndex()
		{
			GameSettings.s_Instance.SetMapIndex(m_CurrentIndex);

			for (int i = 0; i < TanksNetworkManager.s_Instance.connectedPlayers.Count; i++)
			{
				TanksNetworkManager.s_Instance.connectedPlayers[i].RpcSetGameSettingsClientRpc(m_CurrentIndex, m_Settings.modeIndex);
			}
		}

		protected virtual void OnEnable()
		{
			//Enable or disable map selection buttons based on whether this is the host or not.
			for (int i = 0; i < m_MapButtons.Length; i++)
			{
				m_MapButtons[i].SetActive(TanksNetworkManager.s_IsServer);
			}

			if (m_NetManager == null)
			{
				m_NetManager = TanksNetworkManager.s_Instance;
			}

			if (m_NetManager != null)
			{
				m_NetManager.clientDisconnected += OnDisconnect;
				m_NetManager.clientError += OnError;
				m_NetManager.serverError += OnError;
			}
		}

		protected virtual void OnDisable()
		{
			if (m_NetManager != null)
			{
				m_NetManager.clientDisconnected -= OnDisconnect;
				m_NetManager.clientError -= OnError;
				m_NetManager.serverError -= OnError;
			}
		}

		protected virtual void OnError(ulong conn, int errorCode)
		{
			MainMenuUI menuUi = MainMenuUI.s_Instance;

			if (menuUi != null)
			{
				menuUi.ShowDefaultPanel();
				menuUi.ShowInfoPopup("A connection error occurred", null);
			}

			if (m_NetManager != null)
			{
				m_NetManager.Disconnect();
			}
		}

		protected virtual void OnDisconnect(ulong conn)
		{
			MainMenuUI menuUi = MainMenuUI.s_Instance;

			if (menuUi != null)
			{
				menuUi.ShowDefaultPanel();
				menuUi.ShowInfoPopup("Disconnected from server", null);
			}

			if (m_NetManager != null)
			{
				m_NetManager.Disconnect();
			}
		}

		protected virtual void Start()
		{
			m_Settings = GameSettings.s_Instance;
			m_Settings.mapChanged += UpdateMapDetails;
			m_Settings.modeChanged += UpdateModeDetails;

			m_CurrentIndex = GameSettings.s_Instance.mapIndex;
			m_ListLength = m_MapList.Count;

			if (m_Settings.map != null)
			{
				UpdateMapDetails(m_Settings.map);
			}
			if (m_Settings.mode != null)
			{
				UpdateModeDetails(m_Settings.mode);
			}
		}

		protected virtual void OnDestroy()
		{
			if (m_Settings != null)
			{
				m_Settings.mapChanged -= UpdateMapDetails;
				m_Settings.modeChanged -= UpdateModeDetails;
			}
		}
	}
}