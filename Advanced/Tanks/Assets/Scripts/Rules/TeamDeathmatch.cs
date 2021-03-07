using UnityEngine;
using Tanks.UI;
using Tanks.TankControllers;
using System.Collections.Generic;

namespace Tanks.Rules
{
	/// <summary>
	/// Team deathmatch.
	/// </summary>
	public class TeamDeathmatch : RulesProcessor
	{
		/// <summary>
		/// The kill limit - i.e. ScoreTarget
		/// </summary>
		[SerializeField]
		private int m_KillLimit = 20;

		/// <summary>
		/// Gets the score target.
		/// </summary>
		/// <value>The score target.</value>
		public override int scoreTarget
		{
			get{ return m_KillLimit; }
		}

		/// <summary>
		/// List of team objects
		/// </summary>
		[SerializeField]
		private List<Team> m_Teams;

		/// <summary>
		/// The winning team.
		/// </summary>
		protected Team m_WinningTeam = null;

		/// <summary>
		/// The team dictionary - maps a team to a colour
		/// </summary>
		protected Dictionary<Color,Team> m_TeamDictionary = new Dictionary<Color, Team>();

		/// <summary>
		/// Gets the winner identifier - overloaded as the team
		/// </summary>
		/// <value>The winner identifier.</value>
		public override string winnerId
		{
			get
			{
				return m_WinningTeam.teamName;
			}
		}

		/// <summary>
		/// Gets the colors of the teams in the game
		/// </summary>
		/// <returns>The colors.</returns>
		protected List<Color> GetColors()
		{
			List<Color> colors = new List<Color>();
			for (int i = 0; i < m_Teams.Count; i++)
			{
				Team team = m_Teams[i];
				Color teamColor = team.teamColor;
				colors.Add(teamColor);
			}
			return colors;
		}

		/// <summary>
		/// Unity message: Awake
		/// Set up the teams dictionary
		/// </summary>
		protected virtual void Awake()
		{
			for (int i = 0; i < m_Teams.Count; i++)
			{
				Team team = m_Teams[i];
				Color teamColor = team.teamColor;
				if (!m_TeamDictionary.ContainsKey(teamColor))
				{
					m_TeamDictionary.Add(teamColor, team);
				}
			}
		}

		/// <summary>
		/// Setups the color provider.
		/// </summary>
		protected override void SetupColorProvider()
		{
			if (m_ColorProvider == null)
			{
				m_ColorProvider = new TeamColorProvider();
				((TeamColorProvider)m_ColorProvider).SetupColors(GetColors());
			}
		}

