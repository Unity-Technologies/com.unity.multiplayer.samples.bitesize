using UnityEngine;

namespace Tanks
{
	/// <summary>
	/// Some useful Vector2 extension methods
	/// </summary>
	public static class Vector2Extensions
	{
		public static Vector2 Rotate(this Vector2 v, float degrees)
		{
			float rads = degrees * Mathf.Deg2Rad;

			float cos = Mathf.Cos(rads);
			float sin = Mathf.Sin(rads);

			float vx = v.x;
			float vy = v.y;

			return new Vector2(cos * vx - sin * vy, sin * vx + cos * vy);
		}
	}
}