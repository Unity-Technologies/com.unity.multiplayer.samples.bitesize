using System;
using UnityEngine;
using UnityEngine.UI;
using MLAPI;
using Tanks.Data;
using Tanks.CameraControl;
using Tanks.Shells;
using Tanks.Explosions;
using Random = UnityEngine.Random;
using MLAPI.NetworkVariable;
using MLAPI.Messaging;

namespace Tanks.TankControllers
{
	//This class is responsible for all firing behaviour for the tank.
	public class TankShooting : NetworkBehaviour
	{
		//Public events that are subscribed to by the HUD for updates.
		public event Action<int> ammoQtyChanged;
		public event Action<int> overrideShellChanged;

		//Public event that is fired everytime a shell is.
		public event Action fired;

		// Unique playerID used to identify this tank.
		[SerializeField]
		protected int m_PlayerNumber = 1;

		// Prefab of the default shell.
		[SerializeField]
		protected Rigidbody m_Shell;

		// A child of the tank where the shells are spawned.
		private Transform m_FireTransform;

		// A child of the tank that is oriented towards fire direction
		private Transform m_TurretTransform;

		// A child of the tank that displays the current launch force.
		[SerializeField]
		protected Slider m_AimSlider;

		// The transform that contains the aim slider
		[SerializeField]
		protected Transform m_AimSliderParent;

		// Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
		[SerializeField]
		protected AudioSource m_ShootingAudio;

		//Reference to the audio source used to play hooter audio.
		[SerializeField]
		protected AudioSource m_HooterAudio;

		// Audio that plays when each shot is charging up.
		[SerializeField]
		protected AudioClip m_ChargingClip;

		// Audio pool containing clips that play when each normal shell is fired.
		[SerializeField]
		protected AudioClip[] m_FireClip;

		[SerializeField]
		protected float m_LookDirTickInterval;

		[SerializeField]
		protected float m_LookDirInterpolate;

		[SerializeField]
		protected float m_ShootShakeMinMagnitude;

		[SerializeField]
		protected float m_ShootShakeMaxMagnitude;

		[SerializeField]
		protected float m_ShootShakeDuration;

		[SerializeField]
		protected ExplosionSettings m_FiringExplosion;

		[SerializeField]
		protected float m_ChargeShakeMagnitude;

		[SerializeField]
		protected float m_ChargeShakeNoiseScale;

		[SerializeField]
		protected float m_FireRecoilMagnitude = 0.25f;

		[SerializeField]
		protected float m_FireRecoilSpeed = 4f;

		[SerializeField]
		protected AnimationCurve m_FireRecoilCurve;

		private Vector3 m_DefaultTurretPos;
		private Vector2 m_RecoilDirection;
		private float m_RecoilTime;

		//Variables to set and keep track of a minimum safety distance within which the tank will not fire a shell.
		[SerializeField]
		protected float m_MinimumSafetyRange = 4f;
		private float m_SqrMinimumSafetyRange;
		private float m_SqrTargetRange = 0f;

		// The high angle for shots
		[SerializeField]
		protected float m_MaxLaunchAngle = 70f;

		// The long angle for shots
		[SerializeField]
		protected float m_MinLaunchAngle = 20f;

		// How long the shell can charge for before it is fired at max force.
		[SerializeField]
		protected float m_MaxChargeTime = 0.75f;

		//The rate of fire for this tank.
		private float m_RefireRate;

		// The force that will be given to the shell when the fire button is released.
		private float m_CurrentLaunchAngle;

		// How fast the launch force increases, based on the max charge time.
		private float m_ChargeSpeed;

		// Whether or not the shell has been launched with this button press.
		private bool m_Fired;

		// The turret's facing direction in degrees
		private NetworkVariableFloat m_TurretHeading;

		// Client-side interpolation for any but the local player
		private float m_ClientTurretHeading;
		private float m_ClientTurretHeadingVel;

		//The index of the currently equipped special weapon shell.
		private NetworkVariableInt m_ShellIndex = new NetworkVariableInt(-1);

		//The current ammunition count for special weapons, and the cap to which subsequent pickups will stack.
		private NetworkVariableInt m_SpecialAmmoCount = new NetworkVariableInt();
		private int m_SpecialAmmoMax = 5;

