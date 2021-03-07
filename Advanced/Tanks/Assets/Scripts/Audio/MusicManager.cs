using UnityEngine;
using UnityEngine.SceneManagement;
using Tanks.Utilities;

namespace Tanks.Audio
{
	[RequireComponent(typeof(AudioSource))]
	//This class manages the playing of all music in the game. It assumes that music swaps when scenes are transitioned.
	public class MusicManager : PersistentSingleton<MusicManager>
	{
		//The menu music track
		[SerializeField]
		protected AudioClip m_MenuMusic;

		//Field to represent the dedicated audiosource for this manager.
		private AudioSource m_MusicSource;

		public AudioSource musicSource
		{
			get { return m_MusicSource; }
		}

		//Property to allow the start of music to be delayed.
		[SerializeField]
		protected float m_StartDelay = 0.5f;

		//Property to set the rate that music fades in. If 0, music starts at full volume.
		[SerializeField]
		protected float m_FadeRate = 2f;

		//Audiosource volume on instantiation, in case we want to tweak the volume directly in editor.
		private float m_OriginalVolume;

		//Proportion of fading.
		private float m_FadeLevel = 1f;

		//Get references to the local audiosource on start, subscribe to the scene manager change event, and start the menu music (since Start will occur in the main menu).
		protected void Start()
		{
			m_MusicSource = GetComponent<AudioSource>();
			SceneManager.activeSceneChanged += OnSceneChanged;

			m_OriginalVolume = m_MusicSource.volume;

			PlayMusic(m_MenuMusic);
		}

		//Volume fade-in logic happens here, assuming the relevant parameters are set.
		protected void Update()
		{
			if (m_FadeLevel < 1f && m_FadeRate > 0f)
			{
				m_FadeLevel = Mathf.Lerp(m_FadeLevel, 1f, Time.deltaTime * m_FadeRate);

				if (m_FadeLevel >= 0.99f)
				{
					m_FadeLevel = 1f;
				}

				m_MusicSource.volume = m_OriginalVolume * m_FadeLevel;
			}
		}

		//This method is subscribed to the activeSceneChanged event, and will fire whenever the scene changes.
		private void OnSceneChanged(Scene scene1, Scene newScene)
		{
			if (m_MusicSource != null)
			{
				// Make sure to reset pitch
				m_MusicSource.pitch = 1;
			}

			//If we're transitioning to the menu scene, play the menu music (if it is not already playing). Otherwise pull and autoplay the current level's music.
			if (newScene.name == "LobbyScene")
			{
				if (m_MusicSource.clip != m_MenuMusic)
				{
					PlayMusic(m_MenuMusic, true);
				}
			}
			else
			{
				PlayMusic(GameSettings.s_Instance.map.levelMusic, true);
			}
		}

		public void StopMusic()
		{
			m_MusicSource.Stop();
		}

		public void PlayCurrentMusic()
		{
			m_MusicSource.Play();
		}

		private void PlayMusic(AudioClip music, bool fadeIn = false, bool loop = true)
		{
			m_MusicSource.Stop();

			m_MusicSource.loop = loop;
			m_MusicSource.clip = music;
			m_MusicSource.PlayDelayed(m_StartDelay);

			if (fadeIn)
			{
				m_FadeLevel = -m_StartDelay;
			}
		}

		//Unsubscribe from the SceneManager on destruction.
		protected override void OnDestroy()
		{
			SceneManager.activeSceneChanged -= OnSceneChanged;
			base.OnDestroy();
		}
	}
}
