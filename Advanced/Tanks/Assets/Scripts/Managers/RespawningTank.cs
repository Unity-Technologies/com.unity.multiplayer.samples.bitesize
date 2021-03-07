using UnityEngine;
using System;
using Tanks.TankControllers;
using Tanks.Map;
using Tanks.UI;

namespace Tanks
{
	/// <summary>
	/// A helper object that goes through the process of respawning a tank by doing multiple timed transitions
	/// </summary>
	public class RespawningTank : MonoBehaviour
	{
		//Delays for certain events occuring - this allows time transitions
		[SerializeField]
		protected float m_ShowLeaderboardDelay = 1f, m_SpawnDelay = 3.5f, m_SpawnReactivateDelay = 0.5f;
		[SerializeField]
		protected float m_PrespawnDelay = 2f;

		//Message showed on leaderboard
		[SerializeField]
		protected string m_DiedMessage = "YOU DIED!";

		//Timer state values
		protected bool m_IsTiming = false;
		protected float m_CountDownTimer = 0f;
		
		//Timer based function call
		protected Action m_FinishedTimerEvent;
		
		//The tank to be respawned
		protected TankManager m_Tank;
		
		//Cached reference to the game manager
		protected GameManager m_GameManager;

		//The spawn point
		protected Transform m_SpawnPoint;

		/// <summary>
		/// Starts the respawn cycle
		/// </summary>
		/// <param name="tank">Tank</param>
		/// <param name="gameManager">Game manager</param>
		/// <param name="showLeaderboard">If set to <c>true</c> show leaderboard</param>
		/// <param name="spawnPoint">Spawn point</param>
		public void StartRespawnCycle(TankManager tank, GameManager gameManager, bool showLeaderboard, Transform spawnPoint)
		{
			this.m_Tank = tank;
			this.m_GameManager = gameManager;
			this.m_SpawnPoint = spawnPoint;
			tank.movement.SetAudioSourceActive(false);
			if (showLeaderboard)
			{
				SetTimer(m_ShowLeaderboardDelay, ShowLeaderboard);
			}
			else
			{
				tank.Prespawn();
				SetTimer(m_PrespawnDelay, SpawnAtRandomPoint);
			}
		}

		/// <summary>
		/// Shows the leaderboard and sets spawning to occur after delay
		/// </summary>
		protected void ShowLeaderboard()
		{
			m_GameManager.ShowLeaderboard(m_Tank, m_DiedMessage);
			SetTimer(m_SpawnDelay, SpawnAtRandomPoint);
		}

		/// <summary>
		/// Spawns at random point - this simply sets the position and sets reactivation to occur after delay
		/// </summary>
		protected void SpawnAtRandomPoint()
		{
			if (m_SpawnPoint == null)
			{
				Debug.LogWarning("Could not get random empty spawn point transform - tank will respawn at death position!!!");
                
			}
			else if (m_Tank != null)
			{
				m_Tank.RespawnReposition(m_SpawnPoint.position, m_SpawnPoint.rotation);
				SetTimer(m_SpawnReactivateDelay, ReactivateTank);
			}
		}

		/// <summary>
		/// Reactivates the tank by turning on visuals. This occurs after a delay to prevent the tank from appearing to drive really quickly to the new location
		/// </summary>
		protected void ReactivateTank()
		{
			m_Tank.RespawnReactivate();
			m_GameManager.ClearLeaderboard(m_Tank);
			Destroy(gameObject);
		}

		/// <summary>
		/// Unity message: Update
		/// </summary>
		protected void Update()
		{
			HandleTimer();
		}

		#region TIMER HANDLING

		/// <summary>
		/// Handles the timer based function calls that have been set up
		/// </summary>
		protected void HandleTimer()
		{
			if (m_IsTiming)
			{
				m_CountDownTimer -= Time.deltaTime;
				if (m_CountDownTimer <= 0)
				{
					m_IsTiming = false;
					if (m_FinishedTimerEvent != null && !m_GameManager.hasEveryoneBailed)
					{
						m_FinishedTimerEvent.Invoke();
					}
				}
			}
		}

		/// <summary>
		/// Sets the timer based function calls
		/// </summary>
		/// <param name="time">Time</param>
		/// <param name="elapsedEvent">Elapsed event</param>
		protected void SetTimer(float time, Action elapsedEvent)
		{
			if (m_GameManager.hasEveryoneBailed)
			{
				return;
			}

			m_CountDownTimer = time;
			m_FinishedTimerEvent = elapsedEvent;
			m_IsTiming = true;
		}

		#endregion
	}
}