		//The point that we want to fire to.
		private Vector3 m_TargetFirePosition;

		//Whether the input manager has flagged firing, and whether this was the case last tick.
		private bool m_FireInput;
		private bool m_WasFireInput;

		//Internal tracking for reload time.
		private float m_ReloadTime;

		[SerializeField]
		//Array of hooter sounds to draw from when player taps within the safety area.
		protected AudioClip[] m_MeepSounds;

		//The random index that has been selected for this player's hooter sound for this game.
		private NetworkVariableInt m_MeepIndex = new NetworkVariableInt();

		//Last time the look update was ticked.
		private float m_LastLookUpdate;

		//Whether initialization has taken place.
		private bool m_Initialized;


		//Local static reference to the local player's tank for input toggle purposes.
		private static TankShooting s_localTank;

		public static TankShooting s_LocalTank
		{
			get { return s_localTank; }
		}

		//Public field allowing external scripts to block the ability to fire.
		public bool canShoot
		{
			get;
			set;
		}

		//This method is called by the ammo powerup script on collection.
		public void SetSpecialAmmo(int shellTypeIndex, int ammo, string ammoName)
		{
			if (shellTypeIndex != m_ShellIndex.Value)
			{
				//If we have no powerup shell, or another powerup shell type is equipped, override both shell type and ammo with this pickup.
				m_ShellIndex.Value = shellTypeIndex;
				m_SpecialAmmoCount.Value = Mathf.Min(ammo, m_SpecialAmmoMax);
			}
			else
			{
				//Otherwise, just add another helping of ammo for the current special shell type, clamped to the maximum.
				m_SpecialAmmoCount.Value = Mathf.Min(m_SpecialAmmoCount.Value + ammo, m_SpecialAmmoMax);
			}
		}

		//Update the turret's orientation based on a set vector from the input manager.
		public void SetLookDirection(Vector2 target)
		{
			// Subtract from 90 to correct for Atan2 being anti-clockwise from right
			m_TurretHeading.Value = 90 - Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;

			//If we've ticked for a rotation update (to avoid continual traffic), fire the server command to set rotation and update the update tick timer.
			if (Time.realtimeSinceStartup - m_LastLookUpdate >= m_LookDirTickInterval)
			{
				CmdSetLookServerRpc(m_TurretHeading.Value);

				m_LastLookUpdate = Time.realtimeSinceStartup;
			}

			//Set the local turret transform to our new value.
			m_TurretTransform.rotation = Quaternion.AngleAxis(m_TurretHeading.Value, Vector3.up);
		}

		//Calculate the firing position, and perform turret rotation logic to match the new orientation.
		public void SetDesiredFirePosition(Vector3 target)
		{
			m_TargetFirePosition = target;

			// Reorient turret
			Vector3 toAimPos = m_TargetFirePosition - transform.position;
			// Subtract from 90 to correct for Atan2 being anti-clockwise from right
			float newHeading = 90 - Mathf.Atan2(toAimPos.z, toAimPos.x) * Mathf.Rad2Deg;

			if (Time.realtimeSinceStartup - m_LastLookUpdate >= m_LookDirTickInterval)
			{
				CmdSetLookServerRpc(newHeading);

				m_LastLookUpdate = Time.realtimeSinceStartup;
			}
			m_TurretTransform.rotation = Quaternion.AngleAxis(newHeading, Vector3.up);

			//Determine square distance to desired target for range checks later.
			m_SqrTargetRange = toAimPos.sqrMagnitude;
		}

		//Set by input manager to indicate that fire input has been made.
		public void SetFireIsHeld(bool fireHeld)
		{
			m_FireInput = fireHeld;
		}

		//Hooked into the shell index syncvar and fired every time the active special shell index has changed.
		private void OnShellOverrideChanged(int _, int newShell)
		{
			if (newShell == -1)
			{
				return;
			}

			//m_ShellIndex.Value = newShell;

			if (overrideShellChanged != null)
			{
				overrideShellChanged(newShell);
			}
		}

