using UnityEngine;
using System.Collections;
using Tanks.SinglePlayer;
using Tanks.TankControllers;

namespace Tanks.Rules.SinglePlayer.Objectives
{
	/// <summary>
	/// Get to location - special case collection objective, where the collection limit is 1 item
	/// </summary>
	public class GetToLocation : Objective
	{
		//Used for collecting an item
		public override void EntersZone(GameObject zoneObject, TargetZone zone)
		{
			Collectible collectible = zone as Collectible;
			if(collectible != null)
			{
				return;
			}

			TankDisplay tank = zoneObject.GetComponent<TankDisplay>();
			if (tank == null)
			{
				return;
			}

			Achieved();        
		}

		public override string objectiveDescription
		{
			get { return "Get To Location"; }
		}

		public override string objectiveSummary
		{
			get { return objectiveDescription; }
		}
	}
}
