using UnityEngine;
using MLAPI;
using MLAPI.NetworkVariable;
using System;
using Random = UnityEngine.Random;
using Tanks.CameraControl;

namespace Tanks.TankControllers
{
	//This class is responsible for the movement of the tank and related animation/audio.
	public class TankMovement : NetworkBehaviour
	{
		//Enum to define how the tank is moving towards its desired direction.
		public enum MovementMode
		{
			Forward = 1,
			Backward = -1
		}

		//Variables for the server to set this tank's speed and turn rate from the TankLibrary and cascade them to clients when spawning the tank.
		private NetworkVariableFloat m_OriginalSpeed = new NetworkVariableFloat(0f);
		private NetworkVariableFloat m_OriginalTurnRate = new NetworkVariableFloat(0f);

		// How fast the tank moves forward and back. We sync this stat from server to prevent local cheatery.
		private NetworkVariableFloat m_Speed = new NetworkVariableFloat(12f);

		public float speed
		{
			get
			{
				return m_Speed.Value;
			}
		}

		// How fast the tank turns in degrees per second. We sync this stat from server to prevent local cheatery.
		private NetworkVariableFloat m_TurnSpeed = new NetworkVariableFloat(180f);

		// Reference to the audio source used to play engine sounds. NB: different to the shooting audio source.
		[SerializeField]
		protected AudioSource m_MovementAudio;

		// Audio to play when the tank isn't moving.
		[SerializeField]
		protected AudioClip m_EngineIdling;

		// Audio to play when the tank is moving.
		[SerializeField]
		protected AudioClip m_EngineDriving;

		[SerializeField]
		protected AudioClip m_EngineStartDriving;

		[SerializeField]
		protected AudioClip m_NitroDriving;

		// Reference used to move the tank.
		private Rigidbody m_Rigidbody;

		public Rigidbody Rigidbody
		{
			get
			{
				return m_Rigidbody;
			}
		}

		//Reference to the display prefab for the tank.
		private TankDisplay m_TankDisplay;

		//The amount to shake the tank mesh when nitro is active.
		[SerializeField]
		protected float m_NitroShakeMagnitude;

		//The direction that the player wants to move in.
		private Vector2 m_DesiredDirection;

		//The remaining distance for which Nitro will remain active. Server-driven.
		private NetworkVariableFloat m_PowerupDistance = new NetworkVariableFloat();

		[SerializeField]
		protected float m_MaxPowerupDistance = 50f;

		private int m_BoostShakeId;

		//The tank's position last tick.
		private Vector3 m_LastPosition;

		private MovementMode m_CurrentMovementMode;

		public MovementMode currentMovementMode
		{
			get
			{
				return m_CurrentMovementMode;
			}
		}

		//Whether the tank was undergoing movement input last tick.
		private bool m_HadMovementInput;

		//The final velocity of the tank.
		public Vector3 velocity
		{
			get;
			protected set;
		}

		//Whether the tank is moving.
		public bool isMoving
		{
			get
			{
				return m_DesiredDirection.sqrMagnitude > 0.01f;
			}
		}

		//Public event that fires when nitro value is changed. Used by the HUD for update.
		public event Action<float> nitroChanged;

		//Accepts a TankManager reference to pull in all necessary data and references.
		public void Init(TankManager manager)
		{
			m_PowerupDistance.OnValueChanged += OnBoostChange;

			enabled = false;
			m_TankDisplay = manager.display;
			if (IsHost)
			{
				m_OriginalSpeed.Value = manager.playerTankType.speed;
				m_OriginalTurnRate.Value = manager.playerTankType.turnRate;
			}

			SetDefaults();
		}

		//Called by the active tank input manager to set the movement direction of the tank.
		public void SetDesiredMovementDirection(Vector2 moveDir)
		{
			m_DesiredDirection = moveDir;
			m_HadMovementInput = true;

			if (m_DesiredDirection.sqrMagnitude > 1)
			{
				m_DesiredDirection.Normalize();
			}
		}

		private void Awake()
		{
			//Get our rigidbody, and init originalconstraints for enable/disable code.
			LazyLoadRigidBody();
			m_OriginalConstrains = m_Rigidbody.constraints;

			m_CurrentMovementMode = MovementMode.Forward;
			m_BoostShakeId = -1;
		}

		private void LazyLoadRigidBody()
		{
			if (m_Rigidbody != null)
			{
				return;
			}

			m_Rigidbody = GetComponent<Rigidbody>();
		}


		private void Start()
		{
			m_LastPosition = transform.position;
		}

