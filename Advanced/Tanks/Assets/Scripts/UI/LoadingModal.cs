using UnityEngine;

namespace Tanks.UI
{
	/// <summary>
	/// Loading modal - used to handle loading fades
	/// </summary>
	[RequireComponent(typeof(FadingGroup))]
	public class LoadingModal : Modal
	{
		private FadingGroup m_Fader;

		[SerializeField]
		protected float m_FadeTime = 0.5f;

		public static LoadingModal s_Instance
		{
			get;
			private set;
		}

		public bool readyToTransition
		{
			get
			{
				return m_Fader.currentFade == Fade.None && gameObject.activeSelf;
			}
		}

		/// <summary>
		/// Getter for Fader - used in game manager
		/// </summary>
		/// <value>The fader.</value>
		public FadingGroup fader
		{
			get
			{
				return m_Fader;
			}
		}

		/// <summary>
		/// Wraps fade in on FadingGroup
		/// </summary>
		public void FadeIn()
		{
			Show();
			m_Fader.StartFade(Fade.In, m_FadeTime);
		}

		/// <summary>
		/// Wraps fade out on FadingGroup
		/// </summary>
		public void FadeOut()
		{
			Show();
			m_Fader.StartFade(Fade.Out, m_FadeTime, CloseModal);
		}

		protected virtual void Awake()
		{
			if (s_Instance != null)
			{
				Debug.Log("<color=lightblue>Trying to create a second instance of LoadingModal</color");
				Destroy(gameObject);
			}
			else
			{
				s_Instance = this;
			}

			m_Fader = GetComponent<FadingGroup>();
		}

		protected virtual void OnDestroy()
		{
			if (s_Instance == this)
			{
				s_Instance = null;
			}
		}
	}
}