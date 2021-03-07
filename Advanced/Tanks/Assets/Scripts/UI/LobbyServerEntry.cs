using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.Types;
using Tanks.Networking;
using TanksNetworkManager = Tanks.Networking.NetworkManager;
using System;

namespace Tanks.UI
{
	/// <summary>
	/// Represents a server in the server list
	/// </summary>
	public class LobbyServerEntry : MonoBehaviour
	{
		//UI elements
		[SerializeField]
		protected Text m_ServerInfoText;
		[SerializeField]
		protected Text m_ModeText;
		[SerializeField]
		protected Text m_SlotInfo;
		[SerializeField]
		protected Button m_JoinButton;

		//The network manager
		protected TanksNetworkManager m_NetManager;

		//Sets up the UI
		public void Populate(MatchInfoSnapshot match, Color c)
		{
			string name = match.name;

			string[] split = name.Split(new char [1]{ '|' }, StringSplitOptions.RemoveEmptyEntries);

			m_ServerInfoText.text = split[1].Replace(" ", string.Empty);
			m_ModeText.text = split[0];

			m_SlotInfo.text = string.Format("{0}/{1}", match.currentSize, match.maxSize);

			NetworkID networkId = match.networkId;

			m_JoinButton.onClick.RemoveAllListeners();
			m_JoinButton.onClick.AddListener(() => JoinMatch(networkId));

			m_JoinButton.interactable = match.currentSize < match.maxSize;

			GetComponent<Image>().color = c;
		}

		//Load the network manager on enable
		protected virtual void OnEnable()
		{
			if (m_NetManager == null)
			{
				m_NetManager = TanksNetworkManager.s_Instance;
			}
		}

		//Fired when player clicks join
		private void JoinMatch(NetworkID networkId)
		{
			throw new NotImplementedException();

			//MainMenuUI menuUi = MainMenuUI.s_Instance;

			//menuUi.ShowConnectingModal(true);

			//m_NetManager.JoinMatchmakingGame(networkId, (success, matchInfo) =>
			//	{
			//		//Failure flow
			//		if (!success)
			//		{
			//			menuUi.ShowInfoPopup("Failed to join game.", null);
			//		}
			//		//Success flow
			//		else
			//		{
			//			menuUi.HideInfoPopup();
			//			menuUi.ShowInfoPopup("Entering lobby...");
			//			m_NetManager.gameModeUpdated += menuUi.ShowLobbyPanelForConnection;
			//		}
			//	});
		}
	}
}
