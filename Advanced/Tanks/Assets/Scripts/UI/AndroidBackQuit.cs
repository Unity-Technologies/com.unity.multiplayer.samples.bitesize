using UnityEngine;

namespace Tanks.UI
{
	/// <summary>
	/// Quit on Android when main menu
	/// </summary>
	public class AndroidBackQuit : BackButton
	{
		protected override void OnBackPressed()
		{
			#if UNITY_ANDROID
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				Application.Quit();
			}
			#endif
		}
	}
}