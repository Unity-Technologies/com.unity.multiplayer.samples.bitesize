using UnityEngine;
using System.Collections.Generic;
using TanksNetworkPlayer = Tanks.Networking.NetworkPlayer;

namespace Tanks.UI
{
	/// <summary>
	/// Color provider for team based games
	/// </summary>
	public class TeamColorProvider : IColorProvider
	{
		//Available colors
		private List<Color> m_Colors = new List<Color>() { Color.red, Color.blue };

		private List<TanksNetworkPlayer> m_Players = new List<TanksNetworkPlayer>();

		private int m_LastUsedColorIndex = -1;

		//Get the available color
		public Color ServerGetColor(TanksNetworkPlayer player)
		{
			int playerIndex = m_Players.IndexOf(player);
			if (playerIndex == -1)
			{
				m_Players.Add(player);
				playerIndex = m_Players.Count - 1;
			}

			Color playerColor = player.color;
			int index = m_Colors.IndexOf(playerColor);
			if (index == -1)
			{
				//Ensure that the first two tanks aren't both the same colours
				index = Random.Range(0, m_Colors.Count);
				while (index == m_LastUsedColorIndex)
				{
					index = Random.Range(0, m_Colors.Count);
				}
				m_LastUsedColorIndex = index;
			}
			else
			{
				index++;
			}


			if (index == m_Colors.Count)
			{
				index = 0;
			}

			Color newColor = m_Colors[index];
			if (CanUseColor(newColor, playerIndex))
			{
				return newColor;
			}

			return playerColor;
		}

		public void SetupColors(List<Color> colors)
		{
			this.m_Colors = colors;
		}
		
		//Ensures that there are least two teams
		private bool CanUseColor(Color newColor, int playerIndex)
		{
			if (m_Players.Count == 1)
			{
				return true;
			}

			for (int i = 0; i < m_Players.Count; i++)
			{
				if (i != playerIndex)
				{
					if (m_Players[i].color != newColor)
					{
						return true;
					}
				}
			}

			return false;
		}

		public void Reset()
		{
			m_Players.Clear();
			m_LastUsedColorIndex = -1;
		}
	}
}