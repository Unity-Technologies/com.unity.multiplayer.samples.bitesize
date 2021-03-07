using UnityEngine;
using Tanks.Networking;

namespace Tanks.UI
{
	/// <summary>
	/// Lobby panel
	/// </summary>
	public class LobbyPanel : MonoBehaviour
	{
		private MainMenuUI m_MenuUi;
		private NetworkManager m_NetManager;

		protected virtual void Start()
		{
			m_MenuUi = MainMenuUI.s_Instance;
			m_NetManager = NetworkManager.s_Instance;
		}

		public void OnBackClick()
		{
			Back();
		}

		private void Back()
		{
			m_NetManager.Disconnect();
			m_MenuUi.ShowDefaultPanel();
		}
	}
}