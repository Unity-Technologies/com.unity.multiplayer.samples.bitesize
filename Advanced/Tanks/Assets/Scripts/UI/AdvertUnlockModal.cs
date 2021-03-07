using UnityEngine;
using System;
using System.Collections;
using Tanks.Advertising;

namespace Tanks.UI
{
	//This deprecated class was used to extend Modal for ad unlock functionality
	public class AdvertUnlockModal : Modal 
	{
		private Action m_AdvertSuccess;
		private Action m_AdvertFailed;

		//Show the modal, and cache the success and failure references passed by the caller.
		public void ShowAdModal(Action onSuccess, Action onFailure)
		{
			m_AdvertSuccess = onSuccess;
			m_AdvertFailed = onFailure;

			base.Show();
		}

		//Linked to a "Show Advert" button. Fired off the ad via the Ad Manager, passing it the cached delegates, and killed this modal.
		public void OnAdButtonClicked()
		{
			TanksAdvertController.s_Instance.StartAdvert(m_AdvertSuccess,m_AdvertFailed);
			base.CloseModal();
		}
	}
}
