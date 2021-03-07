using UnityEngine;
using Tanks.TankControllers;
using Tanks.Utilities;

namespace Tanks.CameraControl
{
	public class CameraFollow : Singleton<CameraFollow>
	{
		private Transform m_TankToFollowTransform = null;
		private TankMovement m_TankToFollowMovement = null;

		//Distance ahead of the tank to look and the damping time
		[SerializeField]
		protected float m_ForwardThreshold = 5f, m_DampTime = 0.2f;

		private Vector3 m_MoveVelocity;

		// Use this for initialization
		private void Start()
		{
			LazyLoadTankToFollow();
		}

		// Update is called once per frame
		private void Update()
		{
			FollowTank();
		}

		/// <summary>
		/// Lazy loads the tank to follow
		/// </summary>
		private void LazyLoadTankToFollow()
		{
			if (m_TankToFollowTransform != null)
			{
				return;
			}

			var tanksList = GameManager.s_Tanks;
			for (int i = 0; i < tanksList.Count; i++)
			{
				TankManager tank = tanksList[i];
				if (tank != null && tank.IsOwner)
				{
					m_TankToFollowTransform = tank.transform;
					m_TankToFollowMovement = tank.movement;
				}
			}
		}

		/// <summary>
		/// Follows the tank.
		/// </summary>
		private void FollowTank()
		{
			LazyLoadTankToFollow();

			//Null checks
			if (m_TankToFollowTransform == null || m_TankToFollowMovement == null)
			{
				return;
			}
			
			//Calculates the target position
			Vector3 tankPosition = m_TankToFollowTransform.position;
			Vector3 targetPosition = new Vector3(tankPosition.x, transform.position.y, tankPosition.z);
			targetPosition = targetPosition + m_ForwardThreshold * m_TankToFollowTransform.forward * (float)m_TankToFollowMovement.currentMovementMode;

			//Smooth damps to that position
			transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref m_MoveVelocity, m_DampTime, float.PositiveInfinity, Time.unscaledDeltaTime);
		}
	}
}
