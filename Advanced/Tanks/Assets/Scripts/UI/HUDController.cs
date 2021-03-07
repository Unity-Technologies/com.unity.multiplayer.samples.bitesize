using UnityEngine;
using Tanks.TankControllers;
using UnityEngine.UI;
using Tanks.Data;
using Tanks.Utilities;
using System;

namespace Tanks.UI
{
	/// <summary>
	/// Responsible for displaying and updating all elements of the HUD during gameplay.
	/// </summary>
	public class HUDController : Singleton<HUDController>
	{
		public const int MAX_AMMO = 5;

		//Event that fires when the HUD canvas is enabled.
		public event Action<bool> enabledCanvas;

		//Internal references to tank control scripts for update purposes.
		private TankManager m_TankManager;
		private TankMovement m_Movement;
		private TankShooting m_Shooting;
		private TankHealth m_Health;

		//The transform where score or objective overlays are to be anchored.
		[Header("Score")]
		[SerializeField]
		protected Transform m_ScoreAnchor;

		//References and variables for the info text that appears in the centre of the HUD when a player collects a pickup.
		[Header("Pickup info")]
		[SerializeField]
		protected Text m_PickupInfoText;
		[SerializeField]
		protected float m_PickupInfoTimeout = 2f;
		[SerializeField]
		protected float m_PickupFadeStartTime = 1.5f;
		private float m_NextPickupInfoTimeout;
		private float m_PickupFadeTime;
		private Color m_BaseInfoTextColor;


		[Header("Health display")]
		//The radial image that indicates current health ratio
		[SerializeField]
		protected Image m_HealthSlider;

		//The central health icon.
		[SerializeField]
		protected Image m_HealthIcon;

		//The radial image that indicates current shield ratio
		[SerializeField]
		protected Image m_ShieldSlider;

		//A reference to the icon outline for tinting purposes.
		private Outline m_HealthIconOutline;

		//Storage for the original colour of the health icon for tinting purposes.
		private Color m_BaseHealthColor;

		//Storage for the original outline thickness on the health icon.
		private Vector2 m_BaseHealthOutlineThickness;

		//Variables for the pulsing of the health icon when taking damage.
		[SerializeField]
		protected float m_PulseScaleAdd = 0.5f;

		[SerializeField]
		protected float m_HealthPulseRate = 0.2f;

		private float m_HealthPulseScale = 1f;

		//Variables for the border effect when a tank's shield is active.
		[SerializeField]
		protected float m_ShieldOutlineJitterMax = 2f;
		[SerializeField]
		protected float m_ShieldOutlineJitterRate = 2f;

		[SerializeField]
		protected Color m_ShieldOutlineMaxColor;

		private float m_CurrentShieldJitter;
		private bool m_ShieldOutlineJitter;

		//The canvasgroup used to flash the red "you got damage" screen overlay and its parameters
		[SerializeField]
		protected CanvasGroup m_DamageFlashGroup;
		private float m_FlashAlpha = 0f;
		[SerializeField]
		protected float m_MaxFlashAlpha = 0.5f;

		[Header("Nitro display")]
		//The radial image that indicates nitro ratio
		[SerializeField]
		protected Image m_NitroSlider;
		//The parent object for nitro, to enable and disable it as needed
		[SerializeField]
		protected GameObject m_NitroDisplayParent;

		[Header("Ammo display")]
		//The parent object for the special ammo indicator, to enable and disable it as needed
		[SerializeField]
		protected GameObject m_AmmoDisplayParent;
        
		//References and variables for the special ammo display
		[SerializeField]
		protected Image m_AmmoIcon, m_RadialAmmoCount;

		//Parent object that is used to turn on/off the HUD. Canvas is used if this is null
		[SerializeField]
		protected GameObject m_HudParent;

		//Internal reference to the main canvas
		private Canvas m_HudCanvas;

		//References to HUD-specific audioclips and internal audiosource
		[Header("Pickup Audio")]
		[SerializeField]
		protected AudioClip m_CurrencyPickupSound;
		[SerializeField]
		protected AudioClip m_PickupSound;

		private AudioSource m_AudioSource;
		private AudioClip m_QueuedSound;

		//Internal variables for displaying virtual thumbstick. Interfaces with TankTouchInput class.
		[Header("V-pad")]
		protected Vector3 m_VPadDefault;
		[SerializeField]
		protected float m_VPadBuffer = 15;
		[SerializeField]
		protected GameObject m_VPadMain;
		[SerializeField]
		protected GameObject m_VPadHeldPos;
		[SerializeField]
		protected CanvasGroup m_VPadGroup;

