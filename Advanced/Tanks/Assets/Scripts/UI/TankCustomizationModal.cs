using Tanks.Networking;
using Tanks.Data;

namespace Tanks.UI
{
	/// <summary>
	/// Tank selector modal shown in the lobby
	/// </summary>
	public class TankCustomizationModal : TankSelector
	{
		private NetworkPlayer m_CallingPlayer;

		/// <summary>
		/// When OK is press select this tank
		/// </summary>
		public void OKButton()
		{
			m_CallingPlayer.CmdChangeTankServerRpc(m_CurrentIndex);
			if (PlayerDataManager.s_InstanceExists)
			{
				PlayerDataManager.s_Instance.selectedTank = m_CurrentIndex;
			}
			Close();
		}

		/// <summary>
		/// Close the modal
		/// </summary>
		public void Close()
		{
			gameObject.SetActive(false);
		}

		/// <summary>
		/// Open the modal and references the calling player
		/// </summary>
		/// <param name="calledBy">Called by.</param>
		public void Open(NetworkPlayer calledBy)
		{
			ResetSelections();
			m_CallingPlayer = calledBy;

			gameObject.SetActive(true);
		}
	}
}
