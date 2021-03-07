//Script that controls the aiming-action of objects that automatically shoot at a target. By default, this target is the player.

using UnityEngine;
using Tanks.Shells;
using Tanks.TankControllers;
using System.Collections.Generic;

namespace Tanks.Hazards
{
	/// <summary>
	/// Autonomous turret - aim and shoot
	/// </summary>
	public class Turret : MonoBehaviour
	{
		// Audio that plays when each shot is fired.
		[SerializeField]
		protected AudioClip m_FiringAudio;

		// Reference to the audio source used to play the shooting audio.
		[SerializeField]
		protected AudioSource m_FiringAudioSource;

		// A child of the turret where the shells are spawned.
		[SerializeField]
		protected Transform m_ShellSource;

		[SerializeField]
		protected Shell m_Shell;

		// The force that will be given to the shell when the fire button is released.
		protected float m_CurrentLaunchAngle = 45f;

		// The high angle for shots
		protected float m_MaxLaunchAngle = 45f;

		[SerializeField]
		private float m_TurretFireTimer;

		[SerializeField]
		protected bool m_FollowPlayer = true;

		[SerializeField]
		protected List<Transform> m_FollowTransforms = new List<Transform>();

		[SerializeField]
		protected Transform m_TurretTransform;

		[SerializeField]
		protected float m_TurretRotationMultiplier = 3f;

		[SerializeField]
		protected float m_FireDotLimit = 0.9f;

		[SerializeField]
		protected float m_FireSafetyRange = 5f;

		[SerializeField]
		protected float m_MinFiringDistance;
		[SerializeField]
		protected float m_MaxFiringDistance;

		[SerializeField]
		private bool m_DisableFire;

		[SerializeField]
		protected GameObject m_MuzzleFlashPrefab;

		private TankMovement m_TankToFollowMovement = null;

		private float m_CurrentFireTimerCount;
		private float m_SqrSafetyRange;
			
		private float m_TargetDot;
		private Vector3 m_ActiveTargetPosition;

		private int m_PlayerTankIndex = 0, m_CurrentFollowTransformIndex = 0;

		private float m_MinFiringSqrMagnitude, m_MaxFiringSqrMagnitude, m_TargetSqrDistance;

		private bool m_HasGotPlayer;

		private float m_LeadingModifier = 1.5f;

		private Transform m_CurrentFollowTransform;
		public Transform currentFollowTransform
		{
			get
			{
				return m_CurrentFollowTransform;
			}
		}

		protected virtual void Start()
		{
			m_CurrentFireTimerCount = 0f;

			//We're getting a distance of squares here, so we need to convert accordingly.
			m_MinFiringSqrMagnitude = Mathf.Pow(m_MinFiringDistance, 2);
			m_MaxFiringSqrMagnitude = Mathf.Pow(m_MaxFiringDistance, 2);
			m_SqrSafetyRange = Mathf.Pow(m_FireSafetyRange, 2);
		}

		protected virtual void Update()
		{
			GameManager gameManager = GameManager.s_Instance;
			if (gameManager != null && gameManager.rulesProcessor != null)
			{
				if (gameManager.rulesProcessor.IsEndOfRound())
				{
					m_DisableFire = true;
				}
			}

			if (m_FollowPlayer && !m_HasGotPlayer && gameManager != null)
			{
				if (GameManager.s_Tanks.Count > 0)
				{
					TankManager tank = GameManager.s_Tanks[0];
					
					m_FollowTransforms.Add(tank.transform);
					m_TankToFollowMovement = tank.movement;
					m_PlayerTankIndex = m_FollowTransforms.Count - 1;
					m_HasGotPlayer = true;
				}
			}

			if (!m_DisableFire)
			{
				DoTargeting();
				RotateTurret();
				RangeCheck();
				AssessAndFire();
			}
		}

		//This is the general maths behind how far ahead of the player we'll shoot. To alter the primary 'leading' amount, assign a new value to the leadingModifier variable. Recommended amount: 1.5f.
		private void DoTargeting()
		{
			if (m_CurrentFollowTransform == null)
			{
				return;	
			}

			Vector3 position = m_CurrentFollowTransform.position;

			float vInitial = FiringLogic.s_InitialVelocity;

			float vY = vInitial * Mathf.Sin(m_CurrentLaunchAngle);

			float t = (-vY) / (-1 * Mathf.Abs(Physics.gravity.y));

			float tTotal = 2 * t;

			m_ActiveTargetPosition = position;

			//Leading
			if (m_TankToFollowMovement != null && m_CurrentFollowTransformIndex == m_PlayerTankIndex)
			{
				Vector3 tankPositionWithLeading;
				tankPositionWithLeading = position + (m_TankToFollowMovement.speed / m_LeadingModifier * tTotal) * m_CurrentFollowTransform.forward * (float)m_TankToFollowMovement.currentMovementMode;

				if (m_TankToFollowMovement.isMoving)
				{
					m_ActiveTargetPosition = tankPositionWithLeading;
				}
			}

			m_TargetSqrDistance = (transform.position - m_ActiveTargetPosition).sqrMagnitude;
		}