		[SerializeField]
		protected float m_DefaultOpacity = 0.3f;
		[SerializeField]
		protected float m_HeldOpacity = 0.65f;
		[SerializeField]
		protected float m_OpacityChangeSpeed = 2f;

		[SerializeField]
		protected float m_DesiredOpacity;

		//Called by TankTouchInput to update the thumbstick graphic on the HUD to match detected input.
		public void UpdateVPad(Vector2 vPadCenter, Vector2 vPadHeldPosition, bool held)
		{
			if (m_VPadMain != null && m_VPadHeldPos != null)
			{
				if (held)
				{
					m_VPadMain.transform.position = new Vector3(vPadCenter.x, vPadCenter.y, m_VPadDefault.z);
					m_VPadHeldPos.transform.position = new Vector3(vPadHeldPosition.x, vPadHeldPosition.y, m_VPadDefault.z - 0.01f);
				}
				else
				{
					m_VPadMain.transform.position = m_VPadDefault;
					m_VPadHeldPos.transform.position = m_VPadDefault;
				}
			}
		}

		//Called by TankTouchInput to initialize the thumbstick graphic to match its parameters (left/right orientation, input area size).
		public void ShowVPad(float horizontalArea, float verticalArea)
		{
			if (m_VPadMain != null && m_VPadHeldPos != null && m_VPadGroup != null)
			{
				m_VPadGroup.gameObject.SetActive(true);
				m_VPadGroup.alpha = m_DefaultOpacity;

				// Set size
				float canvasScale = m_HudCanvas.scaleFactor;
				float invCanvasScale = 1 / canvasScale;
				float canvasWidth = Screen.width * invCanvasScale;
				float canvasHeight = Screen.height * invCanvasScale;
				float desiredSize = Mathf.Min(canvasWidth * horizontalArea, canvasHeight * verticalArea) - m_VPadBuffer * 2;
				float desiredPos = Mathf.Min(Screen.width * horizontalArea, Screen.height * verticalArea) * 0.5f + m_VPadBuffer;

				Vector2 desiredPosVector = new Vector3(desiredPos, desiredPos, 0);

				m_VPadDefault = desiredPosVector;
				m_VPadMain.transform.localPosition = desiredPosVector;
				m_VPadHeldPos.transform.localPosition = desiredPosVector;

				RectTransform rectTransform = m_VPadMain.transform as RectTransform;
				if (rectTransform != null)
				{
					rectTransform.sizeDelta = new Vector2(desiredSize, desiredSize);
				}

				bool leftyMode = PlayerDataManager.s_InstanceExists && PlayerDataManager.s_Instance.isLeftyMode;
				if (leftyMode)
				{
					m_VPadDefault.x = Screen.width - m_VPadDefault.x;
				}
			}
		}

		//Hides the thumbstick graphics
		public void HideVPad()
		{
			if (m_VPadMain != null && m_VPadHeldPos != null)
			{
				m_VPadGroup.gameObject.SetActive(false);
			}
		}

		//Called by TankTouchInput to bring the thumbstick to full opacity when input is held.
		public void SetVPadHeld()
		{
			m_DesiredOpacity = m_HeldOpacity;
		}

		//Called by TankTouchInput to make the thumstick transparent when input is released.
		public void SetVPadReleased()
		{
			m_DesiredOpacity = m_DefaultOpacity;
		}

		protected void Start()
		{
			m_BaseInfoTextColor = m_PickupInfoText.color;
			m_BaseHealthColor = m_HealthIcon.color;

			m_HealthIconOutline = m_HealthIcon.GetComponent<Outline>();
			m_BaseHealthOutlineThickness = m_HealthIconOutline.effectDistance;

			m_AudioSource = GetComponent<AudioSource>();

			m_HudCanvas = GetComponent<Canvas>();
			SetHudEnabled(false);

			m_NitroDisplayParent.SetActive(false);
			m_AmmoDisplayParent.SetActive(false);
		}

