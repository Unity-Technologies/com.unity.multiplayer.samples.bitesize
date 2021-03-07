using System;
using UnityEngine;
using UnityEngine.Events;
using Tanks.Networking;
using TanksNetworkPlayer = Tanks.Networking.NetworkPlayer;
using Tanks.Utilities;

namespace Tanks.UI
{
	//Page in menu to return to
	public enum MenuPage
	{
		Home,
		SinglePlayer,
		Lobby,
		CustomizationPage
	}

	//Class that handles main menu UI and transitions
	public class MainMenuUI : Singleton<MainMenuUI>
	{
		#region Static config

		public static MenuPage s_ReturnPage;

		#endregion

		#region Fields

		[SerializeField]
		protected CanvasGroup m_DefaultPanel;
		[SerializeField]
		protected CanvasGroup m_CreateGamePanel;
		[SerializeField]
		protected CanvasGroup m_LobbyPanel;
		[SerializeField]
		protected CanvasGroup m_SinglePlayerPanel;
		[SerializeField]
		protected CanvasGroup m_ServerListPanel;
		[SerializeField]
		protected CanvasGroup m_CustomizePanel;
		[SerializeField]
		protected LobbyInfoPanel m_InfoPanel;
		[SerializeField]
		protected TankCustomizationModal m_CustomiseModal;
		[SerializeField]
		protected LobbyPlayerList m_PlayerList;
		[SerializeField]
		protected SettingsModal m_SettingsModal;
		[SerializeField]
		protected SharingModal m_SharingModal;
		[SerializeField]
		protected JoinGameDebugModal m_JoinGameDebugModal;

		[SerializeField]
		protected GameObject m_QuitButton;

		private CanvasGroup m_CurrentPanel;

		private Action m_WaitTask;
		private bool m_ReadyToFireTask;

		#endregion

		public LobbyPlayerList playerList
		{
			get
			{
				return m_PlayerList;
			}
		}


		#region Methods

		protected virtual void Update()
		{
			if (m_ReadyToFireTask)
			{
				bool canFire = false;

				LoadingModal modal = LoadingModal.s_Instance;
				if (modal != null && modal.readyToTransition)
				{
					modal.FadeOut();
					canFire = true;
				}
				else if (modal == null)
				{
					canFire = true;
				}

				if (canFire)
				{
					if (m_WaitTask != null)
					{
						m_WaitTask();
						m_WaitTask = null;
					}

					m_ReadyToFireTask = false;
				}
			}
		}

		protected virtual void Start()
		{
			LoadingModal modal = LoadingModal.s_Instance;
			if (modal != null)
			{
				modal.FadeOut();
			}

			if (m_QuitButton != null)
			{
				m_QuitButton.SetActive(!MobileUtilities.IsOnMobile());
			}
			else
			{
				Debug.LogError("Missing quitButton from MainMenuUI");
			}

			if (m_SharingModal != null)
			{
				m_SharingModal.ShowIfRecording();
			}
			
			//Used to return to correct page on return to menu
			switch (s_ReturnPage)
			{
				case MenuPage.Home:
				default:
					ShowDefaultPanel();
					break;
				case MenuPage.Lobby:
					ShowLobbyPanel();
					break;
				case MenuPage.CustomizationPage:
					ShowCustomizePanel();
					break;
				case MenuPage.SinglePlayer:
					ShowSingleplayerPanel();
					break;
			}
		}
		
		//Convenience function for showing panels
		public void ShowPanel(CanvasGroup newPanel)
		{
			if (m_CurrentPanel != null)
			{
				m_CurrentPanel.gameObject.SetActive(false);
			}

			m_CurrentPanel = newPanel;
			if (m_CurrentPanel != null)
			{
				m_CurrentPanel.gameObject.SetActive(true);
			}
		}

		public void ShowDefaultPanel()
		{
			ShowPanel(m_DefaultPanel);
		}

		public void ShowLobbyPanel()
		{
			ShowPanel(m_LobbyPanel);
		}

