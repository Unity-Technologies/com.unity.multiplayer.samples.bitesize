using UnityEngine;
using MLAPI;
using Tanks.Explosions;
using Tanks.TankControllers;
using MLAPI.Messaging;

namespace Tanks.Hazards
{
	//This class controls the mine hazard.
	[RequireComponent(typeof(NetworkObject))]
	public class MineController : LevelHazard, IDamageObject
	{
		//Reference to the Scriptable Object defining mine explosion parameters
		[SerializeField]
		protected ExplosionSettings m_MineExplosionSettings;

		//The minimum damage that the mine must suffer in one hit to be destroyed.
		[SerializeField]
		protected float m_DamageThreshold = 20f;

		//Reference to the mine mesh.
		[SerializeField]
		protected GameObject m_MineMesh;

		//Trigger timer countdown variables, including the maximum countdown time.
		[SerializeField]
		protected float m_TriggerCountdownDuration = 3f;
		private float m_TriggerTime = 0f;
		private bool m_Triggered = false;

		//Reference to the light on top of the mine.
		[SerializeField]
		protected Renderer m_MineLight;

		//References to the two light materials that will be swapped between to denote the mine's state.
		[SerializeField]
		protected Material m_IdleLightMaterial;
		[SerializeField]
		protected Material m_TriggeredLightMaterial;

		//Reference to the collider acting as the proximity trigger for the mine.
		[SerializeField]
		protected Collider m_TriggerCollider;

		//We assume that mines are triggered and detonated by the ineptitude of the players themselves, so it counts as suicide for score purposes.
		//However, players can also detonate mines by shooting at them.
		//We track last hit player and detonating player seperately, so that proxy-triggering a mine doesn't count as a kill for the last player who shot at but didn't detonate it.
		private int m_LastHitBy = TankHealth.TANK_SUICIDE_INDEX;
		private int m_DetonatedByPlayer = TankHealth.TANK_SUICIDE_INDEX;

		//Is the mine alive and active? Also needed to implement IDamageObject.
		public bool isAlive { get; protected set; }

		protected override void Start()
		{
			if (!IsServer)
				return;

			base.Start();
			isAlive = true;
			m_TriggerCollider = GetComponent<Collider>();
		}

		protected void Update()
		{
			if (IsServer)
			{
				//On the server, check current time against the detonation time and explode the mine when required.
				if (m_TriggerTime > 0f)
				{
					if (m_TriggerTime <= Time.time)
					{
						ExplodeMine();
					}
				}
			}
		}

		//On the server, set the mine as triggered if anything in the "Players" layer enters the collider.
		private void OnTriggerEnter(Collider other)
		{
			if (!IsServer)
				return;

			//If the mine's already been triggered, ignore any new entrants
			if (m_Triggered)
			{
				return;
			}

			if (other.gameObject.layer == LayerMask.NameToLayer("Players"))
			{
				m_TriggerTime = Time.time + m_TriggerCountdownDuration;
				m_Triggered = true;
				RpcSetTriggeredEffectsClientRpc();
			}
		}

		//Perform server-side explosion logic. We don't actually destroy mine objects, we just disable them so that we can turn them back on between rounds if needed.
		private void ExplodeMine()
		{
			if (!IsServer)
				return;

			Debug.Log("<color=orange>Your mine asplode. Detonated by player " + m_DetonatedByPlayer + "</color>");
			m_TriggerTime = 0f;
			isAlive = false;

			m_TriggerCollider.enabled = false;

			//Spawn the explosion through the ExplosionManager. The explosion itself will be broadcast to clients by the ExplosionManager.
			ExplosionManager.s_Instance.SpawnExplosion(transform.position, Vector3.up, gameObject, m_DetonatedByPlayer, m_MineExplosionSettings, false);

			RpcExplodeMineClientRpc();
		}

		//Perform server-side reset logic. This is normally done between rounds of Last Man Standing games.
		public override void ResetHazard()
		{
			if (!IsServer)
				return;

			m_Triggered = true;
			m_TriggerTime = 0f;
			m_TriggerCollider.enabled = false;

			m_LastHitBy = TankHealth.TANK_SUICIDE_INDEX;
			m_DetonatedByPlayer = TankHealth.TANK_SUICIDE_INDEX;

			RpcResetMineClientRpc();
		}

		//Perform server-side reactivation logic.
		public override void ActivateHazard()
		{
			if (!IsServer)
				return;

			m_TriggerCollider.enabled = true;
			isAlive = true;
			m_Triggered = false;
		}

		//Fired on all clients to start the mine's trigger effects (light to red and warning beep sound effect).
		[ClientRpc]
		private void RpcSetTriggeredEffectsClientRpc()
		{
			m_MineLight.material = m_TriggeredLightMaterial;

			GetComponent<AudioSource>().Play();
		}

		//Fired on all clients to hide the visible mine and stop any trigger sound effects.
		[ClientRpc]
		private void RpcExplodeMineClientRpc()
		{
			m_MineMesh.SetActive(false);
			GetComponent<AudioSource>().Stop();
		}

		//Reset mines client-side. Makes them visible again.
		[ClientRpc]
		private void RpcResetMineClientRpc()
		{
			GetComponent<AudioSource>().Stop();
			m_MineLight.material = m_IdleLightMaterial;
			m_MineMesh.SetActive(true);
		}

		#region IDamageObject Implementation

		public Vector3 GetPosition()
		{
			return transform.position;
		}

		//This is called when a mine receives damage from a player's weapon.
		public void Damage(float damage)
		{
			//We only player-detonate this mine if damage exceeds a certain threshold
			if (isAlive && (damage >= m_DamageThreshold))
			{
				//Since we know who detonated this mine, we can formally assign any kills due to the resulting explosion to them.
				m_DetonatedByPlayer = m_LastHitBy;

				ExplodeMine();
			}
		}

		public void SetDamagedBy(int playerNumber, string explosionId)
		{
			if (isAlive)
			{
				m_LastHitBy = playerNumber;
			}
		}

		#endregion
	}
}
