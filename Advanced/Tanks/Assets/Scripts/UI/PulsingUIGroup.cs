using UnityEngine;
using System.Collections;

namespace Tanks.UI
{
	/// <summary>
	/// Pulsing user interface group.
	/// </summary>
	[RequireComponent(typeof(FadingGroup))]
	public class PulsingUIGroup : MonoBehaviour
	{
		//Fade values
		[SerializeField]
		protected float m_FadeInTime = 1f, m_FadeOutTime = 1f, m_FadeOutValue = 0.5f;

		//Awake fading behaviour
		[SerializeField]
		protected Fade m_FadeOnAwake = Fade.None;

		//The fading group
		protected FadingGroup m_FadingGroup;

		protected virtual void Awake()
		{
			if (m_FadeOnAwake != Fade.None)
			{
				StartPulse(m_FadeOnAwake);
			}
		}

		/// <summary>
		/// Starts the pulse.
		/// </summary>
		/// <param name="fade">Fade.</param>
		public void StartPulse(Fade fade)
		{
			gameObject.SetActive(true);
			if (fade == Fade.In)
			{
				FadeIn();
			}
			else
			{
				FadeOut();
			}
		}

		/// <summary>
		/// Stops the pulse.
		/// </summary>
		public void StopPulse()
		{
			m_FadingGroup.StopFade(true);
		}

		protected void FadeIn()
		{
			LazyLoad();
			m_FadingGroup.StartFade(Fade.In, m_FadeInTime, FadeOut, false);
		}

		protected void FadeOut()
		{
			LazyLoad();
			m_FadingGroup.FadeOutToValue(m_FadeOutTime, m_FadeOutValue, FadeIn);
		}

		/// <summary>
		/// Lazy loads the fading group
		/// </summary>
		protected void LazyLoad()
		{
			if (m_FadingGroup != null)
			{
				return;
			}

			m_FadingGroup = GetComponent<FadingGroup>();
		}
	}
}