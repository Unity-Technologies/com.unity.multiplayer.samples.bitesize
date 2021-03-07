using UnityEngine;
using System;

namespace Tanks.Rules
{
	/// <summary>
	/// A serializable object representing a team
	/// </summary>
	[Serializable]
	public class Team
	{
		[SerializeField]
		protected Color m_TeamColor;

		public Color teamColor
		{
			get { return m_TeamColor; }
		}

		[SerializeField]
		protected string m_TeamName;

		public string teamName
		{
			get { return m_TeamName.ToUpperInvariant(); }
		}

		private int m_Score = 0;

		public int score
		{
			get { return m_Score; }
		}

		/// <summary>
		/// Increments the score.
		/// </summary>
		public void IncrementScore()
		{
			m_Score++;
		}
	}
}
