using UnityEngine;
using Tanks.SinglePlayer;
using Tanks.Data;
using Tanks.Networking;
using Tanks.TankControllers;
using Tanks.Audio;
using TanksNetworkPlayer = Tanks.Networking.NetworkPlayer;

namespace Tanks.Rules.SinglePlayer
{
	/// <summary>
	/// Shooting range rules processor - this handles the game mode for the decoration mini game
	/// </summary>
	public class ShootingRangeRulesProcessor : OfflineRulesProcessor
	{
		[SerializeField]
		protected float m_SpeedChangePerSecond = 0.01f;
		[SerializeField]
		protected float m_SpeedChangeTime = 1f;
		[SerializeField]
		protected float m_MaxSpeed = 3f;

		protected TankDecorationDefinition m_EarnedDecoration;
		protected int m_DecorationMaterialId;

		public bool hasReset
		{
			get;
			protected set;
		}

		public TankDecorationDefinition prize
		{
			get
			{
				return m_EarnedDecoration;
			}
		}

		public int prizeColourId
		{
			get
			{
				return m_DecorationMaterialId;
			}
		}

		private float m_CurrentSpeed = 1;
		private float m_TargetSpeed = 1;

		private float m_SpeedVel;

		private float m_InitialCrateSpeed;

		#region Methods
		protected virtual void Update()
		{
			// Increase speed over time
			if (m_GameManager.state == GameState.Playing)
			{
				m_TargetSpeed = Mathf.Min(m_TargetSpeed + (m_SpeedChangePerSecond * Time.deltaTime), m_MaxSpeed);
				m_CurrentSpeed = Mathf.SmoothDamp(m_CurrentSpeed, m_TargetSpeed, ref m_SpeedVel, m_SpeedChangeTime);

				if (CrateManager.s_InstanceExists)
				{
					CrateManager.s_Instance.crateSpeed = m_InitialCrateSpeed * m_CurrentSpeed;
				}

				if (MusicManager.s_InstanceExists)
				{
					// Pitch change is half crate speed increase
					MusicManager.s_Instance.musicSource.pitch = ((m_CurrentSpeed - 1) * 0.5f) + 1;
				}
			}
		}

		// Win the game, and remember the first crate we destroy
		public override void DestroyNpc(Npc npc)
		{
			Crate crate = npc as Crate;
			if (!m_MatchOver && crate != null)
			{
				m_EarnedDecoration = crate.cratePrize;
				m_DecorationMaterialId = crate.decorationMaterialIndex;

				m_MatchOver = true;
				LazyLoadPlayerTank();
				m_Winner = m_PlayerTank;
				m_MissionFailed = false;
			}
		}


		/// <summary>
		/// Make player invulnerable
		/// </summary>
		public override void StartRound()
		{
			if (TanksNetworkPlayer.s_LocalPlayer != null &&
			    TanksNetworkPlayer.s_LocalPlayer.tank != null)
			{
				TankManager localTank = TanksNetworkPlayer.s_LocalPlayer.tank;
				localTank.health.invulnerable = true;
			}

			if (CrateManager.s_InstanceExists)
			{
				m_InitialCrateSpeed = CrateManager.s_Instance.crateSpeed;
			}
		}


		// Do nothing here
		public override void EntersZone(GameObject zoneObject, TargetZone zone)
		{
			
		}

		/// <summary>
		/// Resets the game
		/// </summary>
		public override void ResetGame()
		{
			NetworkManager.s_Instance.ProgressToGameScene();
		}

		public override void Bail()
		{
			NetworkManager.s_Instance.Disconnect();
			base.Bail();
		}

		private void OnAdvertFailed()
		{
			Debug.Log("Advert failure");
		}

		public override void CompleteGame()
		{
			// Award earned decoration.
			Debug.Log("unlocking decoration id: " + m_EarnedDecoration.id);
			int decorationIndex = TankDecorationLibrary.s_Instance.GetIndexForDecoration(m_EarnedDecoration);
			PlayerDataManager.s_Instance.SetDecorationUnlocked(decorationIndex);
			PlayerDataManager.s_Instance.SetDecorationColourUnlocked(decorationIndex, m_DecorationMaterialId);

			NetworkManager.s_Instance.Disconnect();
			base.CompleteGame();
		}
		#endregion
	}
}