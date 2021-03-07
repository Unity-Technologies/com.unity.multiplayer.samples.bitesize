using UnityEngine;
using System.Collections;
using Tanks.Shells;

namespace Tanks.Shells
{
	/// <summary>
	/// static class for helping with firing
	/// </summary>
	public static class FiringLogic
	{
		public static float s_InitialVelocity;

		public static Vector3 CalculateFireVector(Shell shellToFire, Vector3 targetFirePosition, Vector3 firePosition, float launchAngle)
		{
			Vector3 target = targetFirePosition;
			target.y = firePosition.y;
			Vector3 toTarget = target - firePosition;
			float targetDistance = toTarget.magnitude;
			float shootingAngle = launchAngle;
			float grav = Mathf.Abs(Physics.gravity.y);
			grav *= shellToFire != null ? shellToFire.speedModifier : 1;
			float relativeY = firePosition.y - targetFirePosition.y;

			float theta = Mathf.Deg2Rad * shootingAngle;
			float cosTheta = Mathf.Cos(theta);
			float num = targetDistance * Mathf.Sqrt(grav) * Mathf.Sqrt(1 / cosTheta);
			float denom = Mathf.Sqrt(2 * targetDistance * Mathf.Sin(theta) + 2 * relativeY * cosTheta);
			float v = num / denom;
			s_InitialVelocity = v;

			Vector3 aimVector = toTarget / targetDistance;
			aimVector.y = 0;
			Vector3 rotAxis = Vector3.Cross(aimVector, Vector3.up);
			Quaternion rotation = Quaternion.AngleAxis(shootingAngle, rotAxis);
			aimVector = rotation * aimVector.normalized;

			return aimVector * v;
		}

	}
}