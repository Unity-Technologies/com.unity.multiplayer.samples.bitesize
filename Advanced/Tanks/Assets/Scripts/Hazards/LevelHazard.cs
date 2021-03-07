using UnityEngine;
using System.Collections;
using MLAPI;

namespace Tanks.Hazards
{
	// A base class to handle registration and define reset for all environmental hazards that are to be reset each game round.
	// This is intended only to be used on the server-authoritative version of the object.
	public class LevelHazard : NetworkBehaviour 
	{
		protected virtual void Start () 
		{
			if (!IsServer)
				return;

			GameManager.s_Instance.AddHazard(this);
		}

		protected virtual void OnDestroy()
		{
			if (!IsServer)
				return;

			GameManager.s_Instance.RemoveHazard(this);
		}
			
		// Reset code that is called on the hazard at the beginning of a round.
		public virtual void ResetHazard(){}


		// Secondary activation method for when the game manager cedes control to players. Useful for hazards that may trigger erroneously during reset logic.
		public virtual void ActivateHazard(){}
	}
}
