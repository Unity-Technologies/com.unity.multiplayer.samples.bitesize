using UnityEngine;
using System.Collections;
using Tanks.TankControllers;

namespace Tanks.Rules.SinglePlayer.Objectives
{
	/// <summary>
	/// Don't take damage objective. Player fails this objective if their health drops below a certain level
	/// </summary>
	public class DontTakeDamage : Objective
	{
		[SerializeField]
		[Range(0, 1)]
		protected float m_HealthRatio = 1f;

		protected bool m_HasSetupCallback = false;
		protected TankHealth m_PlayerHealth;

		/// <summary>
		/// Initialise as successful
		/// </summary>
		protected void Awake()
		{
			LazySetup();
			TrySetNewlyUnlocked();
		}

		/// <summary>
		/// Unsubscribes from health change event
		/// </summary>
		protected void OnDestroy()
		{
			if (m_PlayerHealth != null)
			{
				m_PlayerHealth.healthChanged -= PlayerHealthChanged;
			}
		}

		/// <summary>
		/// Lazy sets up health change subscription
		/// </summary>
		protected void Update()
		{
			LazySetup();
		}

		/// <summary>
		/// Lazily sets up the health change event
		/// </summary>
		protected virtual void LazySetup()
		{
			if (m_HasSetupCallback)
			{
				return;
			}

			if (this.m_RulesProcessor != null)
			{
				TankManager playerTank = this.m_RulesProcessor.playerTank;
				if (playerTank != null)
				{
					m_PlayerHealth = playerTank.health;
					if (m_PlayerHealth != null)
					{
						m_HasSetupCallback = true;
						m_PlayerHealth.healthChanged += PlayerHealthChanged;
					}
				}
			}
		}

		/// <summary>
		/// Health change event
		/// </summary>
		/// <param name="newHealthRatio">New health ratio.</param>
		protected virtual void PlayerHealthChanged(float newHealthRatio)
		{
			if (newHealthRatio < m_HealthRatio)
			{
				Failed();
			}
		}

		public override string objectiveDescription
		{
			get
			{
				if (m_HealthRatio == 1f)
				{
					return "Do not take damage!";
				} 

				return string.Format("Keep health above {0}%", Mathf.RoundToInt(m_HealthRatio * 100)); 
			}
		}

		public override string objectiveSummary
		{
			get { return "Stay healthy"; }
		}
	}
}