using UnityEngine;
using System.Collections;
using Tanks.TankControllers;
using Tanks.Data;

namespace Tanks.Pickups
{
	//This class allows special projectiles to be pickup up by players. It applies for all possible special projectiles.
	public class ProjectilePickup : PickupBase
	{
		//The number of special ammo shots contained in this pod.
		[SerializeField]
		protected int m_AmmoToAdd = 3;

		//The index of the projectile to be collected in the Special Projectile Library.
		[SerializeField]
		protected int m_ProjectileLibraryIndex;

		protected override void Awake()
		{
			base.Awake();

			//populate the pickup's name from the projectile library using the provided index.
			m_PickupName = SpecialProjectileLibrary.s_Instance.GetProjectileDataForIndex(m_ProjectileLibraryIndex).name;
		}

		protected override void OnPickupCollected(GameObject targetTank)
		{
			targetTank.transform.GetComponentInParent<TankShooting>().SetSpecialAmmo(m_ProjectileLibraryIndex,m_AmmoToAdd,m_PickupName);

			base.OnPickupCollected(targetTank);
		}
	}
}