		//Hooked into the specialAmmoCount syncvar, and fired every time the ammo quantity has changed.
		private void OnAmmoChanged(int _, int newAmmo)
		{
			//m_SpecialAmmoCount.Value = newAmmo;
			if (ammoQtyChanged != null)
			{
				ammoQtyChanged(newAmmo);
			}
		}

		private void Awake()
		{
			m_ShellIndex.OnValueChanged += OnShellOverrideChanged;
			m_SpecialAmmoCount.OnValueChanged += OnAmmoChanged;
			m_LastLookUpdate = Time.realtimeSinceStartup;
		}

		private void Start()
		{
			// The rate that the launch force charges up is the range of possible forces by the max charge time.
			m_ChargeSpeed = (m_MaxLaunchAngle - m_MinLaunchAngle) / m_MaxChargeTime;

			//The square of our minimum firing point safety range, for efficient distance comparison.
			m_SqrMinimumSafetyRange = Mathf.Pow(m_MinimumSafetyRange, 2f);

			//If this is the server, choose a random hoot sound. Will be propagated via syncvar.
			if (IsServer)
			{
				m_MeepIndex.Value = Random.Range(0, m_MeepSounds.Length);
			}
		}

		private void OnDisable()
		{
			SetDefaults();
		}

		private void Update()
		{
			if (!IsClient)
				return;

			if (!m_Initialized)
			{
				return;
			}

			if (!IsOwner)
			{
				// Remote players interpolate their facing direction
				if (m_TurretTransform != null)
				{
					m_ClientTurretHeading = Mathf.SmoothDampAngle(m_ClientTurretHeading, m_TurretHeading.Value, ref m_ClientTurretHeadingVel, m_LookDirInterpolate);
					m_TurretTransform.rotation = Quaternion.AngleAxis(m_ClientTurretHeading, Vector3.up);
				}
				return;
			}

			if (s_localTank == null)
			{
				s_localTank = this;
			}

			// Reload time
			if (m_ReloadTime > 0)
			{
				m_ReloadTime -= Time.deltaTime;
			}

			//If the fire button has been released with the target point inside the tank's safety radius, we fire the hooter instead of continuing with fire logic.
			if (m_FireInput && !m_WasFireInput && InSafetyRange())
			{
				m_ShootingAudio.Stop();

				if (!m_HooterAudio.isPlaying)
				{
					CmdFireMeepServerRpc();
				}
			}
			// Otherwise, if the min angle has been exceeded and the shell hasn't yet been launched...
			else if (m_CurrentLaunchAngle <= m_MinLaunchAngle && !m_Fired)
			{
				// ... use the max force and launch the shell.
				m_CurrentLaunchAngle = m_MinLaunchAngle;
				Fire();
			}
			// Otherwise, if the fire button has just started being pressed...
			else if (m_FireInput && !m_WasFireInput && CanFire())
			{
				// ... reset the fired flag and reset the launch force.
				m_Fired = false;

				m_CurrentLaunchAngle = m_MaxLaunchAngle;

				// Change the clip to the charging clip and start it playing.
				m_ShootingAudio.clip = m_ChargingClip;
				m_ShootingAudio.Play();
			}
			// Otherwise, if the fire button is being held and the shell hasn't been launched yet...
			else if (m_FireInput && !m_Fired)
			{
				// Increment the launch force and update the slider.
				m_CurrentLaunchAngle -= m_ChargeSpeed * Time.deltaTime;
			}
			// Otherwise, if the fire button is released and the shell hasn't been launched yet...
			else if (!m_FireInput && m_WasFireInput && !m_Fired)
			{
				// ... launch the shell.
				Fire();
			}

			m_WasFireInput = m_FireInput;

			UpdateAimSlider();

			// Turret shake
			float shakeMagnitude = Mathf.Lerp(0, m_ChargeShakeMagnitude, Mathf.InverseLerp(m_MaxLaunchAngle, m_MinLaunchAngle, m_CurrentLaunchAngle));
			Vector2 shakeOffset = Vector2.zero;

			if (shakeMagnitude > 0)
			{
				shakeOffset.x = (Mathf.PerlinNoise((Time.realtimeSinceStartup + 0) * m_ChargeShakeNoiseScale, Time.smoothDeltaTime) * 2 - 1) * shakeMagnitude;
				shakeOffset.y = (Mathf.PerlinNoise((Time.realtimeSinceStartup + 100) * m_ChargeShakeNoiseScale, Time.smoothDeltaTime) * 2 - 1) * shakeMagnitude;
			}

			if (m_RecoilTime > 0)
			{
				m_RecoilTime = Mathf.Clamp01(m_RecoilTime - Time.deltaTime * m_FireRecoilSpeed);
				float recoilPoint = m_FireRecoilCurve.Evaluate(1 - m_RecoilTime);

				shakeOffset += m_RecoilDirection * recoilPoint * m_FireRecoilMagnitude;
			}

			m_TurretTransform.localPosition = m_DefaultTurretPos + new Vector3(shakeOffset.x, 0, shakeOffset.y);
		}
			