		private void Update()
		{
			if (IsOwner)
			{
				if (!m_HadMovementInput || !isMoving)
				{
					m_DesiredDirection = Vector2.zero;
				}

				BoostShake();

				m_HadMovementInput = false;
			}

			EngineAudio();
		}

		private void EngineAudio()
		{
			//If speed is zero because our movement is disabled, ignore this method.
			if(m_Speed.Value == 0)
			{
				return;
			}

			// If there is no movement (the tank is stationary)
			if ((m_LastPosition - transform.position).sqrMagnitude <= Mathf.Epsilon)
			{
				//If the tank isn't playing the idling clip, flip it out for the idling clip.
				if (m_MovementAudio.clip != m_EngineIdling)
				{
					m_MovementAudio.loop = true;
					m_MovementAudio.clip = m_EngineIdling;
					m_MovementAudio.Play();
				}
			}
			else
			{
				// Otherwise if the tank is moving and the idling clip is currently playing, fire the StartDriving clip.
				if (m_MovementAudio.clip == m_EngineIdling)
				{
					m_MovementAudio.clip = m_EngineStartDriving;

					//We don't want to loop this sound.
					m_MovementAudio.loop = false;
					m_MovementAudio.Play();
				}
				else
				{
					//We're moving, so if we're not under the infuence of Nitro or if the StartDriving clip is populated but no longer playing (because it's finished), switch to the standard EngineDriving clip.
					if(((m_PowerupDistance.Value <= 0) && (m_MovementAudio.clip == m_NitroDriving)) || ((m_MovementAudio.clip == m_EngineStartDriving) && (!m_MovementAudio.isPlaying)))
					{
						m_MovementAudio.loop = true;

						m_MovementAudio.clip = m_EngineDriving;

						m_MovementAudio.Play();
					}

					//If we're moving under the influence of Nitro, and we're not playing the Nitro clip, play the nitro sound.
					else if((m_PowerupDistance.Value > 0) && (m_MovementAudio.clip != m_NitroDriving))
					{
						m_MovementAudio.loop = true;

						m_MovementAudio.clip = m_NitroDriving;

						m_MovementAudio.Play();
					}
				}
			}
		}
			
		private void BoostShake()
		{
			if (isMoving && m_PowerupDistance.Value > 0)
			{
				if (m_BoostShakeId < 0)
				{
					// Screen shake go!
					if (ScreenShakeController.s_InstanceExists)
					{
						ScreenShakeController shaker = ScreenShakeController.s_Instance;

						// Scale magnitude 
						m_BoostShakeId = shaker.DoPerpetualShake(Vector2.right, m_NitroShakeMagnitude);
					}
				}
			}
			else if (m_BoostShakeId >= 0)
			{
				if (ScreenShakeController.s_InstanceExists)
				{
					ScreenShakeController shaker = ScreenShakeController.s_Instance;

					shaker.StopShake(m_BoostShakeId);
					m_BoostShakeId = -1;
				}
			}
		}


		private void FixedUpdate()
		{
			//If we're on server, we need to decrement any nitro distance this tank covered over the previous frame, and revert our movement syncvars accordingly if we're out of go-juice.
			if (IsServer)
			{
				if (m_PowerupDistance.Value > 0)
				{
					m_PowerupDistance.Value = m_PowerupDistance.Value - velocity.sqrMagnitude;

					if (m_PowerupDistance.Value <= 0)
					{
						ResetMovementVariables();
						m_PowerupDistance.Value = 0;
					}
				}
			}

			velocity = transform.position - m_LastPosition;
			m_LastPosition = transform.position;

			if (!IsOwner)
			{
				return;
			}

			// Adjust the rigidbody's position and orientation in FixedUpdate.
			if (isMoving)
			{
				Turn();
				Move();
			}
		}


		private void Move()
		{
			float moveDistance = m_DesiredDirection.magnitude * m_Speed.Value * Time.deltaTime;

			// Create a movement vector based on the input, speed and the time between frames, in the direction the tank is facing.
			Vector3 movement = m_CurrentMovementMode == MovementMode.Backward ? -transform.forward : transform.forward;
			movement *= moveDistance;

			// Apply this movement to the rigidbody's position.
			// Also immediately move our transform so that attached joints update this frame
			m_Rigidbody.position = m_Rigidbody.position + movement;
			transform.position = m_Rigidbody.position;
		}


