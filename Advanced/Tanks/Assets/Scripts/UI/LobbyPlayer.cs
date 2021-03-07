using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TanksNetworkManager = Tanks.Networking.NetworkManager;
using TanksNetworkPlayer = Tanks.Networking.NetworkPlayer;
using Tanks.Data;
using Tanks.Networking;

namespace Tanks.UI
{
	//Player entry in the lobby. Handle selecting color/setting name & getting ready for the game
	//Any LobbyHook can then grab it and pass those value to the game player prefab (see the Pong Example in the Samples Scenes)
	public class LobbyPlayer : MonoBehaviour
	{
		[SerializeField]
		protected Button m_ColorButton;
		[SerializeField]
		protected Image m_ColorTag;
		[SerializeField]
		protected InputField m_NameInput;
		[SerializeField]
		protected Button m_ReadyButton;
		[SerializeField]
		protected Transform m_WaitingLabel;
		[SerializeField]
		protected Transform m_ReadyLabel;
		[SerializeField]
		protected Button m_TankSelectButton;
		[SerializeField]
		protected Text m_TankIndexText;
		[SerializeField]
		protected Image m_ColorButtonImage;

		/// <summary>
		/// Associated NetworkPlayer object
		/// </summary>
		private TanksNetworkPlayer m_NetPlayer;

		private TanksNetworkManager m_NetManager;

		public void Init(TanksNetworkPlayer netPlayer)
		{
			Debug.LogFormat("Initializing lobby player - Ready {0}", netPlayer.ready);
			this.m_NetPlayer = netPlayer;
			if (netPlayer != null)
			{
				netPlayer.syncVarsChanged += OnNetworkPlayerSyncvarChanged;
			}

			m_NetManager = TanksNetworkManager.s_Instance;
			if (m_NetManager != null)
			{
				m_NetManager.playerJoined += PlayerJoined;
				m_NetManager.playerLeft += PlayerLeft;
			}

			m_ReadyLabel.gameObject.SetActive(false);
			m_WaitingLabel.gameObject.SetActive(false);
			m_ReadyButton.gameObject.SetActive(true);
			m_ReadyButton.interactable = m_NetManager.hasSufficientPlayers;

			if (m_NetManager.gameType == NetworkGameType.Singleplayer)
			{
				return;
			}
			
			MainMenuUI mainMenu = MainMenuUI.s_Instance;
			
			mainMenu.playerList.AddPlayer(this);
			mainMenu.playerList.DisplayDirectServerWarning(NetworkManager.s_IsServer);

			if (netPlayer.IsOwner)
			{
				SetupLocalPlayer();
			}
			else
			{
				SetupRemotePlayer();
			}

			UpdateValues();
		}

		public void RefreshJoinButton()
		{
			if (m_NetPlayer.ready)
			{
				// Toggle ready label
				m_WaitingLabel.gameObject.SetActive(false);
				m_ReadyButton.gameObject.SetActive(false);
				m_ReadyLabel.gameObject.SetActive(true);

				// Make everything else non-interactive
				m_ColorButton.interactable = false;
				m_ColorButtonImage.enabled = false;
				m_NameInput.interactable = false;
				m_NameInput.image.enabled = false;
			}
			else
			{
				// Toggle ready button
				if (m_NetPlayer.IsOwner)
				{
					m_ReadyButton.gameObject.SetActive(true);
					m_ReadyButton.interactable = m_NetManager.hasSufficientPlayers;
				}
				else
				{
					m_WaitingLabel.gameObject.SetActive(true);
				}
				m_ReadyLabel.gameObject.SetActive(false);

				m_ColorButton.interactable = m_NetPlayer.IsOwner;
				m_ColorButtonImage.enabled = m_NetPlayer.IsOwner;
				m_NameInput.interactable = m_NetPlayer.IsOwner;
				m_NameInput.image.enabled = m_NetPlayer.IsOwner;
			}
		}

		protected virtual void PlayerJoined(TanksNetworkPlayer player)
		{
			RefreshJoinButton();
		}

		protected virtual void PlayerLeft(TanksNetworkPlayer player)
		{
			RefreshJoinButton();
		}

		protected virtual void OnDestroy()
		{
			if (m_NetPlayer != null)
			{
				m_NetPlayer.syncVarsChanged -= OnNetworkPlayerSyncvarChanged;
			}

			if (m_NetManager != null)
			{
				m_NetManager.playerJoined -= PlayerJoined;
				m_NetManager.playerLeft -= PlayerLeft;
			}
		}

		private void ChangeReadyButtonColor(Color c)
		{
			m_ReadyButton.image.color = c;
		}

		private void UpdateValues()
		{
			m_NameInput.text = m_NetPlayer.playerName;
			m_ColorTag.color = m_NetPlayer.color;
			m_ColorButton.GetComponent<Image>().color = m_NetPlayer.color;
			m_TankIndexText.text = TankLibrary.s_Instance.GetTankDataForIndex(m_NetPlayer.tankType).name.ToUpperInvariant();

			RefreshJoinButton();
		}

		private void SetupRemotePlayer()
		{
			DeactivateInteractables();

			m_ReadyButton.gameObject.SetActive(false);
			m_WaitingLabel.gameObject.SetActive(true);
		}

		private void SetupLocalPlayer()
		{
			m_NameInput.interactable = true;
			m_NameInput.image.enabled = true;

			//We only want in-lobby tank selection button to be clickable if the local player has more than one tank unlocked.
			bool multipleTanksUnlocked = (TankLibrary.s_Instance.GetNumberOfUnlockedTanks() > 1);
			m_TankSelectButton.interactable = multipleTanksUnlocked;

			m_TankSelectButton.image.enabled = true;
			m_ColorButton.interactable = true;

			m_ReadyButton.gameObject.SetActive(true);

			//we switch from simple name display to name input
			m_NameInput.onEndEdit.RemoveAllListeners();
			m_NameInput.onEndEdit.AddListener(OnNameChanged);

			m_ColorButton.onClick.RemoveAllListeners();
			m_ColorButton.onClick.AddListener(OnColorClicked);

			m_ReadyButton.onClick.RemoveAllListeners();
			m_ReadyButton.onClick.AddListener(OnReadyClicked);
		}

		private void OnNetworkPlayerSyncvarChanged(TanksNetworkPlayer player)
		{
			// Update everything
			UpdateValues();
		}

		//===== UI Handler

		//Note that those handler use Command function, as we need to change the value on the server not locally
		//so that all client get the new value throught syncvar
		public void OnColorClicked()
		{
			m_NetPlayer.CmdColorChangeServerRpc();
		}

		public void OnReadyClicked()
		{
			m_NetPlayer.CmdSetReadyServerRpc();
			DeactivateInteractables();
		}

		public void OnNameChanged(string str)
		{
			m_NetPlayer.CmdNameChangedServerRpc(str);
		}

		public void OnTankClicked()
		{
			if (m_NetPlayer.IsOwner && MainMenuUI.s_InstanceExists)
			{
				MainMenuUI.s_Instance.ShowTankSelectModal(m_NetPlayer);
			}
		}

		private void DeactivateInteractables()
		{
			m_NameInput.interactable = false;
			m_NameInput.image.enabled = false;
			m_TankSelectButton.image.enabled = false;
			m_TankSelectButton.interactable = false;
			m_ColorButtonImage.enabled = false;
			m_TankIndexText.color = Color.white;
		}
	}
}
