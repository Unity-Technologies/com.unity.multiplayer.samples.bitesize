using UnityEngine;
using System.Collections;

namespace Tanks.TankControllers
{
	//This interface allows any classes that implement it to be detected by, and process damage from, explosions.
	public interface IDamageObject
	{
		bool isAlive { get; }

		Vector3 GetPosition();

		void Damage(float damage);

		void SetDamagedBy(int playerNumber, string explosionId);
	}
}