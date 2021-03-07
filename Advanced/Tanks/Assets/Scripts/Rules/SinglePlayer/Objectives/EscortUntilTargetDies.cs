using UnityEngine;
using System.Collections;
using Tanks.SinglePlayer;

namespace Tanks.Rules.SinglePlayer.Objectives
{
	public class EscortUntilTargetDies : Objective
	{
		private bool m_FailedObjective;

		/// <summary>
		/// Awards the achievement if the main target dies, but escort target does not.
		/// </summary>
		/// <param name="npc">Npc.</param>
		public override void DestroyNpc(Npc npc)
		{
			
			TargetNpc target = npc as TargetNpc;
			if (target != null)
			{
				if (!target.isPrimaryObjective)
				{
					m_FailedObjective = true;
				}

				if (target.isPrimaryObjective && !m_FailedObjective)
				{
					Achieved();
				}
			}
		}

		public override string objectiveDescription
		{
			get { return "Escort the VIP to the drop zone"; }
		}

		public override string objectiveSummary
		{
			get { return "Escort VIP"; }
		}
	}
}