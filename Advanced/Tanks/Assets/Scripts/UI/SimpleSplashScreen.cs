using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tanks.UI
{
	/// <summary>
	/// Basic splash screen that fades in the logo and pulses start text
	/// </summary>
	public class SimpleSplashScreen : MonoBehaviour
	{
		//The scene to load
		[SerializeField]
		protected string m_SceneName = "LobbyScene";
		
		//The fading in time of the logo
		[SerializeField]
		protected float m_FadeTime = 2f;
	
		//The fading group component on the logo
		[SerializeField]
		protected FadingGroup m_FadingGroup;

		//The pulsing label
		[SerializeField]
		protected PulsingUIGroup m_PulsingGroup;

		protected virtual void Start()
		{
			//Fades the logo then starts pulsing the text
			m_FadingGroup.StartFade(Fade.In, m_FadeTime, StartPulsingText);
		}

		protected virtual void Update()
		{
			//Go to menu
			if (Input.anyKeyDown)
			{
				ProgressToNextScene();
			}
		}

		//Pulse the "Press Any Key" text
		protected void StartPulsingText()
		{
			m_PulsingGroup.StartPulse(Fade.In);
		}

		//Helper for going to menu
		private void ProgressToNextScene()
		{
			SceneManager.LoadScene(m_SceneName);
		}
	}
}