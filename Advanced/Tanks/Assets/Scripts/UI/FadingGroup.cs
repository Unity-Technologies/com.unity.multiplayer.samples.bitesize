using UnityEngine;
using System;

namespace Tanks.UI
{
	public enum Fade
	{
		None,
		In,
		Out
	}

	[RequireComponent(typeof(CanvasGroup))]
	/// <summary>
	/// Base class to control dynamic fading of UI elements.
	/// </summary>
	public class FadingGroup : MonoBehaviour
	{
		//Internal reference to the canvasgroup that this class forces into existence.
		private CanvasGroup m_CanvasGroup;

		//Current fade state.
		private Fade m_Fade = Fade.None;

		//Working variables for the fade.
		private float m_FadeTime = 0f, m_FadeOutValue = 0f;

		//Delegate to fire when the fade is complete.
		private Action m_FinishFade;

		//Field to return current fade state.
		public Fade currentFade
		{
			get { return m_Fade; }
		}

		//Field to return alpha ratio to fade this tick.
		private float fadeStep
		{
			get
			{
				if (m_FadeTime == 0f)
				{
					return 1f;
				}

				return Time.unscaledDeltaTime / m_FadeTime; 
			}
		}

		private void Awake()
		{
			m_CanvasGroup = GetComponent<CanvasGroup>();
		}

		private void Update()
		{
			if (m_Fade == Fade.Out)
			{		
				FadeOut();
			}
			else if (m_Fade == Fade.In)
			{
				FadeIn();
			}
		}

		private void FadeOut()
		{
			m_CanvasGroup.alpha -= fadeStep;
			if (m_CanvasGroup.alpha <= m_FadeOutValue + Mathf.Epsilon)
			{	
				m_CanvasGroup.alpha = m_FadeOutValue;
				if (m_FadeOutValue == 0)
				{
					gameObject.SetActive(false);
				}

				EndFade();
			}
		}

		private void FadeIn()
		{
			m_CanvasGroup.alpha += fadeStep;
			if (m_CanvasGroup.alpha >= 1 - Mathf.Epsilon)
			{	
				m_CanvasGroup.alpha = 1;
				EndFade();
			}
		}

		private void EndFade()
		{
			m_Fade = Fade.None;
			FireEvent();
		}

		private void FireEvent()
		{
			if (m_FinishFade != null)
			{
				m_FinishFade.Invoke();
			}
		}

		/// <summary>
		/// Starts the fading of a panel.
		/// </summary>
		/// <param name="fade">The fade type to use.</param>
		/// <param name="fadeTime">Fade time.</param>
		/// <param name="finishFade">Delegate to fire once fade is complete.</param>
		/// <param name="reactivate">Whether to reactivate this gameobject for the purposes of the fade.</param>
		public void StartFade(Fade fade, float fadeTime, Action finishFade = null, bool reactivate = true)
		{
			this.m_Fade = fade;
			this.m_FadeTime = fadeTime;
			this.m_FinishFade = finishFade;
			this.m_FadeOutValue = 0f;
			if (reactivate)
			{
				gameObject.SetActive(true);
				m_CanvasGroup.alpha = (fade == Fade.In ? 0f : 1f);
			}
		}

		/// <summary>
		/// Fades the panel to a given value.
		/// </summary>
		/// <param name="fadeTime">Fade time.</param>
		/// <param name="fadeOutValue">Value to fade to.</param>
		/// <param name="finishFade">Delegate to fire once fade is complete.</param>
		public void FadeOutToValue(float fadeTime, float fadeOutValue, Action finishFade = null)
		{
			this.m_Fade = Fade.Out;
			this.m_FadeTime = fadeTime;
			this.m_FadeOutValue = fadeOutValue;
			this.m_FinishFade = finishFade;
		}

		/// <summary>
		/// Starts a fade, and fires the provided event if the gameObject is disabled.
		/// </summary>
		/// <param name="fade">Fade type to use.</param>
		/// <param name="fadeTime">Fade time.</param>
		/// <param name="finishFade">Delegate to fire if object is disabled.</param>
		public void StartFadeOrFireEvent(Fade fade, float fadeTime, Action finishFade = null)
		{
			StartFade(fade, fadeTime, finishFade, false);
			if (!gameObject.activeInHierarchy)
			{
				FireEvent();
			}
		}

		/// <summary>
		/// Stops the fade, snapping the alpha and activating or deactivating the gameObject.
		/// </summary>
		/// <param name="setVisible">Whether the panel should be visible or invisible on stop.</param>
		public void StopFade(bool setVisible)
		{	
			m_Fade = Fade.None;
			gameObject.SetActive(setVisible);
			if (setVisible)
			{
				m_CanvasGroup.alpha = 1f;
			}
			else
			{
				m_CanvasGroup.alpha = 0f;
			}
		}
	}
}