		/// <summary>
		/// Increments the team score.
		/// </summary>
		/// <param name="killer">Tank that did the killing</param>
		/// <param name="killed">Tank that was killed</param>
		protected virtual void IncrementTeamScore(TankManager killer, TankManager killed)
		{
			Color key = killer.playerColor;
			if (m_TeamDictionary.ContainsKey(key))
			{
				if (key != killed.playerColor)
				{
					m_TeamDictionary[key].IncrementScore();
					if (m_TeamDictionary[key].score >= m_KillLimit)
					{
						m_MatchOver = true;
						m_WinningTeam = m_TeamDictionary[key];
					}
				}
			}
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
		/// Handles the death of a tank
		/// </summary>
		/// <param name="tank">Tank.</param>
		public override void TankDies(TankManager tank)
		{
			base.TankDies(tank);
			m_GameManager.RespawnTank(tank.playerNumber);
		}

		/// <summary>
		/// Called when a tank disconnects
		/// If all the tanks of a team have quit then handle as everyone bailed
		/// </summary>
		/// <param name="tank">The tank that disconnects</param>
		public override void TankDisconnected(TankManager tank)
		{
			base.TankDisconnected(tank);

			Color[] teamColours = GetColors().ToArray();
			int[] colourCount = new int[m_Teams.Count];

			int tankNumber = GameManager.s_Tanks.Count;

			//Iterate through all remaining tanks and aggregate how many tanks we have left for each team colour.
			for (int i = 0; i < tankNumber; i++)
			{
				for (int j = 0; j < teamColours.Length; j++)
				{
					if (GameManager.s_Tanks[i].playerColor == teamColours[j])
					{
						colourCount[j]++;
					}
				}
			}

			//Check our colour counts.
			int validTeamCount = 0;

			for (int i = 0; i < colourCount.Length; i++)
			{
				if (colourCount[i] > 0)
				{
					validTeamCount++;
				}
			}

			//If we only have one valid team, treat it as if everyone has bailed and end the game.
			if (validTeamCount == 1)
			{
				m_GameManager.HandleEveryoneBailed();
			}
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
		/// Returns elements for constructing the leaderboard - based on team and
		/// </summary>
		/// <returns>The leaderboard elements.</returns>
		public override List<LeaderboardElement> GetLeaderboardElements()
		{
			List<LeaderboardElement> leaderboardElements = new List<LeaderboardElement>();

			//Cache the color score dictionary from the gameManager - this is guaranteed to have the correct scores and actually is synced across the network
			Dictionary<Color,int> colorScoreDictionary = m_GameManager.colorScoreDictionary;

			//Cache the list length for efficiency
			int teamCount = m_Teams.Count;		

			//Iterate through list of teams
			for (int i = 0; i < teamCount; ++i)
			{
				//Cache the team object as index i - this is because indexing is not a free action
				Team team = m_Teams[i];
				int score = team.score;

				//check for score in the color score dictionary and use it if found
				if (colorScoreDictionary.ContainsKey(team.teamColor))
				{
					score = colorScoreDictionary[team.teamColor];
				}

				//Create the leaderboard element
				LeaderboardElement leaderboardElement = new LeaderboardElement(team.teamName, team.teamColor, score);
				//Add it to list
				leaderboardElements.Add(leaderboardElement);
			}
		
			//Ensure the leaderboard elements are sorted by score
			leaderboardElements.Sort(LeaderboardSort);
			return leaderboardElements;
		}

		/// <summary>
		/// Handles the killer score - this different per game mode
		/// </summary>
		/// <param name="killer">Tank that did the killing</param>
		/// <param name="killed">Tank that was killed</param>
		public override void HandleKillerScore(TankManager killer, TankManager killed)
		{
			//if you kill your own team mate or yourself you should lose points (personal kill count, not team kill count)
			if (killer.playerColor == killed.playerColor)
			{
				killer.DecrementScore();
			}
			//otherwise you should get points
			else
			{
				killer.IncrementScore();
			}

			IncrementTeamScore(killer, killed);

			RegenerateHudScoreList();
		}

		//This override generates score arrays by team colour/score rather than by individual tank.
		public override void RegenerateHudScoreList()
		{
			int totalTeams = m_TeamDictionary.Count;

			Color[] colorList = GetColors().ToArray();
			int[] scoreList = new int[totalTeams];

			for (int i = 0; i < totalTeams; i++)
			{
				scoreList[i] = m_TeamDictionary[colorList[i]].score;
			}

			m_GameManager.UpdateHudScore(colorList, scoreList);
		}

		/// <summary>
		/// Gets the rank of a player given the tank index
		/// </summary>
		/// <returns>The rank.</returns>
		/// <param name="tankIndex">Tank index.</param>
		public override int GetRank(int tankIndex)
		{
			//if your tank color is the same as the winning team color then you are on the winning team and then your rank is one
			if (GameManager.s_Tanks[tankIndex].playerColor == m_WinningTeam.teamColor)
			{
				return 1;
			}

			return 2;
		}

		/// <summary>
		/// Gets the award text based on the rank
		/// </summary>
		/// <returns>The award text.</returns>
		/// <param name="rank">Rank.</param>
		public override string GetAwardText(int rank)
		{
			//if your rank is 1 then you are on the winning team
			if (rank == 1)
			{
				return "Your team won ";
			}
			return "Your team lost ";
		}

		/// <summary>
		/// Gets the award amount based on the rank
		/// </summary>
		/// <returns>The award amount.</returns>
		/// <param name="rank">Rank.</param>
		public override int GetAwardAmount(int rank)
		{
			//Each player on winning team gets 100 coins
			//Each player on the losing team gets 25 coins
			int divisor = 1 + ((rank - 1) * 3);
			return 100 / divisor;
		}
	}
}
