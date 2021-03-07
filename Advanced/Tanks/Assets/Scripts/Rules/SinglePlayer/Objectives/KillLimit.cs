using UnityEngine;
using System.Collections;
using Tanks.SinglePlayer;

namespace Tanks.Rules.SinglePlayer.Objectives
{
	/// <summary>
	/// Kill limit - generic base class for handling kills of NPCs
	/// </summary>
	public class KillLimit<T> : Objective where T : Npc
	{
		[SerializeField]
		protected int m_KillsForSuccess = 3;

		[SerializeField]
		protected bool m_IsSecondaryEnemy;

		private int m_CurrentKills = 0;

		public override void DestroyNpc(Npc npc)
		{
			T killed = npc as T;
            
			if (killed != null)
			{
				m_CurrentKills++;
				if (m_CurrentKills >= m_KillsForSuccess)
				{
					Achieved();
				}
			}
		}

		public override string objectiveDescription
		{
			get
			{ 
				if (m_KillsForSuccess > 1)
				{
					if (!m_IsSecondaryEnemy)
					{
						return string.Format("Kill {0} enemies", m_KillsForSuccess);
					}
					else
					{
						//If the "additional" tank is an escort, we pass the total number of tanks-1 since the primary Chase target would trigger success otherwise.
						return string.Format("Kill {0} escorting tank", (m_KillsForSuccess - 1));
					}
				}
				else
					return string.Format("Kill {0} enemy", m_KillsForSuccess);
			}
		}

		public override string objectiveSummary
		{
			get { return objectiveDescription; }
		}
	}
}
