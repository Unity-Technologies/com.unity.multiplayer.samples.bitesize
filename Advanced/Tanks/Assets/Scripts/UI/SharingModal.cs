using UnityEngine;
using Tanks.Data;

namespace Tanks.UI
{
	//Modal for sharing Everyplay recordings
	public class SharingModal : Modal
	{
		protected EveryplayThumbnailPool m_ThumbnailPool;

		//Safe display of recording modal - only displays if the user is recording the session
		public void ShowIfRecording()
		{
			if (m_ThumbnailPool == null)
			{
				// Reacquire persistent component
				m_ThumbnailPool = EveryplayThumbnailPool.instance;
			}

			if (m_ThumbnailPool != null &&
			    m_ThumbnailPool.availableThumbnailCount > 0)
			{
				if (PlayerDataManager.s_InstanceExists &&
				    PlayerDataManager.s_Instance.everyplayEnabled)
				{
					Show();
				}
				else
				{
					m_ThumbnailPool.Clear();
				}
			}
		}

		//Clear the thumbnails for sharing
		public override void CloseModal()
		{
			m_ThumbnailPool.Clear();
			base.CloseModal();
		}

		//Open Everyplay modal if the user clicks share button
		public void OnShareClick()
		{
			ShowEveryplayShare();
		}

		//Wraps the showing of Everyplay modal
		private void ShowEveryplayShare()
		{
			Everyplay.ShowSharingModal();
		}
	}
}