		// Can shoot if the refire time is depleted and shooting has not been overridden externally.
		private bool CanFire()
		{
			return (m_ReloadTime <= 0 && canShoot);
		}
			
		// Returns whether the current targeting point is within the tank's no-fire safety range.
		private bool InSafetyRange()
		{
			return (m_SqrTargetRange <= m_SqrMinimumSafetyRange);
		}

		private void Fire()
		{
			// Set the fired flag so only Fire is only called once.
			m_Fired = true;

			//Determine which shell we should fire.
			Shell shellToFire = GetShellType().GetComponent<Shell>();

			//Determine our firing solution based on our target location and power.
			Vector3 fireVector = FiringLogic.CalculateFireVector(shellToFire, m_TargetFirePosition, m_FireTransform.position, m_CurrentLaunchAngle);

			// Get a random seed to associate with projectile on all clients.
			// This is specifically used for the cluster bomb and any debris spawns, to ensure that their
			// random velocities are identical
			int randSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

			// Immediately fire shell on client - this provides players with the necessary feedback they want
			FireVisualClientShell(fireVector, m_FireTransform.position, randSeed);

			CmdFireServerRpc(fireVector, m_FireTransform.position, randSeed);

			// Reset the launch force.  This is a precaution in case of missing button events.
			m_CurrentLaunchAngle = m_MaxLaunchAngle;

			m_ReloadTime = m_RefireRate;

			// Small screenshake on client
			if (ScreenShakeController.s_InstanceExists)
			{
				ScreenShakeController shaker = ScreenShakeController.s_Instance;

				float chargeAmount = Mathf.InverseLerp(m_MaxLaunchAngle, m_MinLaunchAngle, m_CurrentLaunchAngle);
				float magnitude = Mathf.Lerp(m_ShootShakeMinMagnitude, m_ShootShakeMaxMagnitude, chargeAmount);
				// Scale magnitude 
				shaker.DoShake(m_TargetFirePosition, magnitude, m_ShootShakeDuration);
			}

			m_RecoilTime = 1;
			Vector3 localVector = transform.InverseTransformVector(fireVector);
			m_RecoilDirection = new Vector2(-localVector.x, -localVector.z);
		}

		//Server-side command to propogate turret facing to all clients.
		[ServerRpc]
		private void CmdSetLookServerRpc(float turretHeading)
		{
			m_TurretHeading.Value = turretHeading;
		}
			
		// Called by the client to tell the server it has fired
		[ServerRpc]
		private void CmdFireServerRpc(Vector3 shotVector, Vector3 position, int randSeed)
		{
			// Tell clients this tank has fired
			RpcFireClientRpc(m_PlayerNumber, shotVector, position, randSeed);
		}
			
		// Called by the server to tell clients that a tank has fired
		[ClientRpc]
		private void RpcFireClientRpc(int playerId, Vector3 shotVector, Vector3 position, int randSeed)
		{
			if (fired != null)
			{
				fired();
			}

			// If this fire message is for our own local player id, we skip. We already spawned a projectile
			if (playerId != TankShooting.s_LocalTank.m_PlayerNumber)
			{
				FireVisualClientShell(shotVector, position, randSeed);
			}

			//Decrement special ammo, if necessary.
			if (m_SpecialAmmoCount.Value > 0)
			{
				m_SpecialAmmoCount.Value = m_SpecialAmmoCount.Value - 1;
			}
		}

