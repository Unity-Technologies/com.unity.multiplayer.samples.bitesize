using UnityEngine;
using System.Collections;
using Tanks.TankControllers;
using System.Collections.Generic;

namespace Tanks.Rules
{
	/// <summary>
	/// Rules processor for the last man standing game mode
	/// </summary>
	public class LastManStanding : RulesProcessor
	{
		/// <summary>
		/// The rounds to win - i.e. ScoreTarget
		/// </summary>
		[SerializeField]
		protected int m_RoundsToWin = 5;
	
		/// <summary>
		/// The number of dead tanks.
		/// </summary>
		private int m_NumTanksDead = 0;

		/// <summary>
		/// Local copy of list of in game tanks
		/// </summary>
		private List<TankManager> m_Tanks;

		/// <summary>
		/// The round winner.
		/// </summary>
		private TankManager m_RoundWinner;

		/// <summary>
		/// Gets the score target.
		/// </summary>
		/// <value>The score target.</value>
		public override int scoreTarget
		{
			get{ return m_RoundsToWin; }
		}

		/// <summary>
		/// Function called on round start
		/// </summary>
		public override void StartRound()
		{
			m_NumTanksDead = 0;
			m_Tanks = new List<TankManager>(GameManager.s_Tanks);

			RegenerateHudScoreList();
		}

		/// <summary>
		/// Handles the death of a tank - the tank is removed from the local list
		/// </summary>
		/// <param name="tank">Tank.</param>
		public override void TankDies(TankManager tank)
		{
			base.TankDies(tank);
			m_Tanks.Remove(tank);
			m_NumTanksDead++;
		}

		/// <summary>
		/// Called when a tank disconnects - removed from the local list
		/// </summary>
		/// <param name="tank">The tank that disconnects</param>
		public override void TankDisconnected(TankManager tank)
		{
			base.TankDisconnected(tank);
			int index = m_Tanks.IndexOf(tank);
			if (index != -1)
			{
				m_Tanks.RemoveAt(index);
			}

			RegenerateHudScoreList();
		}

		/// <summary>
		/// Determines whether it is end of round - if there is one or no players
		/// </summary>
		/// <returns>true</returns>
		/// <c>false</c>
		public override bool IsEndOfRound()
		{
			return m_NumTanksDead >= GameManager.s_Tanks.Count - 1;
		}

		/// <summary>
		/// Handles the round end.
		/// </summary>
		public override void HandleRoundEnd()
		{
			// Clear the winner from the previous round.
			m_RoundWinner = null;
    
			// See if there is a winner now the round is over.
			m_RoundWinner = GetRoundWinner();
    
			// If there is a winner, increment their score.
			if (m_RoundWinner != null)
			{
				m_RoundWinner.IncrementScore();

				if (m_RoundWinner.score >= m_RoundsToWin)
				{
					m_Winner = m_RoundWinner;
					m_MatchOver = true;
				}
			}

			if (!m_MatchOver)
			{
				m_GameManager.ServerResetAllTanks();
			}
		}

		/// <summary>
		/// Gets the round end text - winner or draw if appropriate
		/// </summary>
		/// <returns>The round end text.</returns>
		public override string GetRoundEndText()
		{
			string message = "DRAW!";
			if (m_RoundWinner != null)
			{
				message = string.Format("{0} wins the round!", m_Tanks[0].playerName);
			}

			return message;
		}
		
		//No implementation of the HandleSuicide as it was decided that eliminating yourself from the round is enough of a punishment

		// This function is to find out if there is a winner of the round.
		// This function is called with the assumption that 1 or fewer tanks are currently active.
		private TankManager GetRoundWinner()
		{
			if (m_Tanks.Count == 0)
			{
				return null;
			}
            
			return m_Tanks[0];
		}
	}
}