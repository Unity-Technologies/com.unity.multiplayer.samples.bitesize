using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Tanks.UI
{
	/// <summary>
	/// View of all leaderboard elements
	/// </summary>
	public class LeaderboardUI : MonoBehaviour
	{
		[SerializeField]
		protected LeaderboardUIElement m_UiElementPrefab;

		protected List<GameObject> m_UiElementInstances = new List<GameObject>();
		
		//Create the visual list of elements
		public void Setup(List<LeaderboardElement> leaderboardElements)
		{
			//Clear existing elements so that we start with a new list
			ClearUiElements();
			int count = leaderboardElements.Count;
			for (int i = 0; i < count; i++)
			{
				LeaderboardUIElement uiElement = Instantiate<LeaderboardUIElement>(m_UiElementPrefab);
				uiElement.transform.SetParent(transform, false);
				uiElement.Setup(leaderboardElements[i]);
				m_UiElementInstances.Add(uiElement.gameObject);
			}
		}

		//Get rid of UI elements
		protected void ClearUiElements()
		{
			int count = m_UiElementInstances.Count;
			for (int i = count - 1; i >= 0; i--)
			{
				Destroy(m_UiElementInstances[i]);
				m_UiElementInstances.RemoveAt(i);
			}
		}
	}
}