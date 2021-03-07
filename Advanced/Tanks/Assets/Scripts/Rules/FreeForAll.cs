using UnityEngine;
using Tanks.TankControllers;

namespace Tanks.Rules
{
	/// <summary>
	/// Deathmatch rules processor - used to be called free-for-all
	/// </summary>
	public class FreeForAll : RulesProcessor
	{
		/// <summary>
		/// The configurable kill limit i.e. ScoreTarget
		/// </summary>
		[SerializeField]
		protected int m_KillLimit = 20;

		/// <summary>
		/// Overriden score target
		/// </summary>
		/// <value>The score target.</value>
		public override int scoreTarget
		{
			get{ return m_KillLimit; }
		}

		/// <summary>
		/// Function called on round start
		/// </summary>
		public override void StartRound()
		{
			base.StartRound();

			RegenerateHudScoreList();
		}

		/// <summary>
		/// Handles the death of a tank - i.e. respawn 
		/// </summary>
		/// <param name="tank">Tank.</param>
		public override void TankDies(TankManager tank)
		{
			base.TankDies(tank);
			m_GameManager.RespawnTank(tank.playerNumber);
		}

		/// <summary>
		/// Determines whether it is end of round - death match only has one round so this occurs when the match is over
		/// </summary>
		/// <returns>true</returns>
		/// <c>false</c>
		public override bool IsEndOfRound()
		{
			return m_MatchOver;
		}

		/// <summary>
		/// Handles the killer score - killer's score is increased for Deathmatch
		/// </summary>
		/// <param name="killer">Tank that did the killing</param>
		/// <param name="killed">Tank that was killed</param>
		public override void HandleKillerScore(TankManager killer, TankManager killed)
		{
			killer.IncrementScore();

			if (killer.score >= m_KillLimit)
			{
				m_Winner = killer;
				m_MatchOver = true;
			}
		}

		/// <summary>
		/// Handles the player's suicide - for Deathmatch the score is decremented
		/// </summary>
		/// <param name="killer">The tank that kill themself</param>
		public override void HandleSuicide(TankManager killer)
		{
			killer.DecrementScore();
		}

		/// <summary>
		/// Called when a tank disconnects
		/// </summary>
		/// <param name="tank">The tank that disconnects</param>
		public override void TankDisconnected(TankManager tank)
		{
			base.TankDisconnected(tank);

			RegenerateHudScoreList();
		}
	}
}