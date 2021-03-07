using UnityEngine;

namespace Tanks.Managers
{
	public class QualitySettingsManager : MonoBehaviour
	{
		#region Fields
		[SerializeField]
		protected int m_LowDeviceMaxRam = 512;
		[SerializeField]
		protected int m_LowDeviceMaxHeight = 768;
		#endregion


		#region Methods
		/// <summary>
		/// Set half mips for devices with small screens or low ram
		/// </summary>
		protected virtual void Awake()
		{
			int deviceRam = SystemInfo.systemMemorySize;

			Debug.LogFormat("System memory: {0}\nGraphics memory: {1}", SystemInfo.systemMemorySize, SystemInfo.graphicsMemorySize);
			if (Tanks.Utilities.MobileUtilities.IsOnMobile() && 
			    (deviceRam <= m_LowDeviceMaxRam ||
			     Screen.height <= m_LowDeviceMaxHeight))
			{
				QualitySettings.masterTextureLimit = 1;
				Debug.Log("Setting max mip level to 1");
			}
		}
		#endregion
	}
}