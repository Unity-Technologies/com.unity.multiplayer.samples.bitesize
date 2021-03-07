using UnityEngine;
using MLAPI;
using System;

namespace Tanks.TankControllers
{
	/// <summary>
	/// Tank input module - base class for all input systems
	/// </summary>
	[RequireComponent(typeof(TankShooting))]
	[RequireComponent(typeof(TankMovement))]
	public abstract class TankInputModule : NetworkBehaviour
	{
		protected TankShooting m_Shooting;
		protected TankMovement m_Movement;
		protected int m_GroundLayerMask;
		protected Plane m_FloorPlane;

		/// <summary>
		/// Occurs when input method changed.
		/// </summary>
		public static event Action<bool> s_InputMethodChanged;

		public static TankInputModule s_CurrentInputModule
		{
			get;
			private set;
		}

		public bool isActiveModule
		{
			get { return s_CurrentInputModule == this; }
		}

		protected virtual void Awake()
		{
			OnBecomesInactive();
			m_Shooting = GetComponent<TankShooting>();
			m_Movement = GetComponent<TankMovement>();
			m_FloorPlane = new Plane(Vector3.up, 0);
			m_GroundLayerMask = LayerMask.GetMask("Ground");
		}

		protected virtual void Update()
		{
			if (!IsOwner)
			{
				return;
			}

			bool isActive = DoMovementInput();
			isActive |= DoFiringInput();

			if (isActive && !isActiveModule)
			{
				if (s_CurrentInputModule != null)
				{
					s_CurrentInputModule.OnBecomesInactive();
				}
				s_CurrentInputModule = this;
				OnBecomesActive();
			}
		}

		protected virtual void OnBecomesActive()
		{
		}

		protected virtual void OnBecomesInactive()
		{
		}

		protected abstract bool DoMovementInput();

		protected abstract bool DoFiringInput();

		protected void SetDesiredMovementDirection(Vector2 moveDir)
		{
			m_Movement.SetDesiredMovementDirection(moveDir);
		}

		protected void SetDesiredFirePosition(Vector3 target)
		{
			m_Shooting.SetDesiredFirePosition(target);
		}

		protected void SetFireIsHeld(bool fireHeld)
		{
			m_Shooting.SetFireIsHeld(fireHeld);
		}

		protected void OnDisable()
		{
			SetDesiredMovementDirection(Vector2.zero);
			SetFireIsHeld(false);
		}

		protected void OnInputMethodChanged(bool isTouch)
		{
			if (s_InputMethodChanged != null)
			{
				s_InputMethodChanged(isTouch);
			}
		}
	}
}