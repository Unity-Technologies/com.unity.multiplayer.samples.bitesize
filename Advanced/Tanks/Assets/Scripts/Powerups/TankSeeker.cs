using UnityEngine;
using MLAPI;

namespace Tanks.Pickups
{
	//This class can be attached to any given network-linked rigidbody to have it automatically attract itself towards the nearest tank.
	//Its primary purpose is to allow drop pods to be attracted to nearby tanks.
	public class TankSeeker : NetworkBehaviour 
	{
		//The minimum speed at which this object should move.
		[SerializeField]
		protected float m_MinMovementRate = 2f;

		//The maximum speed at which this object should move.
		[SerializeField]
		protected float m_MaxMovementRate = 10f;

		//The radius under which the attractive force will start. A square distance is also calculated for efficiency.
		[SerializeField]
		protected float m_MaxAttractionRadius = 6f;

		//Internal variable to store total range of speed values.
		private float m_MovementRateDifference;
		private float m_SqrMaxAttractionRadius = 6f;

		//The destination point to which the object will move.
		private Vector3 m_LerpDestination;

		//Internal reference to the object's rigidbody.
		private Rigidbody m_RigidBody;

		//Internal flag to determine whether attraction should be active.
		private bool m_CanBeAttracted = false;


		//Set whether attraction is active. Used to ensure that attraction doesn't occur the second a pickup is spawned.
		public void SetAttracted(bool isAttracted)
		{
			m_CanBeAttracted = isAttracted;
		}

		protected virtual void Start()
		{
			if (!IsServer)
				return;

			m_RigidBody = GetComponent<Rigidbody>();
			m_SqrMaxAttractionRadius = Mathf.Pow(m_MaxAttractionRadius, 2f);
			m_MovementRateDifference = m_MaxMovementRate - m_MinMovementRate;
		}

		protected virtual void Update()
		{
			if (!IsServer)
				return;

			if(!m_CanBeAttracted)
			{
				return;
			}
				
			Vector3 displacement = Vector3.zero;

			//Iterate through all active tanks and determine their relative distances.
			for(int i = 0; i < GameManager.s_Tanks.Count; i++)
			{
				Vector3 tankVector = (GameManager.s_Tanks[i].transform.position - m_RigidBody.position);

				float tankSqrDistance = tankVector.sqrMagnitude;

				//If this tank is within the attraction radius of the powerup, have it add an attractive force in its direction proportional to its distance.
				if(tankSqrDistance <= m_SqrMaxAttractionRadius)
				{
					float displacementForce = m_MinMovementRate + (m_MovementRateDifference * (1f - (tankSqrDistance/m_SqrMaxAttractionRadius)));

					displacement += (tankVector.normalized * displacementForce);
				}
			}

			//Move the object in the aggregate vector after all attracting tanks are taken into account.
			m_RigidBody.MovePosition(m_RigidBody.position + displacement * Time.deltaTime);
		}
	}
}