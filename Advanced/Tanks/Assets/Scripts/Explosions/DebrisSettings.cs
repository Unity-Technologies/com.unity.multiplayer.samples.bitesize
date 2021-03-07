using UnityEngine;

namespace Tanks.Explosions
{
	/// <summary>
	/// Debris settings configuration scriptable object
	/// </summary>
	[CreateAssetMenu(fileName = "Debris", menuName = "Debris Definition", order = 1)]
	public class DebrisSettings : ScriptableObject
	{
		public Rigidbody prefab;
		public int minSpawns;
		public int maxSpawns;
		public float minForce;
		public float maxForce;
		public float maxUpAngle;
	}
}