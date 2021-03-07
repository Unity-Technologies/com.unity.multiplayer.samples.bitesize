using UnityEngine;
using System.Collections;

namespace Tanks.UI
{
	/// <summary>
	/// Model an element in the leaderboard
	/// </summary>
	public class LeaderboardElement
	{
		private readonly string m_Description;

		public string description
		{
			get { return m_Description; }
		}

		private readonly Color m_Color;

		public Color color
		{
			get { return m_Color; }
		}

		private readonly int m_Score;

		public int score
		{
			get { return m_Score; }
		}

		public LeaderboardElement(string description, Color color, int score)
		{
			this.m_Description = description;
			this.m_Color = color;
			this.m_Score = score;
		}
	}
}