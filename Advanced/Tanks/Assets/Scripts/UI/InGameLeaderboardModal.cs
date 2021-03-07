using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Tanks.Rules;
using Tanks.Utilities;

namespace Tanks.UI
{
	/// <summary>
	/// Modal to display leaderboard reflecting current in-game scores.
	/// </summary>
	public class InGameLeaderboardModal : Singleton<InGameLeaderboardModal>
	{
		[SerializeField]
		protected LeaderboardUI m_Leaderboard;

		[SerializeField]
		protected Text m_Heading;

		protected RulesProcessor m_Rules;

		protected override void Awake()
		{
			base.Awake();
			Hide();
		}

		/// <summary>
		/// Displays the modal.
		/// </summary>
		/// <param name="text">Text to display as modal header.</param>
		public void Show(string text)
		{
			gameObject.SetActive(true);
			LazyLoad();
			m_Leaderboard.Setup(m_Rules.GetLeaderboardElements());
			m_Heading.text = text;
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}

		protected void LazyLoad()
		{
			if (m_Rules != null)
			{
				return;
			}

			m_Rules = GameSettings.s_Instance.mode.rulesProcessor;
			m_Rules.SetGameManager(GameManager.s_Instance);
		}

	}
}