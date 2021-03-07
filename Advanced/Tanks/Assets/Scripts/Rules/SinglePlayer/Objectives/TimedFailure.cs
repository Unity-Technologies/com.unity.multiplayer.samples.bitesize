using UnityEngine;
using System.Collections;
using Tanks.SinglePlayer;

namespace Tanks.Rules.SinglePlayer.Objectives
{
	/// <summary>
	/// Timed objective - player fails if they take longer than specified time.
	/// </summary>
	public class TimedFailure : Objective
	{
		/// <summary>
		/// The timer interval, and time left. This counts down
		/// </summary>
		[SerializeField]
		protected float m_Timer = 30.0f;

		//To distinguish the gameplay mode that the timer is used for.
		[SerializeField]
		protected string m_ObjectiveDescriptionText;
		[SerializeField]
		protected string m_ObjectiveSummaryText;

		/// <summary>
		/// The interval, stays the same and is used in the UI
		/// </summary>
		private float m_MaxTime;

		/// <summary>
		/// If the timer should count down. This is to prevent it from counting down after its interval being exceeded
		/// </summary>
		private bool m_MustCountDown = true;

		/// <summary>
		/// Initialised the task to be succeeded
		/// </summary>
		protected virtual void Awake()
		{
			m_MaxTime = m_Timer;
			TrySetNewlyUnlocked();
		}

		/// <summary>
		/// Update: Run the count down
		/// </summary>
		protected virtual void Update()
		{
			CountDown();
		}

		/// <summary>
		/// Handles counting down and marking the state as failed
		/// </summary>
		protected void CountDown()
		{
			if (m_MustCountDown)
			{
				m_Timer -= Time.deltaTime;

				if (m_Timer <= 0.0f)
				{
					Failed();
					m_MustCountDown = false;
				}
			}
		}

		public override string objectiveDescription
		{
			get
			{
				if (m_ObjectiveDescriptionText == string.Empty)
				{
					return string.Format("Finish primary objective in {0} seconds", m_MaxTime);
				}
				else
				{
					return string.Format(m_ObjectiveDescriptionText + " {0} seconds", m_MaxTime);
				}
			}
		}

		public override string objectiveSummary
		{
			get
			{
				if (m_ObjectiveSummaryText == string.Empty)
				{	
					return string.Format("Finish: {0}s", m_MaxTime);
				}
				else
				{
					return string.Format(m_ObjectiveSummaryText + " {0}s", m_MaxTime);
				}
			}
		}
	}
}
