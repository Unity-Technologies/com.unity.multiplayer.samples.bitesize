using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using Tanks.Shells;
using Tanks.CameraControl;
using Tanks.Data;
using Tanks.Effects;
using Tanks.TankControllers;

namespace Tanks.Explosions
{
	[RequireComponent(typeof(NetworkObject))]
	public class ExplosionManager : NetworkBehaviour
	{
		/// <summary>
		/// Screen shake duration for explosions
		/// </summary>
		[SerializeField]
		protected float m_ExplosionScreenShakeDuration = 0.3f;

		/// <summary>
		/// Mask for sphere test
		/// </summary>
		private int m_PhysicsMask;


		/// <summary> 
		/// Reimplemented singleton. Can't use generics on NetworkBehaviours
		/// </summary>
		public static ExplosionManager s_Instance
		{
			get;
			protected set;
		}

		/// <summary>
		/// Gets whether an instance of this singleton exists
		/// </summary>
		public static bool s_InstanceExists { get { return s_Instance != null; } }

		/// <summary>
		/// A permanent reference to the Effects Library from which we'll get our explosion prefabs to spawn.
		/// </summary>
		private EffectsGroup m_EffectsGroup;

		/// <summary>
		/// Get physics mask
		/// </summary>
		protected virtual void Awake()
		{
			if (s_Instance != null)
			{
				Destroy(gameObject);
			}
			else
			{
				s_Instance = this;
			}

			m_PhysicsMask = LayerMask.GetMask("Players", "Projectiles", "Powerups", "DestructibleHazards", "Decorations");
		}

		protected virtual void Start()
		{
			m_EffectsGroup = ThemedEffectsLibrary.s_Instance.GetEffectsGroupForMap();
		}


		/// <summary>
		/// Clear instance
		/// </summary>
		protected virtual void OnDestroy()
		{
			if (s_Instance == this)
			{
				s_Instance = null;
			}
		}

		/// <summary>
		/// Create clusters where appropriate
		/// </summary>
		public static void SpawnDebris(Vector3 spawnPos, Vector3 normalVector, int damageOwnerId, Collider ignoreCollider, DebrisSettings settings,
		                               int randSeed)
		{
			if (settings != null && settings.prefab)
			{
				Random.State prevState = Random.state;
				Random.InitState(randSeed);

				Vector3 right = Vector3.Dot(normalVector, Vector3.up) < 0.8 ? Vector3.Cross(normalVector, Vector3.up) : Vector3.right;

				int numSpawns = Random.Range(settings.minSpawns, settings.maxSpawns);
				float distributionAngle = (360f / (numSpawns - 1));

				for (int i = 0; i < numSpawns; i++)
				{
					Vector3 lookDir = normalVector;

					lookDir = Quaternion.AngleAxis(Random.Range(0f, settings.maxUpAngle), right) * lookDir;
					lookDir = Quaternion.AngleAxis(i * distributionAngle, normalVector) * lookDir;

					// Create an instance of the shell and store a reference to its rigidbody.
					Rigidbody shellInstance =
						Instantiate(settings.prefab, spawnPos, Quaternion.identity) as Rigidbody;

					if (shellInstance != null)
					{
						shellInstance.transform.forward = lookDir;
						Vector3 shellVel = lookDir * Random.Range(settings.minForce, settings.maxForce);
						shellInstance.velocity = shellVel;
						shellInstance.MovePosition(spawnPos + shellVel * Time.fixedDeltaTime);

						Shell shellObject = shellInstance.GetComponent<Shell>();
						if (shellObject != null)
						{
							shellObject.Setup(damageOwnerId, ignoreCollider, randSeed);
						}
					}
				}

				Random.state = prevState;
			}
		}

		/// <summary>
		/// Create an explosion at a given location
		/// </summary>
		public void SpawnExplosion(Vector3 explosionPosition, Vector3 explosionNormal, GameObject ignoreObject, int damageOwnerId, ExplosionSettings explosionConfig, bool clientOnly)
		{
			if (clientOnly)
			{
				CreateVisualExplosion(explosionPosition, explosionNormal, explosionConfig.explosionClass);
			}
			else if (IsServer)
			{
				RpcVisualExplosionClientRpc(explosionPosition, explosionNormal, explosionConfig.explosionClass);
			}

			DoLogicalExplosion(explosionPosition, explosionNormal, ignoreObject, damageOwnerId, explosionConfig);
		}

