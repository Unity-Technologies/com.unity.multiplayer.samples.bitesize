using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking.Match;
using Tanks.Networking;
using UnityEngine.UI;

namespace Tanks.UI
{
	//UI view list of available games
	public class LobbyServerList : MonoBehaviour
	{
		//Number of games per page
		[SerializeField]
		private int m_PageSize = 6;

		//Editor configurable time
		[SerializeField]
		private float m_ListAutoRefreshTime = 60f;
		private float m_NextRefreshTime;
		
		//Reference to paging buttons
		[SerializeField]
		protected Button m_NextButton, m_PreviousButton;
		
		[SerializeField]
		protected Text m_PageNumber;

		[SerializeField]
		protected RectTransform m_ServerListRect;
		[SerializeField]
		protected GameObject m_ServerEntryPrefab;
		[SerializeField]
		protected GameObject m_NoServerFound;

		//Page tracking
		protected int m_CurrentPage = 0;
		protected int m_PreviousPage = 0;
		protected int m_NewPage = 0;

		static Color s_OddServerColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
		static Color s_EvenServerColor = new Color(.94f, .94f, .94f, 1.0f);

		//Cached singletons
		private NetworkManager m_NetManager;
		private MainMenuUI m_MenuUi;


		protected virtual void OnEnable()
		{
			//Cache singletons
			if (m_NetManager == null)
			{
				m_NetManager = NetworkManager.s_Instance;
			}
			if (m_MenuUi == null)
			{
				m_MenuUi = MainMenuUI.s_Instance;
			}
			
			//Reset pages
			m_CurrentPage = 0;
			m_PreviousPage = 0;

			ClearUi();

			//Disable NO SERVER FOUND error message
			m_NoServerFound.SetActive(false);

			m_NextRefreshTime = Time.time;

			//Subscribe to network events
			if (m_NetManager != null)
			{
				m_NetManager.clientDisconnected += OnDisconnect;
				m_NetManager.clientError += OnError;
				m_NetManager.serverError += OnError;
			}
		}

		protected void ClearUi()
		{
			foreach (Transform t in m_ServerListRect)
			{
				Destroy(t.gameObject);
			}
		}

		protected virtual void OnDisable()
		{
			//Unsubscribe from network events
			if (m_NetManager != null)
			{
				m_NetManager.clientDisconnected -= OnDisconnect;
				m_NetManager.clientError -= OnError;
				m_NetManager.serverError -= OnError;
			}
		}
		
		//Network event
		protected virtual void OnError(ulong conn, int errorCode)
		{
			if (m_MenuUi != null)
			{
				m_MenuUi.ShowDefaultPanel();
				m_MenuUi.ShowInfoPopup("A connection error occurred", null);
			}

			if (m_NetManager != null)
			{
				m_NetManager.Disconnect();
			}
		}
		
		//Network event
		protected virtual void OnDisconnect(ulong conn)
		{
			if (m_MenuUi != null)
			{
				m_MenuUi.ShowDefaultPanel();
				m_MenuUi.ShowInfoPopup("Disconnected from server", null);
			}

			if (m_NetManager != null)
			{
				m_NetManager.Disconnect();
			}
		}
		
		//Network event
		//protected virtual void OnDrop()
		//{
		//	if (m_MenuUi != null)
		//	{
		//		m_MenuUi.ShowDefaultPanel();
		//		m_MenuUi.ShowInfoPopup("Disconnected from server", null);
		//	}

		//	if (m_NetManager != null)
		//	{
		//		m_NetManager.Disconnect();
		//	}
		//}

		//Check for refresh
		protected virtual void Update()
		{
			if (m_NextRefreshTime <= Time.time)
			{
				RequestPage(m_CurrentPage);

				m_NextRefreshTime = Time.time + m_ListAutoRefreshTime;
			}
		}
		
		//On click of back button
		public void OnBackClick()
		{
			m_NetManager.Disconnect();
			m_MenuUi.ShowDefaultPanel();
		}
		
		//Callback for request
		public void OnGuiMatchList(bool flag, string extraInfo, List<MatchInfoSnapshot> response)
		{
			//If no response do nothing
			if (response == null)
			{
				return;
			}
			
			m_NextButton.interactable = true;
			m_PreviousButton.interactable = true;

			m_PreviousPage = m_CurrentPage;
			m_CurrentPage = m_NewPage;
	
			//if nothing is returned
			if (response.Count == 0)
			{
				//current page is 0 then set enable NO SERVER FOUND message
				if (m_CurrentPage == 0)
				{
					m_NoServerFound.SetActive(true);
					ClearUi();
					m_PreviousButton.interactable = false;
					m_NextButton.interactable = false;
					m_PageNumber.enabled = false;
				}

				m_CurrentPage = m_PreviousPage;

				return;
			}
			
			//Prev button should not be interactable for first (zeroth) page
			m_PreviousButton.interactable = m_CurrentPage > 0;
			//Next button should not be interactable if the current page is not full
			m_NextButton.interactable = response.Count == m_PageSize;
	
			m_NoServerFound.SetActive(false);
			
			//Handle page number
			m_PageNumber.enabled = true;
			m_PageNumber.text = (m_CurrentPage + 1).ToString();

			//Clear all transforms
			foreach (Transform t in m_ServerListRect)
				Destroy(t.gameObject);
			
			//Instantiate UI gameObjects
			for (int i = 0; i < response.Count; ++i)
			{
				GameObject o = Instantiate(m_ServerEntryPrefab);

				o.GetComponent<LobbyServerEntry>().Populate(response[i], (i % 2 == 0) ? s_OddServerColor : s_EvenServerColor);

				o.transform.SetParent(m_ServerListRect, false);
			}
		}
		
		//Called by button clicks
		public void ChangePage(int dir)
		{
			int newPage = Mathf.Max(0, m_CurrentPage + dir);
			this.m_NewPage = newPage;

			//if we have no server currently displayed, need we need to refresh page0 first instead of trying to fetch any other page
			if (m_NoServerFound.activeSelf)
				newPage = 0;

			RequestPage(newPage);
		}

		//Handle requests
		public void RequestPage(int page)
		{
			return;

			//if (m_NetManager != null && m_NetManager.matchMaker != null)
			//{
			//	m_NextButton.interactable = false;
			//	m_PreviousButton.interactable = false;

			//	Debug.Log("Requesting match list");
			//	m_NetManager.matchMaker.ListMatches(page, m_PageSize, string.Empty, false, 0, 0, OnGuiMatchList);
			//}
		}

		//We just set the autorefresh time to RIGHT NOW when this button is pushed, triggering all the refresh logic in the next Update tick.
		public void RefreshList()
		{
			m_NextRefreshTime = Time.time;
		}
	}
}