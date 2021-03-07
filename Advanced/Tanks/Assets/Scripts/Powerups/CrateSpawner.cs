using UnityEngine;
using System.Collections;
using MLAPI;
using Tanks.FX;
using Tanks.Explosions;
using System;
using Random = UnityEngine.Random;
using Tanks.Extensions;

namespace Tanks.Pickups
{
	//This struct is used to define spawner-relevant parameters for powerups in the CrateSpawner.
	[Serializable]
	public struct PowerupDefinition
	{
		//The name of the powerup. Not used for anything other than convenient reference.
		public string name;

		//The prefab for this powerup.
		public GameObject powerupPrefab;

		//The relative probability of this object spawning.
		public int dropWeighting;
	}

	//This server-only class is responsible for spawning powerup pods at set intervals on multiplayer maps.
	public class CrateSpawner : NetworkBehaviour
	{
		//Reference to the ScriptableObject defining the explosion when powerups spawn.
		[SerializeField]
		protected ExplosionSettings m_SpawnExplosion;

		//The radius around the centre of the map in which powerups will be spawned.
		[SerializeField]
		protected float m_DropRadius = 40f;

		//The interval between powerup drops.
		[SerializeField]
		protected float m_DropInterval = 30f;

		//The radius of the spherecast to determine whether a candidate drop area is clear or not.
		[SerializeField]
		protected float m_SpherecastRadius = 3f;

		//The prefab to spawn to indicate an incoming drop, and a temporary reference variable so we can early-out it if necessary.
		[SerializeField]
		protected GameObject m_HotdropEffectPrefab;

		//A list of powerup definition objects that will be spawned.
		[SerializeField]
		protected PowerupDefinition[] m_PowerupsToSpawn;

		//Flag to set whether this spawner is active or not.
		[HideInInspector]
		private bool m_IsSpawnerActive = false;

		//The next time when a drop will occur.
		private float m_NextDropTime;

		//The next time when a drop pod will spawn. This is used to offset the time of actual instantiation of the drop pod from the time of the incoming drop effect.
		private float m_NextSpawnTime = 0f;

		//The point of the next drop.
		private Vector3 m_DropTargetPosition;

		private GameObject m_ActiveDropEffect;

		//The total of all drop weightings for random selection purposes.
		private int m_TotalWeighting;

		//Internal variables to cache the ground and tank layers for collision scanning purposes.
		private int m_GroundLayer;
		private int m_TankLayer;

		private void Start()
		{
			if (!IsServer)
				return;

			//Add a reference to the GameManager to allow it to enable and disable this spawner as necessary.
			GameManager.s_Instance.AddCrateSpawner(this);

			m_GroundLayer = LayerMask.NameToLayer("Ground");
			m_TankLayer = LayerMask.NameToLayer("Players");

			//Aggregate all the drop weightings of the items in our powerup list for selection.
			for (int i = 0; i < m_PowerupsToSpawn.Length; i++)
			{
				m_TotalWeighting += m_PowerupsToSpawn[i].dropWeighting;
			}
		}

		private void Update()
		{
			if (!IsServer)
				return;

			if (m_IsSpawnerActive)
			{
				//This tracks the next time to spawn the drop effect.
				if (Time.time >= m_NextDropTime)
				{
					BeginDrop();
					m_NextDropTime = Time.time + m_DropInterval;
				}

				//The tracks the next time to spawn a powerup.
				if ((m_NextSpawnTime > 0f) && (Time.time >= m_NextSpawnTime))
				{
					SpawnPowerup();
					m_NextSpawnTime = 0f;
				}
			}
		}

		//This method scans the battlefield for empty ground, and having found a candidate target zone, instantiates the drop pod effect and queues the spawning of the powerup object.
		private void BeginDrop()
		{
			bool hasTarget = false;

			RaycastHit hitdata;

			while (!hasTarget)
			{
				Vector3 randomRadius = Vector3.right * Random.Range(0f, m_DropRadius);
				Quaternion randomRotation = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up);

				m_DropTargetPosition = randomRotation * randomRadius;

				Physics.SphereCast(m_DropTargetPosition + (Vector3.up * 500f), m_SpherecastRadius, Vector3.down, out hitdata, 600f);

				if ((hitdata.collider.gameObject.layer == m_GroundLayer) || (hitdata.collider.gameObject.layer == m_TankLayer))
				{
					hasTarget = true;
				}
			}

			//Instantiate the drop effect.
			GameObject dropEffect = (GameObject)Instantiate(m_HotdropEffectPrefab, m_DropTargetPosition, Quaternion.identity);
			m_ActiveDropEffect = dropEffect;
			//Set the powerup spawn timer for when the effect is done.
			m_NextSpawnTime = Time.time + dropEffect.GetComponent<HotdropLight>().dropTime;
		}

		//Gets a random powerup and spawns it to the field along with an explosion to correlate with its "drop" from orbit.
		private void SpawnPowerup()
		{
			m_ActiveDropEffect = null;

			GameObject cratePrefab = GetRandomPowerup();

			//Crates will auto-network-spawn on start, so we only need to instantiate them.
			GameObject dropPod = (GameObject)Instantiate(cratePrefab, m_DropTargetPosition, Quaternion.identity);


			if (m_SpawnExplosion != null && ExplosionManager.s_InstanceExists)
			{
				ExplosionManager.s_Instance.SpawnExplosion(dropPod.transform.position, transform.up, dropPod, -1, m_SpawnExplosion, false);
			}
		}

		//Activates the spawner and sets its next drop time. Normally called from the GameManager at the beginning of a new round.
		public void ActivateSpawner()
		{
			m_IsSpawnerActive = true;
			m_NextDropTime = Time.time + m_DropInterval;
		}

		//Deactivates the spawner, and stops any drop sequence that is still active in its tracks.
		public void DeactivateSpawner()
		{
			m_IsSpawnerActive = false;

			if (m_ActiveDropEffect != null)
			{
				Destroy(m_ActiveDropEffect);
			}

			m_NextSpawnTime = 0f;
		}

		private GameObject GetRandomPowerup()
		{
			return m_PowerupsToSpawn.WeightedSelection(m_TotalWeighting, t => t.dropWeighting).powerupPrefab;
		}
	}
}