		public void ShowLobbyPanelForConnection()
		{
			ShowPanel(m_LobbyPanel);
			NetworkManager.s_Instance.gameModeUpdated -= ShowLobbyPanelForConnection;
			HideInfoPopup();
		}

		public void ShowServerListPanel()
		{
			ShowPanel(m_ServerListPanel);
		}

		public void ShowSettingsModal()
		{
			m_SettingsModal.Show();
		}
		
		public void ShowJoinGameDebugModal()
		{
			m_JoinGameDebugModal.Show();
		}

		public void ShowCustomizePanel()
		{
			ShowPanel(m_CustomizePanel);
		}

		public void ShowTankSelectModal(TanksNetworkPlayer player)
		{
			if (m_CustomiseModal != null)
			{
				m_CustomiseModal.Open(player);
			}
		}

		/// <summary>
		/// Shows the info popup with a callback
		/// </summary>
		/// <param name="label">Label.</param>
		/// <param name="callback">Callback.</param>
		public void ShowInfoPopup(string label, UnityAction callback)
		{
			if (m_InfoPanel != null)
			{
				m_InfoPanel.Display(label, callback, true);
			}
		}

		public void ShowInfoPopup(string label)
		{
			if (m_InfoPanel != null)
			{
				m_InfoPanel.Display(label, null, false);
			}
		}

		public void ShowConnectingModal(bool reconnectMatchmakingClient)
		{
			ShowInfoPopup("Connecting...", () =>
				{
					if (NetworkManager.s_InstanceExists)
					{
						if (reconnectMatchmakingClient)
						{
							NetworkManager.s_Instance.Disconnect();
							//NetworkManager.s_Instance.StartMatchingmakingClient();
						}
						else
						{
							NetworkManager.s_Instance.Disconnect();
						}
					}
				});
		}

		public void HideInfoPopup()
		{
			if (m_InfoPanel != null)
			{
				m_InfoPanel.gameObject.SetActive(false);
			}
		}

		/// <summary>
		/// Wait for network to disconnect before performing an action
		/// </summary>
		public void DoIfNetworkReady(Action task)
		{
			if (task == null)
			{
				throw new ArgumentNullException("task");
			}

			NetworkManager netManager = NetworkManager.s_Instance;

			// TODO: Not as simple with MLAPI to detect an active connection? Maybe grab the transport instead?
			if (netManager.IsServer || netManager.IsClient)
			{
				m_WaitTask = task;

				LoadingModal modal = LoadingModal.s_Instance;
				if (modal != null)
				{
					modal.FadeIn();
				}

				m_ReadyToFireTask = false;
				netManager.clientStopped += OnClientStopped;
			}
			else
			{
				task();
			}
		}
		
		//Event listener
		private void OnClientStopped()
		{
			NetworkManager netManager = NetworkManager.s_Instance;
			netManager.clientStopped -= OnClientStopped;
			m_ReadyToFireTask = true;
		}

		private void ShowSingleplayerPanel()
		{
			ShowPanel(m_SinglePlayerPanel);
		}

		private void GoToSingleplayerPanel()
		{
			ShowSingleplayerPanel();
			NetworkManager.s_Instance.StartSinglePlayerMode(null);
		}

		private void GoToFindGamePanel()
		{
			ShowJoinGameDebugModal();
			//ShowServerListPanel();
			//NetworkManager.s_Instance.StartMatchingmakingClient();
		}

		private void GoToCreateGamePanel()
		{
			ShowPanel(m_CreateGamePanel);
		}

		#endregion


		#region Button events

		public void OnCustomiseClicked()
		{
			DoIfNetworkReady(ShowCustomizePanel);
		}

		public void OnCreateGameClicked()
		{
			DoIfNetworkReady(GoToCreateGamePanel);
		}

		public void OnSinglePlayerClicked()
		{
			// Set network into SP mode
			DoIfNetworkReady(GoToSingleplayerPanel);
		}

		public void OnFindGameClicked()
		{
			// Set network into matchmaking search mode
			DoIfNetworkReady(GoToFindGamePanel);
		}

		public void OnQuitGameClicked()
		{
			Application.Quit();
		}

		#endregion
	}
}