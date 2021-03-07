using UnityEngine;
using Tanks.TankControllers;
using Tanks.SinglePlayer;
using Tanks.UI;

namespace Tanks.Rules.SinglePlayer
{
	/// <summary>
	/// Offline rules processor - abstract class for all offline modes - i.e. single player and shooting mini-game
	/// </summary>
	public abstract class OfflineRulesProcessor : RulesProcessor
	{
		/// <summary>
		/// The end game modal - if this is not specified a fallback modal is used
		/// </summary>
		[SerializeField]
		protected EndGameModal m_EndGameModal;

		/// <summary>
		/// The start game modal.
		/// </summary>
		[SerializeField]
		protected StartGameModal m_StartGameModal;

		/// <summary>
		/// Was the mission failed
		/// </summary>
		protected bool m_MissionFailed = false;

		#region Properties
		/// <summary>
		/// The player tank
		/// </summary>
		protected TankManager m_PlayerTank;

		public TankManager playerTank
		{
			get
			{
				LazyLoadPlayerTank();
				return m_PlayerTank;
			}
		}
		public EndGameModal endGameModal
		{
			get
			{
				return m_EndGameModal;
			}
		}
		public StartGameModal startGameModal
		{
			get
			{
				return m_StartGameModal;
			}
		}

		/// <summary>
		/// Flag to determine whether we can start the game
		/// </summary>
		protected bool m_CanStartGame = false;
		public override bool canStartGame
		{
			get { return m_CanStartGame; }
		}

		/// <summary>
		/// Gets a value indicating whether this instance has winner.
		/// </summary>
		/// <value><c>true</c> if this instance has winner; otherwise, <c>false</c>.</value>
		public override bool hasWinner
		{
			get { return !m_MissionFailed; }
		}
		#endregion


		#region Methods
		/// <summary>
		/// Handles destructions of NPC
		/// </summary>
		/// <param name="npc">Npc.</param>
		public abstract void DestroyNpc(Npc npc);

		/// <summary>
		/// Handles a game object entering the zone
		/// </summary>
		/// <param name="zoneObject">Zone object.</param>
		/// <param name="zone">Zone.</param>
		public abstract void EntersZone(GameObject zoneObject, TargetZone zone);
		#endregion


		#region Concrete methods
		/// <summary>
		/// Called on click START button in start game modal
		/// </summary>
		public void StartGame()
		{
			m_CanStartGame = true;
		}

		/// <summary>
		/// Logic fired on reset of game
		/// </summary>
		public virtual void ResetGame()
		{
			
		}

		/// <summary>
		/// Determines whether it is end of round.
		/// </summary>
		/// <returns>true</returns>
		/// <c>false</c>
		public override bool IsEndOfRound()
		{
			return m_MatchOver;
		}

		/// <summary>
		/// Lazy loads player tank.
		/// </summary>
		protected void LazyLoadPlayerTank()
		{
			if (m_PlayerTank != null)
			{
				return;
			}

			if (GameManager.s_Tanks.Count > 1)
			{
				Debug.LogWarning("Can't be more than one player in single player game");
			}

			if (GameManager.s_Tanks.Count == 1)
			{
				m_PlayerTank = GameManager.s_Tanks[0];
			}
		}
		#endregion
	}
}