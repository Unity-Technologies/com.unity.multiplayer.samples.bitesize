using UnityEngine;
using System.Collections;

namespace Tanks.SinglePlayer
{
	/// <summary>
	/// A specific NPC - used in chase and VIP missions
	/// </summary>
	public class TargetNpc : Npc
	{
		[SerializeField]
		protected bool m_IsPrimaryObjective = false;

		public bool isPrimaryObjective
		{
			get { return m_IsPrimaryObjective; }
		}
	}
}
