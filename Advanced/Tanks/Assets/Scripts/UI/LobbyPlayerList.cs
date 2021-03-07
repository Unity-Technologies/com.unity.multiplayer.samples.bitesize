using UnityEngine;
using UnityEngine.UI;
using Tanks.UI;
using Tanks.Networking;
using TanksNetworkPlayer = Tanks.Networking.NetworkPlayer;

namespace Tanks.UI
{
	/// <summary>
	/// Lobby player list.
	/// </summary>
	public class LobbyPlayerList : MonoBehaviour
	{
		public static LobbyPlayerList s_Instance = null;

		[SerializeField]
		protected RectTransform m_PlayerListContentTransform;
		[SerializeField]
		protected GameObject m_WarningDirectPlayServer;

		private NetworkManager m_NetManager;

		protected virtual void Awake()
		{
			s_Instance = this;
		}

		//Subscribe to events on start
		protected virtual void Start()
		{
			m_NetManager = NetworkManager.s_Instance;
			if (m_NetManager != null)
			{
				m_NetManager.playerJoined += PlayerJoined;
				m_NetManager.playerLeft += PlayerLeft;
				m_NetManager.serverPlayersReadied += PlayersReadied;
			}
		}

		//Unsubscribe to events on destroy
		protected virtual void OnDestroy()
		{
			if (m_NetManager != null)
			{
				m_NetManager.playerJoined -= PlayerJoined;
				m_NetManager.playerLeft -= PlayerLeft;
				m_NetManager.serverPlayersReadied -= PlayersReadied;
			}
		}
		
		//Used in direct play - display warning
		public void DisplayDirectServerWarning(bool enabled)
		{
			if (m_WarningDirectPlayServer != null)
				m_WarningDirectPlayServer.SetActive(enabled);
		}

		//Add lobby player to UI
		public void AddPlayer(LobbyPlayer player)
		{
			Debug.Log("Add player to list");
			player.transform.SetParent(m_PlayerListContentTransform, false);
		}
		
		//Log player joining for tracing
		protected virtual void PlayerJoined(TanksNetworkPlayer player)
		{
			Debug.LogFormat("Player joined {0}", player.name);
		}

		//Log player leaving for tracing
		protected virtual void PlayerLeft(TanksNetworkPlayer player)
		{
			Debug.LogFormat("Player left {0}", player.name);
		}

		//When players are all ready progress
		protected virtual void PlayersReadied()
		{
			m_NetManager.ProgressToGameScene();
		}
	}
}
