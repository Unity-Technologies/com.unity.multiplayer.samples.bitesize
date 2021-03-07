using UnityEngine;
using System.Collections;


namespace Tanks.CameraControl
{
	/// <summary>
	/// This class allows us to override whatever camera rig rotations have been saved in scene for ease of mass-tweaking.
	/// </summary>
	public class CameraRigRotationInit : MonoBehaviour
	{
		public Vector3 startEulerRotation;

		void Awake()
		{
			transform.eulerAngles = startEulerRotation;
		}
	}
}
