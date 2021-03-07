using UnityEngine;
using System.Collections;
using Tanks.SinglePlayer;

namespace Tanks.Rules.SinglePlayer.Objectives
{
	/// <summary>
	/// Chase objective - stop a TargetNpc from getting to a location
	/// </summary>
	public class Chase : KillTarget
	{
		public override string objectiveDescription
		{
			get { return "Kill the target before it reaches its destination"; }
		}

		public override string objectiveSummary
		{
			get { return "Kill target"; }
		}

		/// <summary>
		/// Game is failed when the primary target enters the zone
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
				Failed();
			}  
		}
	}
}
