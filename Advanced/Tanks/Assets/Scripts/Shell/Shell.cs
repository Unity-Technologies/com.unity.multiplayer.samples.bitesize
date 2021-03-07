using UnityEngine;
using Tanks.Explosions;

namespace Tanks.Shells
{
	[RequireComponent(typeof(Rigidbody))]
	//This class defines the behaviour of all shells in the game.
	public class Shell : MonoBehaviour
	{
		//References to ExplosionSettings ScriptableObject that defines the explosion parameters for this shell.
		[SerializeField]
		protected ExplosionSettings m_ExplosionSettings;

		//Explosion parameters for the explosion spawned if the shell bounces.
		[SerializeField]
		protected ExplosionSettings m_BounceExplosionSettings;

		//Variables to allow objects to be spawned on explosion, either for elaborate SFX or cluster munitions.
		[SerializeField]
		protected DebrisSettings m_SpawnedDebris;

		//Variables for controlling shell bounciness - how many times to bounce, force decay per bounce, and bounce direction
		[SerializeField]
		protected int m_Bounces = 0;
		[SerializeField]
		protected float m_BounceForceDecay = 1.05f;
		[SerializeField]
		protected Vector3 m_BounceAdditionalForce = Vector3.up;

		//Minimum height that a shell can be in world coordinates before self-destructing.
		[SerializeField]
		protected float m_MinY = -1;

		//Modifier for shell velocity
		[SerializeField]
		protected float m_SpeedModifier = 1;

		//Modifier for the shell's spin rate.
		[SerializeField]
		protected float m_Spin = 720;

		//Number of physics ticks that the shell ignores the tempignore collider.
		[SerializeField]
		protected int m_IgnoreColliderFixedFrames = 2;

		//internal reference to ignore collider and ignore ticks.
		private Collider m_TempIgnoreCollider;
		private int m_TempIgnoreColliderTime = 2;

		//Has this shell exploded?
		private bool m_Exploded;

		//The unique index of the player who fired this projectile for score purposes
		private int m_OwningPlayerId = -1;

		//Random seed for spawning debris.
		private int m_RandSeed;

		//The current rotation of the shell.
		private float m_CurrentSpinRot;

		//Internal reference to this shell's rigidbody
		private Rigidbody m_CachedRigidBody;

		//Internal list of trail renderers attached to this shell
		private TrailRenderer[] m_ShellTrails;

		//Accessor for speed modifier
		public float speedModifier
		{
			get { return m_SpeedModifier; }
		}


		public int owningPlayerId
		{
			get { return m_OwningPlayerId; }
		}

		private void Awake()
		{
			m_CachedRigidBody = GetComponent<Rigidbody>();
			m_Exploded = false;

			//Scan all children to find all trailrenderer objects attached to this shell.
			m_ShellTrails = GetComponentsInChildren<TrailRenderer>();
		}

		public void Setup(int owningPlayerId, Collider ignoreCollider, int seed)
		{
			this.m_OwningPlayerId = owningPlayerId;

			if (ignoreCollider != null)
			{
				// Ignore collisions temporarily
				Physics.IgnoreCollision(GetComponent<Collider>(), ignoreCollider, true);
				m_TempIgnoreCollider = ignoreCollider;
				m_TempIgnoreColliderTime = m_IgnoreColliderFixedFrames;
			}

			m_RandSeed = seed;

			// If we have a speed modifier, add an extra constant gravitational force to us
			if (m_SpeedModifier != 1)
			{
				ConstantForce force = gameObject.AddComponent<ConstantForce>();
				force.force = Physics.gravity * m_CachedRigidBody.mass * (m_SpeedModifier - 1);
			}

			transform.forward = m_CachedRigidBody.velocity;
		}

		private void FixedUpdate()
		{
			//If we have an ignore collider, deplete the count and cancel our collision ignorance when it's zero.
			if (m_TempIgnoreCollider != null)
			{
				m_TempIgnoreColliderTime--;
				if (m_TempIgnoreColliderTime <= 0)
				{
					Physics.IgnoreCollision(GetComponent<Collider>(), m_TempIgnoreCollider, false);
					m_TempIgnoreCollider = null;
				}
			}
		}
			
		// Face towards our movement direction
		private void Update()
		{
			transform.forward = m_CachedRigidBody.velocity;

			// Spin the projectile
			m_CurrentSpinRot += m_Spin * Time.deltaTime * m_CachedRigidBody.velocity.magnitude;
			transform.Rotate(Vector3.forward, m_CurrentSpinRot, Space.Self);

			// Enforce minimum y, in case we go out of bounds
			if (transform.position.y <= m_MinY)
			{
				Destroy(gameObject);
			}
			else
			{
				// Reset this. We can set it to true during bounces to stop multiple colliders hitting it per frame
				m_Exploded = false;
			}
		}
			
		// Create explosions on collision
		private void OnCollisionEnter(Collision c)
		{
			if (m_Exploded)
			{
				return;
			}

			//Determine the collision's normal, position, and which explosion definition to use based on how many bounces we have left.
			Vector3 explosionNormal = c.contacts.Length > 0 ? c.contacts[0].normal : Vector3.up;
			Vector3 explosionPosition = c.contacts.Length > 0 ? c.contacts[0].point : transform.position;
			ExplosionSettings settings = m_Bounces > 0 ? m_BounceExplosionSettings : m_ExplosionSettings;

			if (ExplosionManager.s_InstanceExists)
			{
				ExplosionManager em = ExplosionManager.s_Instance;
				if (settings != null)
				{
					em.SpawnExplosion(transform.position, explosionNormal, gameObject, m_OwningPlayerId, settings, false);
				}
				ExplosionManager.SpawnDebris(explosionPosition, explosionNormal, m_OwningPlayerId, c.collider, m_SpawnedDebris, m_RandSeed);
			}

			//If we're bouncing, reflect our movement direction, decay our force, reduce our number of bounces.
			if (m_Bounces > 0)
			{
				m_Bounces--;

				Vector3 refl = Vector3.Reflect(-c.relativeVelocity, explosionNormal);
				refl *= m_BounceForceDecay;
				refl += m_BounceAdditionalForce;
				// Push us back up
				m_CachedRigidBody.velocity = refl;

				// Temporarily ignore collisions with this object
				if (m_TempIgnoreCollider != null)
				{
					Physics.IgnoreCollision(GetComponent<Collider>(), m_TempIgnoreCollider, false);
				}
				m_TempIgnoreCollider = c.collider;
				m_TempIgnoreColliderTime = m_IgnoreColliderFixedFrames;
			}
			else
			{
				Destroy(gameObject);
			}

			m_Exploded = true;
		}

		private void OnDestroy()
		{
			//On destruction, decouple any trail objects that still exist from this object. 
			//This allows them to fizzle out naturally and self-destruct instead of popping out of existence when at full length.
			for(int i = 0; i < m_ShellTrails.Length; i++)
			{
				TrailRenderer trail = m_ShellTrails[i];
				if (trail != null)
				{
					trail.transform.SetParent(null);
					trail.autodestruct = true;
				}
			}
		}
	}
}