		protected void Update()
		{
			if (m_PickupInfoText.gameObject.activeSelf)
			{
				if (Time.time >= m_PickupFadeTime)
				{
					float fadeTime = m_NextPickupInfoTimeout - m_PickupFadeTime;

					m_PickupInfoText.color = Color.Lerp(new Color(m_BaseInfoTextColor.r, m_BaseInfoTextColor.g, m_BaseInfoTextColor.b, 0f), m_BaseInfoTextColor, (m_NextPickupInfoTimeout - Time.time) / fadeTime);
				}

				if (Time.time >= m_NextPickupInfoTimeout)
				{
					m_PickupInfoText.gameObject.SetActive(false);
				}

				if (m_QueuedSound != null)
				{
					PlayInterfaceAudio(m_QueuedSound);
					m_QueuedSound = null;
				}
			}

			//If the health icon has been scaled up due to the player receiving damage, scale it down to base size by increments each tick.
			if (m_HealthPulseScale > 1f)
			{
				m_HealthPulseScale -= Time.deltaTime * m_HealthPulseRate;

				if (m_HealthPulseScale <= 1.01f)
				{
					m_HealthPulseScale = 1f;
				}

				m_HealthIcon.transform.localScale = Vector3.one * m_HealthPulseScale;
			}

			//If shield effect is active, increment border size and change outline colour proportionally.
			if (m_ShieldOutlineJitter)
			{
				m_CurrentShieldJitter += Time.deltaTime * m_ShieldOutlineJitterRate;

				if (m_CurrentShieldJitter > m_ShieldOutlineJitterMax)
				{
					m_CurrentShieldJitter = 1f;
				}

				m_HealthIconOutline.effectColor = Color.Lerp(m_ShieldOutlineMaxColor, Color.white, ((m_ShieldOutlineJitterMax - m_CurrentShieldJitter) / (m_ShieldOutlineJitterMax - 1f)));

				m_HealthIconOutline.effectDistance = Vector2.one * m_CurrentShieldJitter;
			}

			//If the damage flash overlay has been enabled due to the player receiving damage, scale down its opacity by increments, and disable when transparent.
			if (m_DamageFlashGroup.gameObject.activeSelf)
			{
				if (m_FlashAlpha > 0)
				{
					m_FlashAlpha -= Time.deltaTime;
					m_DamageFlashGroup.alpha = m_FlashAlpha;
				}
				else
				{
					m_DamageFlashGroup.gameObject.SetActive(false);
				}
			}

			//If we have thumbstick graphics, scale their opacity based on the values set by the relevant methods.
			if (m_VPadGroup != null)
			{
				float currentOpacity = m_VPadGroup.alpha;
				float diff = m_DesiredOpacity - currentOpacity;
				float absDiff = Mathf.Abs(diff);

				if (absDiff > Mathf.Epsilon)
				{
					float changeAmount = Mathf.Sign(diff) *
					                     Mathf.Min(Time.deltaTime * m_OpacityChangeSpeed, absDiff);

					m_VPadGroup.alpha += changeAmount;
				}
			}
		}

		//This method is subscribed to the player's tank's health change event, and fires the heart pulse and damage flash effects, as well as altering the fill ratio of the radial indicator graphic.
		private void UpdateHealth(float newHealthRatio)
		{
			m_HealthSlider.fillAmount = newHealthRatio;

			if (newHealthRatio != 1f)
			{
				m_FlashAlpha = m_MaxFlashAlpha;
				m_DamageFlashGroup.gameObject.SetActive(true);

				m_HealthPulseScale = 1f + m_PulseScaleAdd;
			}
		}

		//This method is subscribed to the player's tank's shield change event, and starts/stops the shield effects, as well as altering the fill ratio of the radial shield indicator graphic.
		private void UpdateShield(float newShieldRatio)
		{
			m_ShieldSlider.fillAmount = newShieldRatio;

			//If we have a shield, tint the heart and enable the pulse effect flag.
			if (newShieldRatio > 0)
			{
				m_HealthIcon.color = m_ShieldSlider.color;
				m_ShieldOutlineJitter = true;
			}

			//Otherwise if shield is depleted, stop the pulse effect flag and reset heart tint, border sizes, border colour, etc, to defaults.
			else
			{
				m_HealthIcon.color = m_BaseHealthColor;
				m_HealthIconOutline.effectColor = Color.black;
				m_ShieldOutlineJitter = false;
				m_HealthIconOutline.effectDistance = m_BaseHealthOutlineThickness;
			}
		}

		//This method is subscribed to the player's tank's death event, and disables the HUD when it is fired
		private void OnPlayerDeath()
		{
			SetHudEnabled(false);
		}

		//This method is subscribed to the player's tank's spawn event, and resets the HUD when it is fired
		private void OnPlayerRespawn()
		{
			m_FlashAlpha = 0f;
			SetHudEnabled(true);
		}

		//This method is subscribed to the player's tank's nitro change event. It enables/disables the HUD overlay if the tank has Nitro, and changes its radial graphic value as required
		private void UpdateNitro(float nitroRatio)
		{
			bool hasNitro = (nitroRatio > 0f);

			if (nitroRatio > 0f)
			{
				m_NitroSlider.fillAmount = nitroRatio;
			}

			m_NitroDisplayParent.SetActive(hasNitro);
		}

