using UnityEngine;
using System.Collections;
using Tanks.TankControllers;
using Tanks.SinglePlayer;

namespace Tanks.Rules.SinglePlayer.Objectives
{
	/// <summary>
	/// Collection objective - collect n items
	/// </summary>
	public class Collection : Objective
	{
		//Set this value to 1 to have a get to location game
		[SerializeField]
		protected int m_CollectionLimit = 1;

		private int m_Collected = 0;

		//Used for collecting an item
		public override void EntersZone(GameObject zoneObject, TargetZone zone)
		{
			Collectible collectible = zone as Collectible;
			if (collectible == null)
			{
				return;
			}

			TankDisplay tank = zoneObject.GetComponent<TankDisplay>();
			if (tank == null)
			{
				return;
			}
            
			m_Collected++;
            
			if (m_Collected >= m_CollectionLimit)
			{
				Achieved();
			}          
		}

		public override string objectiveDescription
		{
			get
			{
				if (m_CollectionLimit > 1)
				{
					return string.Format("Collect {0} Drop Pods", m_CollectionLimit);
				}
				else
				{
					return string.Format("Collect {0} Drop Pod", m_CollectionLimit);
				}
			}
		}

		public override string objectiveSummary
		{
			get { return objectiveDescription; }
		}
	}
}