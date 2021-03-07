using UnityEngine;

namespace Tanks.Utilities
{
	//Math utilities
	public static class MathUtilities
	{
		public static Vector3 CatmullRom(Vector3 previous, Vector3 start, Vector3 end, Vector3 next, 
		                                 float normalizedProgress)
		{
			// r = 0.5
			float progressSquared = normalizedProgress * normalizedProgress;
			float progressCubed = progressSquared * normalizedProgress;

			Vector3 result = previous * (-0.5f * progressCubed + progressSquared + -0.5f * normalizedProgress);
			result += start * (1.5f * progressCubed + -2.5f * progressSquared + 1.0f);
			result += end * (-1.5f * progressCubed + 2.0f * progressSquared + 0.5f * normalizedProgress);
			result += next * (0.5f * progressCubed + -0.5f * progressSquared);

			return result;
		}

		public static Vector3 CatmullRomDerivative(Vector3 previous, Vector3 start, Vector3 end, Vector3 next, 
		                                           float normalizedProgress)
		{
			float progressSquared = normalizedProgress * normalizedProgress;

			Vector3 result = previous * (-1.5f * progressSquared + 2.0f * normalizedProgress + -0.5f);
			result += start * (4.5f * progressSquared + -5.0f * normalizedProgress);
			result += end * (-4.5f * progressSquared + 4.0f * normalizedProgress + 0.5f);
			result += next * (1.5f * progressSquared - normalizedProgress);
	 		
			return result;
		}
	}
}