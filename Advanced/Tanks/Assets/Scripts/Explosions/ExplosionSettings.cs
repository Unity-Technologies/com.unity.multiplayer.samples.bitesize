using UnityEngine;

namespace Tanks.Explosions
{
	/// <summary>
	/// Enum representing the type of explosion
	/// </summary>
	public enum ExplosionClass
	{
		Large,
		Small,
		ExtraLarge,
		TankExplosion,
		TurretExplosion,
		BounceExplosion,
		ClusterExplosion,
		FiringExplosion
	}

	/// <summary>
	/// Explosion settings configuration scriptable object
	/// </summary>
	[CreateAssetMenu(fileName = "Explosion", menuName = "Explosion Definition", order = 1)]
	public class ExplosionSettings : ScriptableObject
	{
		public string id;
		public ExplosionClass explosionClass;
		public float explosionRadius;
		public float damage;
		public float physicsRadius;
		public float physicsForce;
		[Range(0, 1)]
		public float shakeMagnitude;
	}
}