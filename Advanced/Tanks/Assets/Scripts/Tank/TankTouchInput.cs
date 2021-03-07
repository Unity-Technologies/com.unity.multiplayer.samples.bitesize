using UnityEngine;
using UnityEngine.EventSystems;
using Tanks.UI;
using Tanks.Data;
using Tanks.Utilities;

namespace Tanks.TankControllers
{
	/// <summary>
	/// Input module that handles touch controls
	/// </summary>
	public class TankTouchInput : TankInputModule
	{
		/// <summary>
		/// Percentage of screen height in the corner of the screen where touch is detected as a move, at maximum
		/// </summary>
		[SerializeField]
		protected float m_VerticalMovementAreaPercentage = 0.4f;
		[SerializeField]
		protected float m_MinVerticalMovementAreaPercentage = 0.2f;
		/// <summary>
		/// Percentage of screen width in the corner of the screen where touch is detected as a move, at maximum
		/// </summary>
		[SerializeField]
		protected float m_HorizontalMovementAreaPercentage = 0.3f;
		[SerializeField]
		protected float m_MinHorizontalMovementAreaPercentage = 0.15f;
		/// <summary>
		/// Minimum distance in inches for slowest movement
		/// </summary>
		[SerializeField]
		protected float m_TouchDistMinSensitivity = 0.05f;
		/// <summary>
		/// Maximum distance in inches for fastest movement
		/// </summary>
		[SerializeField]
		protected float m_TouchDistMaxSensitivity = 0.8f;
		/// <summary>
		/// Extra distance over touchDistMaxSensitivity that causes the v-pad center to be pulled towards the finger
		/// </summary>
		[SerializeField]
		protected float m_TouchDistCenterPull = 0.1f;
		/// <summary>
		/// Object that displays movement direction under tank
		/// </summary>
		[SerializeField]
		protected GameObject m_MovementIndicator;

		private int m_MovementTouchId;
		private int m_FiringTouchId;

		private bool m_StoppedMoving;
		private bool m_StoppedFiring;
		private bool m_LeftyMode;

		private float m_HorizontalArea;
		private float m_VerticalArea;

		private Touch? m_MovementTouch;
		private Touch? m_FiringTouch;

		private Vector2 m_PadCenter;

		protected override void Awake()
		{
			base.Awake();
			Input.simulateMouseWithTouches = false;

			InitDirectionAndSize();
		}

        // TODO: MLAPI figure out the equivalent behavior for override
        public void OnStartAuthority()
        {
            if (MobileUtilities.IsOnMobile())
            {
                OnBecomesActive();
            }
        }

        public void InitDirectionAndSize()
		{
			PlayerDataManager saveData = PlayerDataManager.s_Instance;

			m_LeftyMode = saveData != null && saveData.isLeftyMode;

			m_HorizontalArea = saveData != null ? Mathf.Lerp(m_MinHorizontalMovementAreaPercentage, m_HorizontalMovementAreaPercentage, saveData.thumbstickSize) : m_HorizontalMovementAreaPercentage;
			m_VerticalArea = saveData != null ? Mathf.Lerp(m_MinVerticalMovementAreaPercentage, m_VerticalMovementAreaPercentage, saveData.thumbstickSize) : m_VerticalMovementAreaPercentage;
		}

		public void ForceUpdateUi()
		{
			UpdateUi();
		}


		protected override void Update()
		{
			if (!IsOwner)
			{
				return;
			}

			ProcessTouches();

			base.Update();

			UpdateUi();
		}


		protected override void OnBecomesActive()
		{
			if (HUDController.s_InstanceExists)
			{
				HUDController.s_Instance.ShowVPad(m_HorizontalArea, m_VerticalArea);
			}

			if (m_MovementIndicator != null)
			{
				m_MovementIndicator.SetActive(true);
			}

			OnInputMethodChanged(true);
		}


		protected override void OnBecomesInactive()
		{
			if (HUDController.s_InstanceExists)
			{
				HUDController.s_Instance.HideVPad();
			}

			if (m_MovementIndicator != null)
			{
				m_MovementIndicator.SetActive(false);
			}

			m_MovementTouchId = -1;
			m_FiringTouchId = -1;
		}

		/// <summary>
		/// Update UI elements
		/// </summary>
		protected virtual void UpdateUi()
		{
			HUDController hud = HUDController.s_Instance;
			if (hud != null)
			{
				if (m_MovementTouch != null)
				{
					hud.UpdateVPad(m_PadCenter, m_MovementTouch.Value.position, true);
					hud.SetVPadHeld();
				}
				else
				{
					hud.UpdateVPad(Vector2.zero, Vector2.zero, false);
					hud.SetVPadReleased();
				}
			}
		}

