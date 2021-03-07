using UnityEngine;

#if UNITY_WSA_10_0 && NETFX_CORE
using Windows.System.Profile;
#endif

namespace Tanks.Utilities
{
	public static class MobileUtilities
	{
		public static bool IsOnMobile()
		{
#if UNITY_ANDROID || UNITY_IOS
			// True if not in editor
			return !Application.isEditor;
#elif UNITY_WSA_10_0 && NETFX_CORE
			// TODO: Actually test this
			// True if device ID returns mobile
			return AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile";
#else
			return false;
			#endif
		}
	}
}