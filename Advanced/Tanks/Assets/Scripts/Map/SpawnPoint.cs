using UnityEngine;
using System.Collections;
using Tanks.TankControllers;

namespace Tanks.Map
{
	/// <summary>
	/// Spawn point - has a collider to check if any player is in the zone
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class SpawnPoint : MonoBehaviour
	{
		[SerializeField]
		protected Transform m_SpawnPointTransform;

		public Transform spawnPointTransform
		{
			get
			{
				if (m_SpawnPointTransform == null)
				{
					m_SpawnPointTransform = transform;
				}
				return m_SpawnPointTransform;
			}
		}

		//if multiple respawns occurs simultaneously then there will be no firing on trigger functionality to ensure zones are marked as occupied, hence the need for a dirty variable
		private bool m_IsDirty = false;

		//number of tanks currently within the bounds of the spawn point
		private int m_NumberOfTanksInZone = 0;

		//Is the zone empty and not marked as dirty
		public bool isEmptyZone
		{
			get { return !m_IsDirty && m_NumberOfTanksInZone == 0; }
		}

		/// <summary>
		/// Raises the trigger enter event - if the collider is a tank then increase the number of tanks in zone
		/// </summary>
		/// <param name="c">C.</param>
		private void OnTriggerEnter(Collider c)
		{
			TankHealth tankHealth = c.GetComponentInParent<TankHealth>();
            
			if (tankHealth != null)
			{
				m_NumberOfTanksInZone++;
				tankHealth.currentSpawnPoint = this;
			}
		}

		/// <summary>
		/// Raises the trigger exit event - if the collider is a tank then decrease the number of tanks in zone
		/// </summary>
		/// <param name="c">C.</param>
		private void OnTriggerExit(Collider c)
		{
			TankHealth tankHealth = c.GetComponentInParent<TankHealth>();
            
			if (tankHealth != null)
			{
				Decrement();
				tankHealth.NullifySpawnPoint(this);
			}
		}

		/// <summary>
		/// Safely decrement the number of tanks in zone and set isDirty to false
		/// </summary>
		public void Decrement()
		{
			m_NumberOfTanksInZone--;
			if (m_NumberOfTanksInZone < 0)
			{
				m_NumberOfTanksInZone = 0;
			}

			m_IsDirty = false;
		}

		/// <summary>
		/// Used to set the spawn point to dirty to prevent simultaneous spawns from occurring at the same point 
		/// </summary>
		public void SetDirty()
		{
			m_IsDirty = true;
		}

		/// <summary>
		/// Resets/cleans up the spawn point
		/// </summary>
		public void Cleanup()
		{
			m_IsDirty = false;
			m_NumberOfTanksInZone = 0;
		}
	}
}