		private void Turn()
		{
			// Determine turn direction
			float desiredAngle = 90 - Mathf.Atan2(m_DesiredDirection.y, m_DesiredDirection.x) * Mathf.Rad2Deg;

			// Check whether it's shorter to move backwards here
			Vector2 facing = new Vector2(transform.forward.x, transform.forward.z);
			float facingDot = Vector2.Dot(facing, m_DesiredDirection);

			// Only change if the desired direction is a significant change over our current one
			if (m_CurrentMovementMode == MovementMode.Forward &&
				facingDot < -0.5)
			{
				m_CurrentMovementMode = MovementMode.Backward;
			}
			if (m_CurrentMovementMode == MovementMode.Backward &&
				facingDot > 0.5)
			{
				m_CurrentMovementMode = MovementMode.Forward;
			}
			// currentMovementMode =  >= 0 ? MovementMode.Forward : MovementMode.Backward;

			if (m_CurrentMovementMode == MovementMode.Backward)
			{
				desiredAngle += 180;
			}

			// Determine the number of degrees to be turned based on the input, speed and time between frames.
			float turn = m_TurnSpeed.Value * Time.deltaTime;

			// Make this into a rotation in the y axis.
			Quaternion desiredRotation = Quaternion.Euler(0f, desiredAngle, 0f);

			// Approach that direction
			// Also immediately turn our transform so that attached joints update this frame
			m_Rigidbody.rotation = Quaternion.RotateTowards(m_Rigidbody.rotation, desiredRotation, turn);
			transform.rotation = m_Rigidbody.rotation;
		}


		// This function is called at the start of each round to make sure each tank is set up correctly.
		public void SetDefaults()
		{
			enabled = true;
			// TODO: MLAPI - should really separate this to a server-side action for NetworkVariables, and a client-side vis-only action
			if (IsServer)
			{
				m_PowerupDistance.Value = 0f;
				ResetMovementVariables();
			}
			LazyLoadRigidBody();

			m_Rigidbody.velocity = Vector3.zero;
			m_Rigidbody.angularVelocity = Vector3.zero;

			m_DesiredDirection = Vector2.zero;
			m_CurrentMovementMode = MovementMode.Forward;
		}

		//Disable movement, and also disable our engine noise emitter.
		public void DisableMovement()
		{
			if (IsServer)
			{
				m_Speed.Value = 0;
			}
			m_MovementAudio.enabled = false;
		}

		//Reenable movement, and also the engine noise emitter.
		public void EnableMovement()
		{
			if (IsServer)
			{
				m_Speed.Value = m_OriginalSpeed.Value;
			}

			m_MovementAudio.enabled = true;
		}

		//NOTE: This method will only be called from server-based instances of the Nitro pickup.
		public void SetMovementPowerupVariables(float effectiveDistance, float speedBoostRatio, float turnBoostRatio)
		{
			//We don't want the boost powerup to stack its effects. So if we have no boost left, we set all variables. Otherwise, we just top up the effective distance again.
			if (m_PowerupDistance.Value == 0)
			{
				m_Speed.Value = m_OriginalSpeed.Value * speedBoostRatio;
				m_TurnSpeed.Value = m_OriginalTurnRate.Value * turnBoostRatio;
			}

			m_PowerupDistance.Value = Mathf.Clamp(m_PowerupDistance.Value + effectiveDistance, 0f, m_MaxPowerupDistance);
		}

		//We freeze the rigibody when the control is disabled to avoid the tank drifting!
		protected RigidbodyConstraints m_OriginalConstrains;

		public void SetAudioSourceActive(bool isActive)
		{
			if (m_MovementAudio != null)
			{
				m_MovementAudio.enabled = isActive;
			}
		}

		//On disable, lock our rigidbody in position.
		void OnDisable()
		{
			m_OriginalConstrains = m_Rigidbody.constraints;
			m_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
		}

		//On enable, restore our rigidbody's range of movement.
		void OnEnable()
		{
			m_Rigidbody.constraints = m_OriginalConstrains;
		}

		//Reset our movement values to their original values for this tank. This is to reset Nitro effects.
		void ResetMovementVariables()
		{
			if (!IsServer)
			{
				throw new InvalidOperationException();
			}
			m_Speed.Value = m_OriginalSpeed.Value;
			m_TurnSpeed.Value = m_OriginalTurnRate.Value;
		}

		//This is hooked into the powerupDistance syncvar, and will fire whenever this value updates from server.
		void OnBoostChange(float _, float newPowerupDistance)
		{
			//m_PowerupDistance.Value = newPowerupDistance;

			//We only want nitro particles to be enabled if we have nitro in the tank, and if the tank's not been frozen due to round end.
			m_TankDisplay.SetNitroParticlesActive((newPowerupDistance > 0f) && (m_Speed.Value > 0));

			if (nitroChanged != null)
			{
				nitroChanged(m_PowerupDistance.Value / m_MaxPowerupDistance);
			}
		}
	}
}