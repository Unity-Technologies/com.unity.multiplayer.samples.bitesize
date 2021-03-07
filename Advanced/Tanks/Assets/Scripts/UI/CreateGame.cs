using System;
using Tanks.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace Tanks.UI
{
	/// <summary>
	/// Governs the Create Game functionality in the main menu.
	/// </summary>
	public class CreateGame : MonoBehaviour
	{
		[SerializeField]
		//Internal reference to the InputField used to enter the server name.
		protected InputField m_MatchNameInput;

		[SerializeField]
		//Internal reference to the MapSelect instance used to flip through multiplayer maps.
		protected MapSelect m_MapSelect;

		[SerializeField]
		//Internal reference to the ModeSelect instance used to cycle multiplayer modes.
		protected ModeSelect m_ModeSelect;

		//Cached references to other UI singletons.
		private MainMenuUI m_MenuUi;
		private NetworkManager m_NetManager;

		protected virtual void Start()
		{
			m_MenuUi = MainMenuUI.s_Instance;
			m_NetManager = NetworkManager.s_Instance;
		}

		/// <summary>
		/// Back button method. Returns to main menu.
		/// </summary>
		public void OnBackClicked()
		{
			m_MenuUi.ShowDefaultPanel();
		}

		/// <summary>
		/// Create button method. Validates entered server name and launches game server.
		/// </summary>
		public void OnCreateClicked()
		{
			if (string.IsNullOrEmpty(m_MatchNameInput.text))
			{
				m_MenuUi.ShowInfoPopup("Server name cannot be empty!", null);
				return;
			}
			
			GameSettings settings = GameSettings.s_Instance;
			settings.SetMapIndex(m_MapSelect.currentIndex);
			settings.SetModeIndex(m_ModeSelect.currentIndex);

			m_NetManager.StartMultiplayerServer((() =>
			{
				m_MenuUi.HideInfoPopup();
				m_MenuUi.ShowLobbyPanel();
			}));
			//StartMatchmakingGame();
		}

		/// <summary>
		/// Populates game settings for broadcast to clients and attempts to start matchmaking server session.
		/// </summary>
		private void StartMatchmakingGame()
		{
			throw new NotImplementedException();
			//GameSettings settings = GameSettings.s_Instance;
			//settings.SetMapIndex(m_MapSelect.currentIndex);
			//settings.SetModeIndex(m_ModeSelect.currentIndex);

			//m_MenuUi.ShowConnectingModal(false);

			//Debug.Log(GetGameName());
			//m_NetManager.StartMatchmakingGame(GetGameName(), (success, matchInfo) =>
			//	{
			//		if (!success)
			//		{
			//			m_MenuUi.ShowInfoPopup("Failed to create game.", null);
			//		}
			//		else
			//		{
			//			m_MenuUi.HideInfoPopup();
			//			m_MenuUi.ShowLobbyPanel();
			//		}
			//	});
		}

		//Returns a formatted string containing server name and game mode information.
		private string GetGameName()
		{
			return string.Format("|{0}| {1}", m_ModeSelect.selectedMode.abbreviation, m_MatchNameInput.text);
		}
	}
}