		/// <summary>
		/// Perform logical explosion
		/// On server, this deals damage to stuff. On clients, it just applies forces
		/// </summary>
		private void DoLogicalExplosion(Vector3 explosionPosition, Vector3 explosionNormal, GameObject ignoreObject, int damageOwnerId, ExplosionSettings explosionConfig)
		{
			// Collect all the colliders in a sphere from the explosion's current position to a radius of the explosion radius.
			Collider[] colliders = Physics.OverlapSphere(explosionPosition, Mathf.Max(explosionConfig.explosionRadius, explosionConfig.physicsRadius), m_PhysicsMask);

			// Go through all the colliders...
			for (int i = 0; i < colliders.Length; i++)
			{
				Collider struckCollider = colliders[i];

				// Skip ignored object
				if (struckCollider.gameObject == ignoreObject)
				{
					continue;
				}

				// Create a vector from the shell to the target.
				Vector3 explosionToTarget = struckCollider.transform.position - explosionPosition;

				// Calculate the distance from the shell to the target.
				float explosionDistance = explosionToTarget.magnitude;

				// Server deals damage to objects
				if (IsServer)
				{
					// Find the DamageObject script associated with the rigidbody.
					IDamageObject targetHealth = struckCollider.GetComponentInParent<IDamageObject>();

					// If there is one, deal it damage
					if (targetHealth != null &&
					    targetHealth.isAlive &&
					    explosionDistance < explosionConfig.explosionRadius)
					{
						// Calculate the proportion of the maximum distance (the explosionRadius) the target is away.
						float normalizedDistance = Mathf.Clamp01((explosionConfig.explosionRadius - explosionDistance) / explosionConfig.explosionRadius);

						// Calculate damage as this proportion of the maximum possible damage.
						float damage = normalizedDistance * explosionConfig.damage;

						// Deal this damage to the tank.
						targetHealth.SetDamagedBy(damageOwnerId, explosionConfig.id);
						targetHealth.Damage(damage);
					}
				}

				// Apply force onto PhysicsAffected objects, for anything we have authority on, or anything that's client only
				PhysicsAffected physicsObject = struckCollider.GetComponentInParent<PhysicsAffected>();
				NetworkObject identity = struckCollider.GetComponentInParent<NetworkObject>();

				if (physicsObject != null && physicsObject.enabled && explosionDistance < explosionConfig.physicsRadius &&
				    (identity == null || identity.IsOwner))
				{
					physicsObject.ApplyForce(explosionConfig.physicsForce, explosionPosition, explosionConfig.physicsRadius);
				}
			}

			DoShakeForExplosion(explosionPosition, explosionConfig);
		}


		private void CreateVisualExplosion(Vector3 explosionPosition, Vector3 explosionNormal, ExplosionClass explosionClass)
		{
			Effect spawnedEffect = null;

			switch (explosionClass)
			{
				case ExplosionClass.ExtraLarge:
					spawnedEffect = Instantiate<Effect>(m_EffectsGroup.extraLargeExplosion);
					break;
				case ExplosionClass.Large:
					spawnedEffect = Instantiate<Effect>(m_EffectsGroup.largeExplosion);
					break;
				case ExplosionClass.Small:
					spawnedEffect = Instantiate<Effect>(m_EffectsGroup.smallExplosion);
					break;
				case ExplosionClass.TankExplosion:
					spawnedEffect = Instantiate<Effect>(m_EffectsGroup.tankExplosion);
					break;
				case ExplosionClass.TurretExplosion:
					spawnedEffect = Instantiate<Effect>(m_EffectsGroup.turretExplosion);
					break;
				case ExplosionClass.BounceExplosion:
					spawnedEffect = Instantiate<Effect>(m_EffectsGroup.smallExplosion);
					break;
				case ExplosionClass.FiringExplosion:
					spawnedEffect = Instantiate<Effect>(m_EffectsGroup.firingExplosion);
					break;
			}

			if (spawnedEffect != null)
			{
				spawnedEffect.transform.position = explosionPosition;
				spawnedEffect.transform.up = explosionNormal;

				AudioSource sound = spawnedEffect.GetComponentInChildren<AudioSource>();
				if (sound != null)
				{
					sound.clip = ThemedEffectsLibrary.s_Instance.GetRandomExplosionSound(explosionClass);
					sound.Play();
				}
			}
		}


		/// <summary>
		/// Make a pretty explosion on clients
		/// </summary>
		[ClientRpc]
		private void RpcVisualExplosionClientRpc(Vector3 explosionPosition, Vector3 explosionNormal, ExplosionClass explosionClass)
		{
			CreateVisualExplosion(explosionPosition, explosionNormal, explosionClass);
		}


		private void DoShakeForExplosion(Vector3 explosionPosition, ExplosionSettings explosionConfig)
		{
			// Do screen shake on main camera
			if (ScreenShakeController.s_InstanceExists)
			{
				ScreenShakeController shaker = ScreenShakeController.s_Instance;

				float shakeMagnitude = explosionConfig.shakeMagnitude;
				shaker.DoShake(explosionPosition, shakeMagnitude, m_ExplosionScreenShakeDuration, 0.0f, 1.0f);
			}
		}
	}
}