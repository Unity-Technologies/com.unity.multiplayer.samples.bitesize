using System.Collections.Generic;
using UnityEngine;
using Tanks.TankControllers;
using Tanks.UI;

namespace Tanks.Rules
{
	/// <summary>
	/// Rules processor - a base class for all game modes including single player and multiplayer
	/// </summary>
	public class RulesProcessor : MonoBehaviour
	{
		//The transition time from once the game ends until the game exits
		public static float s_EndGameTime = 10f;

		//The scene in the menu to return to
		[SerializeField]
		protected MenuPage m_ReturnPage;

		public MenuPage returnPage
		{
			get { return m_ReturnPage; }
		}

		//Reference to the game manager
		protected GameManager m_GameManager;

		//Winning tank - null if no winner
		protected TankManager m_Winner;

		//Is the match over
		protected bool m_MatchOver = false;

		//If the match is over
		public bool matchOver
		{
			get{ return m_MatchOver; }
		}

		//The color provider - this is used in the lobby to provide different color selection implementation based on the game mode (i.e. individual versus team-based modes)
		protected IColorProvider m_ColorProvider;

		//The score target - how many points (kills or team kills) before the game is won
		public virtual int scoreTarget
		{
			get{ return 0; }
		}

		//Whether the game can be started - Offline (single player) game modes implement this to prevent the game from being started until a button is pressed
		public virtual bool canStartGame
		{
			get { return true; }
		}

		//Check if the game has a winner
		public virtual bool hasWinner
		{
			get { return m_Winner != null; }
		}

		//The id of the winner.
		public virtual string winnerId
		{
			get
			{
				return m_Winner.playerTankType.id;
			}
		}

		/// <summary>
		/// Gets the round message.
		/// </summary>
		/// <returns>The round message.</returns>
		public virtual string GetRoundMessage()
		{
			return string.Empty;
		}

		/// <summary>
		/// Sets the game manager.
		/// </summary>
		/// <param name="gameManager">Game manager.</param>
		public void SetGameManager(GameManager gameManager)
		{
			if (this.m_GameManager == null)
			{
				this.m_GameManager = gameManager;
			}
		}

		/// <summary>
		/// Determines whether it is end of round.
		/// </summary>
		/// <returns><c>true</c> if is end of round; otherwise, <c>false</c>.</returns>
		public virtual bool IsEndOfRound()
		{
			return false;
		}

		/// <summary>
		/// Function called on round start
		/// </summary>
		public virtual void StartRound()
		{
		}

		/// <summary>
		/// Called on Match end
		/// </summary>
		public virtual void MatchEnd()
		{
		}

		/// <summary>
		/// Handles the death of a tank
		/// </summary>
		/// <param name="tank">Tank.</param>
		public virtual void TankDies(TankManager tank)
		{
			m_GameManager.HandleKill(tank);
		}

		/// <summary>
		/// Handles the killer score - this different per game mode
		/// </summary>
		/// <param name="killer">Tank that did the killing</param>
		/// <param name="killed">Tank that was killed</param>
		public virtual void HandleKillerScore(TankManager killer, TankManager killed)
		{
		}

		/// <summary>
		/// Handles the player's suicide - this different per game mode
		/// </summary>
		/// <param name="killer">The tank that kill themself</param>
		public virtual void HandleSuicide(TankManager killer)
		{
		}

		/// <summary>
		/// Called when a tank disconnects
		/// </summary>
		/// <param name="tank">The tank that disconnects</param>
		public virtual void TankDisconnected(TankManager tank)
		{         
		}

		/// <summary>
		/// Handles the round end.
		/// </summary>
		public virtual void HandleRoundEnd()
		{
		}

		/// <summary>
		/// Gets the round end text.
		/// </summary>
		/// <returns>The round end text.</returns>
		public virtual string GetRoundEndText()
		{
			return string.Empty;
		}

		/// <summary>
		/// Returns elements for constructing the leaderboard
		/// </summary>
		/// <returns>The leaderboard elements.</returns>
		public virtual List<LeaderboardElement> GetLeaderboardElements()
		{
			List<LeaderboardElement> leaderboardElements = new List<LeaderboardElement>();

			List<TankManager> matchTanks = GameManager.s_Tanks;
			int tankCount = matchTanks.Count;
			
			for (int i = 0; i < tankCount; ++i)
			{
				TankManager currentTank = matchTanks[i];
				LeaderboardElement leaderboardElement = new LeaderboardElement(currentTank.playerName, currentTank.playerColor, currentTank.score);
				leaderboardElements.Add(leaderboardElement);
			}

			leaderboardElements.Sort(LeaderboardSort);
			return leaderboardElements;
		}

		/// <summary>
		/// Used for sorting the leaderboard
		/// </summary>
		/// <returns>The sort.</returns>
		/// <param name="player1">Player1.</param>
		/// <param name="player2">Player2.</param>
		protected int LeaderboardSort(LeaderboardElement player1, LeaderboardElement player2)
		{
			return player2.score - player1.score;
		}

		/// <summary>
		/// Gets the color provider.
		/// </summary>
		/// <returns>The color provider.</returns>
		public IColorProvider GetColorProvider()
		{
			SetupColorProvider();
			return m_ColorProvider;
		}

		/// <summary>
		/// Setups the color provider.
		/// </summary>
		protected virtual void SetupColorProvider()
		{
			if (m_ColorProvider == null)
			{
				m_ColorProvider = new PlayerColorProvider();
			}
		}

		/// <summary>
		/// Handles bailing (i.e. leaving the game)
		/// </summary>
		public virtual void Bail()
		{
			m_GameManager.ExitGame(m_ReturnPage);
		}

		/// <summary>
		/// Handles the game being complete (including the transitions)
		/// </summary>
		public virtual void CompleteGame()
		{
			m_GameManager.ExitGame(m_ReturnPage);
		}

			
		//Generates two arrays of player colours and their game scores, and passes them to the GameManager to update client HUDs.
		//The GameManager has pre-sorted the tanks by score by the time this is called.
		public virtual void RegenerateHudScoreList()
		{
			if (m_GameManager == null)
			{
				return;   
			}

			int totalTanks = GameManager.s_Tanks.Count;

			Color[] colorList = new Color[totalTanks];
			int[] scoreList = new int[totalTanks];

			for (int i = 0; i < totalTanks; i++)
			{
				colorList[i] = GameManager.s_Tanks[i].playerColor;
				scoreList[i] = GameManager.s_Tanks[i].score;
			}

			m_GameManager.UpdateHudScore(colorList, scoreList);
		}

		/// <summary>
		/// Gets the rank of a player given the tank index
		/// </summary>
		/// <returns>The rank.</returns>
		/// <param name="tankIndex">Tank index.</param>
		public virtual int GetRank(int tankIndex)
		{
			return tankIndex + 1;
		}

		/// <summary>
		/// Gets the award text based on the rank
		/// </summary>
		/// <returns>The award text.</returns>
		/// <param name="rank">Rank.</param>
		public virtual string GetAwardText(int rank)
		{
			string[] rankSuffix = new string[]{ "st", "nd", "rd", "th" };
			return string.Format("You ranked {0}{1}", rank, rankSuffix[rank - 1]);
		}

		/// <summary>
		/// Gets the award amount based on the rank
		/// </summary>
		/// <returns>The award amount.</returns>
		/// <param name="rank">Rank.</param>
		public virtual int GetAwardAmount(int rank)
		{
			return Mathf.FloorToInt(100 / Mathf.Pow(2f, (float)(rank - 1)));
		}
	}
}