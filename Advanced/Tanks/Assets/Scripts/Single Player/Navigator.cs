using UnityEngine;
using System.Collections;

namespace Tanks.Rules.SinglePlayer
{
	/// <summary>
	/// Navigation state - governs how the Navigator behaves
	/// </summary>
	public enum NavigationState
	{
		StartUp,
		Moving,
		Turning,
		Complete
	}

	/// <summary>
	/// Navigator - used to create NPCs that follow a path
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	public class Navigator : MonoBehaviour
	{
		//properties of navigation
		[SerializeField]
		protected float m_RotationSpeed = 0.5f, m_MoveSpeed = 3f, m_AngleTolerance = 0.99f, m_TurningMoveSpeedFactor = 0.8f;

		//This will effect how fast the navigator is moving during turning based on the dot product
		[SerializeField]
		protected bool m_UseDotProductInTurnMovement = true;

		//This is what the navigator will move towards/look at
		private Transform m_Target;

		//Current navigation state
		private NavigationState m_State = NavigationState.StartUp;

		private float m_DotValue = 0f;

		private Rigidbody m_MovingRigidBody;

		private void Awake()
		{
			m_MovingRigidBody = GetComponent<Rigidbody>();
		}

		private void Update()
		{
			if (m_State == NavigationState.StartUp)
			{
				Initialise();
			}
		}

		/// <summary>
		/// Used for visualising 
		/// </summary>
		private void OnDrawGizmos()
		{
			if (m_Target == null)
			{
				return; 
			}

			Gizmos.color = Color.red;
			Gizmos.DrawLine(transform.position, m_Target.position);
		}

		/// <summary>
		/// Navigation logic
		/// </summary>
		protected void Navigate()
		{
			if (m_State == NavigationState.Moving)
			{
				Turn();
				Move();
			}
			else if (m_State == NavigationState.Turning)
			{
				Turn();

				if (m_DotValue > 0)
				{
					float turningMovementSpeed = m_UseDotProductInTurnMovement ? m_DotValue * m_TurningMoveSpeedFactor * m_MoveSpeed : m_TurningMoveSpeedFactor * m_MoveSpeed;
					Move(turningMovementSpeed);
				}
			}
		}

		private void FixedUpdate()
		{
			Navigate();
		}

		/// <summary>
		/// Initialise the navigator
		/// </summary>
		private void Initialise()
		{
			if (m_Target != null)
			{
				m_State = NavigationState.Turning;
				return;
			}
            
			int length = Waypoint.s_Waypoints.Count;
			Transform newTarget = null;

			float minimumSquareDistance = Mathf.Infinity;
    
			for (int i = 0; i < length; i++)
			{
				Transform waypointTransform = Waypoint.s_Waypoints[i].transform;
				Vector3 displacement = waypointTransform.position - transform.position;
				float sqrDistance = displacement.sqrMagnitude;
				Debug.LogFormat("name = {0} distance = {1}", Waypoint.s_Waypoints[i].name, sqrDistance);
				if (minimumSquareDistance > sqrDistance)
				{
					minimumSquareDistance = sqrDistance;
					newTarget = waypointTransform;
				}
			}
            
			m_Target = newTarget;
		}

		/// <summary>
		/// Turn at specified speed.
		/// </summary>
		/// <param name="speed">Speed.</param>
		protected void Turn(float speed)
		{
            
			Vector3 targetDir = m_Target.position - transform.position;
			float step = speed * Time.fixedDeltaTime;
			Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, step, 0.0F);
			Debug.DrawRay(transform.position, newDir, Color.green);
			m_MovingRigidBody.MoveRotation(Quaternion.LookRotation(newDir));

			m_DotValue = Vector3.Dot(targetDir.normalized, newDir.normalized);
			if (m_DotValue > m_AngleTolerance)
			{
				m_State = NavigationState.Moving;
			}
		}

		/// <summary>
		/// Turns at the precalculated rotation speed
		/// </summary>
		protected void Turn()
		{
			Turn(m_RotationSpeed);
		}

		/// <summary>
		/// Move at specified speed.
		/// </summary>
		/// <param name="speed">Speed.</param>
		protected void Move(float speed)
		{
			Debug.DrawRay(transform.position, transform.forward.normalized * 20, Color.green);
			m_MovingRigidBody.MovePosition(transform.position + transform.forward.normalized * speed * Time.fixedDeltaTime);
		}

		/// <summary>
		/// Moves at the precalculated move speed
		/// </summary>
		protected void Move()
		{
			Move(m_MoveSpeed);
		}

		/// <summary>
		/// Sets the target.
		/// </summary>
		/// <param name="target">Target.</param>
		public void SetTarget(Transform target)
		{
			if (target == null)
			{
				SetComplete();
				return;
			}

			this.m_Target = target;
			m_State = NavigationState.Turning;
		}

		/// <summary>
		/// Once the navigator has reached it's end point
		/// </summary>
		public void SetComplete()
		{
			m_State = NavigationState.Complete;
		}
	}
}