		//This is the 'trigger', and will only fire if other conditions are met.
		private void AssessAndFire()
		{
			if (m_CurrentFollowTransform != null)
			{
				if (m_CurrentFireTimerCount <= 0)
				{
					if (m_TargetDot >= m_FireDotLimit)
					{
						if (m_TargetSqrDistance >= m_SqrSafetyRange)
						{
							Fire();
						}
					}
				}
				else
				{
					m_CurrentFireTimerCount -= Time.deltaTime;
				}
			}
		}

		//Has the current target left our target radius?
		private void CheckCurrentTransformInRange()
		{
			if (m_CurrentFollowTransform == null)
			{
				return;
			}

			Vector3 displacement = transform.position - m_CurrentFollowTransform.position;
			float distanceToTank = displacement.sqrMagnitude;
			if (distanceToTank > m_MaxFiringSqrMagnitude)
			{
				m_CurrentFollowTransform = null;
			}
		}

		//We need to 'ping' to see if the player is in range to lock on to them. This is important when we have multiple possible targets, as it allows the AI to look more natural.
		private void RangeCheck()
		{
			CheckCurrentTransformInRange();
			
			if (m_CurrentFollowTransform != null)
			{
				return;
			}

			float minDistance = Mathf.Infinity;

			int length = m_FollowTransforms.Count;

			for (int i = 0; i < length; i++)
			{
				Transform followTransform = m_FollowTransforms[i];
				if (followTransform == null)
				{
					continue;
				}

				Vector3 displacement = transform.position - followTransform.position;
				float distanceToTank = displacement.sqrMagnitude;
				if (distanceToTank >= m_MinFiringSqrMagnitude && distanceToTank <= m_MaxFiringSqrMagnitude)
				{
					if (distanceToTank < minDistance)
					{
						minDistance = distanceToTank;
						m_CurrentFollowTransform = followTransform;
						m_CurrentFollowTransformIndex = i;
					}
				}
			}
		}

		private void Fire()
		{
			// Change the clip to the firing clip and play it.
			m_FiringAudioSource.clip = m_FiringAudio;
			m_FiringAudioSource.Play();

			Vector3 fireVector = FiringLogic.CalculateFireVector(m_Shell, m_ActiveTargetPosition, m_ShellSource.position, m_CurrentLaunchAngle);

			// Get a random seed to associate with projectile on all clients.
			// This is specifically used for the cluster bomb and any debris spawns, to ensure that their
			// random velocities are identical
			int randSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

			// Immediately fire shell on client - this provides players with the necessary feedback they want
			FireVisualClientShell(fireVector, m_ShellSource.position, randSeed);

			// Reset the launch force.  This is a precaution in case of missing button events.
			m_CurrentLaunchAngle = m_MaxLaunchAngle;

			m_CurrentFireTimerCount = m_TurretFireTimer;

			//Instantiate a muzzzle flash object
			if (m_MuzzleFlashPrefab == null)
			{
				Debug.LogError("MuzzleFlash prefab not assigned");
			}
			else
			{
				GameObject muzzleFlash = Instantiate(m_MuzzleFlashPrefab, m_ShellSource, false) as GameObject;
				muzzleFlash.transform.localPosition = Vector3.zero;
				muzzleFlash.transform.up = fireVector;
			}
		}

		//Create the shell to fire. Shell behaviour is in its own script.
		private Shell FireVisualClientShell(Vector3 shotVector, Vector3 position, int randSeed)
		{
			// Create an instance of the shell and store a reference to it's rigidbody.
			Shell shellInstance = Instantiate<Shell>(m_Shell);

			// Set the shell's velocity and position
			shellInstance.transform.position = position;
			shellInstance.GetComponent<Rigidbody>().velocity = shotVector;

			shellInstance.Setup(9999, GetComponent<Collider>(), randSeed);

			return shellInstance;
		}

		//Handle the behaviour of the turret structure. How fast we look at the target, how much we appear to be leading by, etc.
		private void RotateTurret()
		{
			if (m_CurrentFollowTransform != null)
			{
				Vector3 targetPosition = new Vector3(m_ActiveTargetPosition.x, m_CurrentFollowTransform.position.y, m_ActiveTargetPosition.z);

				Vector3 targetVector = targetPosition - transform.position;

				float targetAngle = 90f - Mathf.Atan2(targetVector.z, targetVector.x) * Mathf.Rad2Deg;

				float rotationAngle = Mathf.LerpAngle(m_TurretTransform.rotation.eulerAngles.y, targetAngle, Time.deltaTime * m_TurretRotationMultiplier);

				m_TurretTransform.rotation = Quaternion.AngleAxis(rotationAngle, Vector3.up);

				m_TargetDot = Vector3.Dot(targetVector.normalized, m_TurretTransform.forward);
			}
		}
	}
}