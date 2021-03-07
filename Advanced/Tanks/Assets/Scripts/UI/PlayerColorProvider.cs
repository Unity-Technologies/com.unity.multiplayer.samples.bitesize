using UnityEngine;
using System.Collections.Generic;
using TanksNetworkPlayer = Tanks.Networking.NetworkPlayer;

namespace Tanks.UI
{
	/// <summary>
	/// Color provider used by not team based games
	/// </summary>
	public class PlayerColorProvider : IColorProvider
	{
		private Color[] m_Colors = new Color[] { Color.magenta, Color.red, Color.cyan, Color.green, Color.yellow, Color.gray };

		//used on server to avoid assigning the same color to two player
		private List<int> m_ColorsInUse = new List<int>();

		//Gets an unused color for the player
		public Color ServerGetColor(TanksNetworkPlayer player)
		{
			Color playerColor = player.color;
			int index = System.Array.IndexOf(m_Colors, playerColor);
			if (index == -1)
			{
				index = Random.Range(0, m_Colors.Length);
			}
			else
			{
				m_ColorsInUse.Remove(index);
			}

			int uniqueIndex = index;

			bool isUnique = false;

			while (!isUnique)
			{
				uniqueIndex++;
				if (uniqueIndex == m_Colors.Length)
				{
					uniqueIndex = 0;
				}

				if (!m_ColorsInUse.Contains(uniqueIndex))
				{
					isUnique = true;
					m_ColorsInUse.Add(uniqueIndex);
				}
			}

			return m_Colors[uniqueIndex];
		}

		public void Reset()
		{
			m_ColorsInUse.Clear();
		}
	}
}