		//This method is subscribed to the player's tank's ammo quantity change events, and enables/disables the HUD overlay and changes its value when fired
		private void UpdateAmmo(int newAmmo)
		{
			bool hasAmmo = (newAmmo > 0);

			if (m_AmmoDisplayParent != null)
			{
				m_AmmoDisplayParent.SetActive(hasAmmo);

				if (hasAmmo)
				{
					m_RadialAmmoCount.fillAmount = (float)newAmmo / MAX_AMMO;
				}
			}
		}

		//This method is subscribed to the player's tank's ammo type change event, and switches the ammo icon on the HUD when fired
		private void UpdateShellInfo(int newShell)
		{
			ProjectileDefinition projectileDefinition = SpecialProjectileLibrary.s_Instance.GetProjectileDataForIndex(newShell);
			m_AmmoIcon.sprite = projectileDefinition.weaponIcon;
			m_AmmoIcon.color = projectileDefinition.weaponColor;
			m_RadialAmmoCount.color = projectileDefinition.weaponColor;
		}

		//This method is subscribed to the player's tank's currency change event, and plays the relevant sound when fired
		private void UpdatePickUpCurrency(int currency)
		{
			if (currency > 0)
			{
				PlayInterfaceAudio(m_CurrencyPickupSound);
			}
		}

		public void SetHudEnabled(bool enabled)
		{
			if (m_HudParent != null)
			{
				m_HudParent.SetActive(enabled);
			}
			else
			{
				m_HudCanvas.enabled = enabled;
			}

			if (enabledCanvas != null)
			{
				enabledCanvas(enabled);
			}

			m_PickupInfoText.gameObject.SetActive(false);
		}

		//This method is subscribed to the player's tank's pickup message change event, and makes the pickup text visible with the correct item name when fired
		private void OnPickupTextChanged(string itemName)
		{
			m_PickupInfoText.gameObject.SetActive(true);
			m_PickupInfoText.color = m_BaseInfoTextColor;
			m_PickupInfoText.text = itemName + " collected.";

			m_QueuedSound = m_PickupSound;

			m_NextPickupInfoTimeout = Time.time + m_PickupInfoTimeout;
			m_PickupFadeTime = Time.time + m_PickupFadeStartTime;
		}

		//This method is called during setup, and subscribes all the HUD's listener methods to the local player's tank
		public void InitHudPlayer(TankManager playerTank)
		{
			m_TankManager = playerTank;
			m_Health = m_TankManager.health;
			m_Shooting = m_TankManager.shooting;
			m_Movement = m_TankManager.movement;

			m_Shooting.ammoQtyChanged += UpdateAmmo;
			m_Shooting.overrideShellChanged += UpdateShellInfo;

			m_Health.healthChanged += UpdateHealth;
			m_Health.shieldChanged += UpdateShield;
			m_Health.playerDeath += OnPlayerDeath;
			m_Health.playerReset += OnPlayerRespawn;

			m_Movement.nitroChanged += UpdateNitro;

			m_TankManager.onPickupCollected += OnPickupTextChanged;
			m_TankManager.onCurrencyChanged += UpdatePickUpCurrency;

			SetHudEnabled(false);
		}

		//We unsubscribe all listeners from the player's tank when this object is destroyed
		protected override void OnDestroy()
		{
			if (m_Shooting != null)
			{
				m_Shooting.ammoQtyChanged -= UpdateAmmo;
				m_Shooting.overrideShellChanged -= UpdateShellInfo;
			}

			if (m_Health != null)
			{
				m_Health.healthChanged -= UpdateHealth;
				m_Health.shieldChanged -= UpdateShield;
				m_Health.playerDeath -= OnPlayerDeath;
				m_Health.playerReset -= OnPlayerRespawn;
			}

			if (m_Movement != null)
			{
				m_Movement.nitroChanged -= UpdateNitro;
			}

			if (m_TankManager != null)
			{
				m_TankManager.onPickupCollected -= OnPickupTextChanged;
				m_TankManager.onCurrencyChanged -= UpdatePickUpCurrency;
			}

			base.OnDestroy();
		}

		//This method is used to play dedicated HUD audio effects via the HUD audiosource.
		public void PlayInterfaceAudio(AudioClip soundEffect)
		{
			m_AudioSource.PlayOneShot(soundEffect);
		}


		/// <summary>
		/// Creates an instance of the score overlay panel for multiplayer.
		/// </summary>
		/// <returns>Reference to ScoreDisplay script of new instance.</returns>
		public HUDMultiplayerScore CreateScoreDisplay()
		{
			//This method is called from the GameManager during multiplayer setup.
			GameObject scorePrefab = GameSettings.s_Instance.mode.hudScoreObject;

			GameObject mpScore = (GameObject)Instantiate(scorePrefab);
			mpScore.transform.SetParent(m_ScoreAnchor, false);

			return mpScore.GetComponent<HUDMultiplayerScore>();
		}


	}
}
