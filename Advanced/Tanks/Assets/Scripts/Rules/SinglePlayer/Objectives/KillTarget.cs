using UnityEngine;
using System.Collections;
using Tanks.SinglePlayer;

namespace Tanks.Rules.SinglePlayer.Objectives
{
	/// <summary>
	/// Kill target objective - achieved when the player destroys a specific NPC
	/// </summary>
	public class KillTarget : Objective
	{
		/// <summary>
		/// Handles the NPC being destroyed
		/// </summary>
		/// <param name="npc">Npc.</param>
		public override void DestroyNpc(Npc npc)
		{
			TargetNpc target = npc as TargetNpc;
			if (target != null)
			{
				if (target.isPrimaryObjective)
				{
					Achieved();
				}       
			}
		}

		public override string objectiveDescription
		{
			get { return "Assassinate Target!"; }
		}

		public override string objectiveSummary
		{
			get { return objectiveDescription; }
		}
	}
}
