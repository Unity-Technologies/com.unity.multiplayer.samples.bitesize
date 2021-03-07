using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Tanks.UI
{
	/// <summary>
	/// Leaderboard user interface element - a view
	/// </summary>
	public class LeaderboardUIElement : MonoBehaviour
	{
		[SerializeField]
		protected Text m_Description, m_Score;
		
		[SerializeField]
		protected Image m_Badge;

		//Convert model to view
		public void Setup(LeaderboardElement leaderboardElement)
		{
			if (m_Description != null)
			{
				m_Description.text = leaderboardElement.description;
			}
			else
			{
				Debug.LogWarning("No description text UI");
			}

			if (m_Score != null)
			{
				m_Score.text = leaderboardElement.score.ToString();
			}
			else
			{
				Debug.LogWarning("No score text UI");
			}
	
			if (m_Badge != null)
			{
				m_Badge.color = leaderboardElement.color;
			}
			else
			{
				Debug.LogWarning("No badge image UI");
			}
		}
	}
}