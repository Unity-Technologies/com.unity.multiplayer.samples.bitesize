using UnityEngine;
using System.Collections;
using Tanks.SinglePlayer;

namespace Tanks.Rules.SinglePlayer.Objectives
{
	/// <summary>
	/// Escort objective - ensure the VIP reaches its objective
	/// </summary>
	public class Escort : Objective
	{
		/// <summary>
		/// Fails the mission if the VIP dies
		/// </summary>
		/// <param name="npc">Npc.</param>
		public override void DestroyNpc(Npc npc)
		{
			TargetNpc target = npc as TargetNpc;
			if (target != null)
			{
				if (target.isPrimaryObjective)
				{
					Failed();
				}   
			}
		}

		/// <summary>
		/// Passes the mission if the VIP reaches the end zone
		/// </summary>
		/// <param name="zoneObject">Zone object.</param>
		public override void EntersZone(GameObject zoneObject, TargetZone zone)
		{
			TargetNpc target = zoneObject.GetComponent<TargetNpc>();
			if (target == null)
			{
				return;
			}

			if (target.isPrimaryObjective)
			{
				Achieved();
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
