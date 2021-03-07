using UnityEngine;
using System.Collections;
using Tanks.SinglePlayer;

namespace Tanks.Rules.SinglePlayer.Objectives
{
	/// <summary>
	/// Enum for representing if the objective has been achieved and how (in the current game or previously) 
	/// </summary>
	public enum LockState
	{
		Locked,
		PreviouslyUnlocked,
		NewlyUnlocked
	}

	/// <summary>
	/// A representation of an objective
	/// </summary>
	public class Objective : MonoBehaviour
	{
		/// <summary>
		/// Is this objective the primary objective - i.e. will it end the level if it is passed or failed
		/// </summary>
		[SerializeField]
		protected bool m_IsPrimaryObjective;

		/// <summary>
		/// The gear reward for completing the objective - this is awarded once off.
		/// </summary>
		[SerializeField]
		protected int m_GearReward = 100;

		protected LockState m_LockState = LockState.Locked;

		/// <summary>
		/// The single player rule processor - the objective calls success/failure methods on the rules processr
		/// </summary>
		protected SinglePlayerRulesProcessor m_RulesProcessor;

		public int currencyReward
		{
			get { return m_GearReward; }
		}

		public virtual LockState lockState
		{
			get { return m_LockState; }
		}

		public bool isPrimaryObjective
		{
			get
			{
				return m_IsPrimaryObjective;
			}
		}

		/// <summary>
		/// The long description of what needs to be done. This is overloaded in the child classes
		/// </summary>
		/// <value>The objective description.</value>
		public virtual string objectiveDescription
		{
			get { return "An Objective Description"; }
		}

		/// <summary>
		/// Summary of what needs to be done - this is used in some instances by the UI
		/// </summary>
		/// <value>The objective summary.</value>
		public virtual string objectiveSummary
		{
			get { return "Short Summary"; }
		}

		/// <summary>
		/// Handles a game object (e.g. NPC or player) entering a zone
		/// </summary>
		/// <param name="zoneObject">Zone object.</param>
		public virtual void EntersZone(GameObject zoneObject, TargetZone zone)
		{ 
		}

		/// <summary>
		/// Handles the NPC being destroyed
		/// </summary>
		/// <param name="npc">Npc.</param>
		public virtual void DestroyNpc(Npc npc)
		{
		}

		/// <summary>
		/// Sets the lock state to previously unlocked. This is needed for persistence
		/// </summary>
		public void SetPreviouslyUnlocked()
		{
			m_LockState = LockState.PreviouslyUnlocked;
		}

		/// <summary>
		/// Sets the rules processor.
		/// </summary>
		/// <param name="rulesProcessor">Rules processor.</param>
		public virtual void SetRulesProcessor(SinglePlayerRulesProcessor rulesProcessor)
		{
			this.m_RulesProcessor = rulesProcessor;
		}

		/// <summary>
		/// Function run when the objective is achieved
		/// </summary>
		protected void Achieved()
		{
			TrySetNewlyUnlocked();
			m_RulesProcessor.ObjectiveAchieved(this);
		}

		/// <summary>
		/// Function run when the objective is failed
		/// </summary>
		protected void Failed()
		{
			TrySetLocked();
			m_RulesProcessor.ObjectiveFailed(this);
		}

		/// <summary>
		/// Convenience function for safe lock state change
		/// </summary>
		protected void TrySetNewlyUnlocked()
		{
			TrySetLockState(LockState.NewlyUnlocked);
		}

		/// <summary>
		/// Convenience function for safe lock state change
		/// </summary>
		protected void TrySetLocked()
		{
			TrySetLockState(LockState.Locked);
		}

		/// <summary>
		/// Convenience function for safe lock state change
		/// </summary>
		/// <param name="newState">New state.</param>
		/// <param name="blockingState">Blocking state.</param>
		protected void TrySetLockState(LockState newState, LockState blockingState = LockState.PreviouslyUnlocked)
		{
			if (m_LockState == blockingState)
			{
				return;
			}

			m_LockState = newState;
		}
	}
}