		/// <summary>
		/// Process touch input - find and associate touches with correct input
		/// </summary>
		protected virtual void ProcessTouches()
		{
			// Clear touches
			m_MovementTouch = null;
			m_FiringTouch = null;
			m_StoppedMoving = false;
			m_StoppedFiring = false;

			// Update input touch IDs
			for (int i = 0; i < Input.touchCount; ++i)
			{
				Touch touch = Input.GetTouch(i);

				switch (touch.phase)
				{
					case TouchPhase.Began:
						if (IsValidMovementTouch(touch))
						{
							m_MovementTouchId = touch.fingerId;
							m_MovementTouch = touch;
							m_PadCenter = touch.position;
						}
						else if (IsValidFireTouch(touch) && touch.fingerId != m_MovementTouchId)
						{
							m_FiringTouchId = touch.fingerId;
							m_FiringTouch = touch;
						}
						break;

					case TouchPhase.Canceled:
					case TouchPhase.Ended:
						// Look for this touch
						if (touch.fingerId == m_FiringTouchId)
						{
							m_FiringTouchId = -1;
							m_FiringTouch = null;
							m_StoppedFiring = true;
						}

						if (touch.fingerId == m_MovementTouchId)
						{
							m_MovementTouchId = -1;
							m_MovementTouch = null;
							m_StoppedMoving = true;
						}
						break;

					case TouchPhase.Moved:
					case TouchPhase.Stationary:
						// Look for this touch
						if (touch.fingerId == m_FiringTouchId)
						{
							m_FiringTouch = touch;
						}

						if (touch.fingerId == m_MovementTouchId)
						{
							m_MovementTouch = touch;
						}
						break;
				}
			}
		}

		/// <summary>
		/// Firing touch causes tank to fire at touch position
		/// </summary>
		protected override bool DoFiringInput()
		{
			if (m_StoppedFiring)
			{
				SetFireIsHeld(false);
			}
			else if (m_FiringTouch != null)
			{
				Ray mouseRay = Camera.main.ScreenPointToRay(m_FiringTouch.Value.position);
				float hitDist;
				RaycastHit hit;
				if (Physics.Raycast(mouseRay, out hit, float.PositiveInfinity, m_GroundLayerMask))
				{
					SetDesiredFirePosition(hit.point);
				}
				else if (m_FloorPlane.Raycast(mouseRay, out hitDist))
				{
					SetDesiredFirePosition(mouseRay.GetPoint(hitDist));
				}

				SetFireIsHeld(true);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Movement touch behaves as a virtual stick
		/// </summary>
		protected override bool DoMovementInput()
		{
			// Calculate desired movement direction
			if (m_MovementTouch != null && !m_StoppedMoving)
			{
				Vector2 fingerpos = m_MovementTouch.Value.position;
				Vector2 movementDir = fingerpos - m_PadCenter;
				float magnitude = movementDir.magnitude;
				float dpi = Screen.dpi > 0 ? Screen.dpi : 72;

				float minTouch = m_TouchDistMinSensitivity * dpi;
				float maxTouch = m_TouchDistMaxSensitivity * dpi;
				float touchPullThreshold = maxTouch + (m_TouchDistCenterPull * dpi);

				float moveSpeed = Mathf.Clamp01((magnitude - minTouch) / (maxTouch - minTouch));

				Vector3 cameraDirection = new Vector3(movementDir.x, movementDir.y, 0);

				if (cameraDirection.sqrMagnitude > 0.01f)
				{
					// Get camera relative vectors
					Vector3 worldUp = Camera.main.transform.TransformDirection(Vector3.up);
					worldUp.y = 0;
					worldUp.Normalize();
					Vector3 worldRight = Camera.main.transform.TransformDirection(Vector3.right);
					worldRight.y = 0;
					worldRight.Normalize();

					Vector3 worldDirection = worldUp * movementDir.y + worldRight * movementDir.x;
					worldDirection.Normalize();
					worldDirection *= moveSpeed;
					Vector2 moveDir = new Vector2(worldDirection.x, worldDirection.z);
					SetDesiredMovementDirection(moveDir);

					// If there's no touch input, tank should look at desired move dir
					if (m_FiringTouch == null)
					{
						m_Shooting.SetLookDirection(moveDir);
					}

					if (m_MovementIndicator != null)
					{
						m_MovementIndicator.SetActive(true);
						float moveAngle = 90 - Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
						m_MovementIndicator.transform.rotation = Quaternion.AngleAxis(moveAngle, Vector3.up);
						m_MovementIndicator.transform.localScale = new Vector3(1, 1, Mathf.Lerp(0.5f, 1.0f, moveSpeed));
					}
				}
				else if (m_MovementIndicator != null)
				{
					m_MovementIndicator.SetActive(false);
				}

				// Pull center if beyond touchDistMaxSensitivity + touchDistCenterPull
				if (magnitude > touchPullThreshold)
				{
					Vector2 normalizedMovementDir = movementDir / magnitude;
					// Reposition center
					m_PadCenter = fingerpos - (normalizedMovementDir * touchPullThreshold);
				}

				return true;
			}
			else if (m_MovementIndicator != null)
			{
				m_MovementIndicator.SetActive(false);
			}

			return false;
		}

		private bool IsValidMovementTouch(Touch touch)
		{
			if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
			{
				return false;
			}

			if (m_MovementTouchId >= 0)
			{
				// Already have a movement touch
				return false;
			}

			// True if the touch is in the lower-left corner of the screen
			Vector2 normalizedTouch = Vector2.Scale(touch.position, new Vector2(1.0f / Screen.width, 1.0f / Screen.height));

			if (m_LeftyMode)
			{
				return (normalizedTouch.x >= 1 - m_HorizontalArea &&
				normalizedTouch.y <= m_VerticalArea);
			}
			else
			{
				return (normalizedTouch.x <= m_HorizontalArea &&
				normalizedTouch.y <= m_VerticalArea);
			}
		}

		private bool IsValidFireTouch(Touch touch)
		{
			if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
			{
				return false;
			}
	
			if (m_FiringTouchId >= 0)
			{
				// Already have a firing touch
				return false;
			}

			// True for any touch that
			return true;
		}
	}
}