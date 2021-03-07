using UnityEngine;

namespace Tanks.Shells
{
	[RequireComponent(typeof(Rigidbody))]
	//This class allows objects to be physically affected by explosions spawned using the ExplosionManager.
	public class PhysicsAffected : MonoBehaviour
	{
		[SerializeField]
		private float m_UpwardsModifier;
		private Rigidbody m_Rigidbody;

		private void Awake()
		{
			m_Rigidbody = GetComponent<Rigidbody>();
		}
		
		//ApplyForce is called by the ExplosionManager if this object is within an explosion's bounds.
		public void ApplyForce(float force, Vector3 position, float radius)
		{
			m_Rigidbody.AddExplosionForce(force, position, radius, m_UpwardsModifier);
		}
	}
}