		//Tell the server to rpc clients to play hooter noise.
		[ServerRpc]
		private void CmdFireMeepServerRpc()
		{
			RpcMeepClientRpc();
		}

		//Play the hooter sound for this tank on clients.
		[ClientRpc]
		private void RpcMeepClientRpc()
		{
			m_HooterAudio.clip = m_MeepSounds[m_MeepIndex.Value];
			m_HooterAudio.Play();
		}

		//This method takes care of all the aesthetic elements of firing - instantiating a shell prefab and playing all visual and audio effects.
		private Shell FireVisualClientShell(Vector3 shotVector, Vector3 position, int randSeed)
		{
			// Create explosion for muzzle flash
			if (ExplosionManager.s_InstanceExists)
			{
				ExplosionManager.s_Instance.SpawnExplosion(position, shotVector, null, m_PlayerNumber, m_FiringExplosion, true);
			}

			// Create an instance of the shell and store a reference to its rigidbody.
			Rigidbody shellInstance =
				Instantiate<Rigidbody>(GetShellType());

			// Set the shell's velocity and position
			shellInstance.transform.position = position;
			shellInstance.velocity = shotVector;

			Shell shell = shellInstance.GetComponent<Shell>();
			shell.Setup(m_PlayerNumber, null, randSeed);

			//Ensure that the shell does not collide with this tank, which fired it.
			Physics.IgnoreCollision(shell.GetComponent<Collider>(), GetComponentInChildren<Collider>(), true);

			//Play the correct firing sound for the shell.
			if (m_SpecialAmmoCount.Value > 0)
			{
				//If a special shell, we query the SpecialProjectileLibrary for a random firing sound.
				m_ShootingAudio.clip = SpecialProjectileLibrary.s_Instance.GetFireSoundForIndex(m_ShellIndex.Value);
			}
			else
			{
				//If a normal shell, we use one of the standard fire sounds populated in this controller.
				m_ShootingAudio.clip = m_FireClip[Random.Range(0, m_FireClip.Length)];
			}

			m_ShootingAudio.Play();

			return shell;
		}

		private Rigidbody GetShellType()
		{
			//We return the standard shell populated in this controller by default.
			Rigidbody shellType = m_Shell;

			//However, if we have a special shell index and there is special ammo remaining, we return that shell info from the SpecialProjectileLibrary instead.
			if ((m_ShellIndex.Value != -1) && (m_SpecialAmmoCount.Value > 0))
			{
				shellType = SpecialProjectileLibrary.s_Instance.GetProjectileDataForIndex(m_ShellIndex.Value).projectilePrefab;
			}

			return shellType;
		}

		public void Init(TankManager manager)
		{
			enabled = false;
			canShoot = false;
			m_Initialized = true;
			m_PlayerNumber = manager.playerNumber;
			m_FireTransform = manager.display.GetFireTransform();
			m_TurretTransform = manager.display.GetTurretTransform();
			m_RefireRate = manager.playerTankType.fireRate;

			// Reparent aim slider
			m_AimSliderParent.SetParent(m_TurretTransform, false);
			m_DefaultTurretPos = m_TurretTransform.localPosition;

			SetDefaults();
		}

		//Updates the fill value of the aim charge arrow graphic.
		private void UpdateAimSlider()
		{
			float aimValue = m_Fired ? m_MaxLaunchAngle : m_CurrentLaunchAngle;
			m_AimSlider.value = m_MaxLaunchAngle - aimValue + m_MinLaunchAngle;
		}

		// This is used by the game manager to reset the tank.
		public void SetDefaults()
		{
			enabled = true;
			//CanShoot = true;
			m_CurrentLaunchAngle = m_MaxLaunchAngle;
			UpdateAimSlider();
			
			if (IsServer)
			{
				m_SpecialAmmoCount.Value = 0;
				m_ShellIndex.Value = -1;
			}

			m_FireInput = m_WasFireInput = false;
			m_Fired = true;
			m_ShootingAudio.Stop();
			m_HooterAudio.Stop();
		}
	}
}