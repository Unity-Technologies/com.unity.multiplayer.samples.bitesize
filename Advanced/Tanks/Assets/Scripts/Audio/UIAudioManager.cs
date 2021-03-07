using UnityEngine;
using System.Collections;
using Tanks.Utilities;
using UnityEngine.SceneManagement;

namespace Tanks.Audio
{
	[RequireComponent(typeof(AudioSource))]

	//This class provides a centralized means to play common UI audio such as button clicks and status prompts.
	public class UIAudioManager : PersistentSingleton<UIAudioManager>
	{
		//The sound to be used for button clicks by default.
		[SerializeField]
		protected AudioClip m_DefaultButtonSound;

		//The sound to be played at the start of a multiplayer round.
		[SerializeField]
		protected AudioClip m_RoundStartEffect;

		//The sound to be played on single player victory.
		[SerializeField]
		protected AudioClip m_VictoryEffect;

		//The sound to be played on single player failure.
		[SerializeField]
		protected AudioClip m_FailureEffect;

		//The sound to be played whenever currency is awarded.
		[SerializeField]
		protected AudioClip m_CoinEffect;

		private AudioSource m_ButtonSource;


		protected override void Awake()
		{
			base.Awake();

			m_ButtonSource = GetComponent<AudioSource>();
		}

		//Convenience function to play button clicks. If necessary, the default button click sound can be overridden by the caller.
		public void PlayButtonEffect(AudioClip overrideSound = null)
		{
			m_ButtonSource.Stop();

			if (overrideSound != null)
			{
				PlaySound(overrideSound);
			}
			else
			{
				PlaySound(m_DefaultButtonSound);
			}
		}
			
		//Convenience function to play the round start sound.
		public void PlayRoundStartSound()
		{
			m_ButtonSource.PlayOneShot(m_RoundStartEffect);
		}

		//Convenience function to play the victory sound.
		public void PlayVictorySound()
		{
			PlaySound(m_VictoryEffect);
		}

		//Convenience function to play the failure sound.
		public void PlayFailureSound()
		{
			PlaySound(m_FailureEffect);
		}

		//Convenience function to play the coin award sound.
		public void PlayCoinSound()
		{
			m_ButtonSource.PlayOneShot(m_CoinEffect);
		}

		//Internal sound-playing method.
		private void PlaySound(AudioClip sound)
		{
			m_ButtonSource.clip = sound;
			m_ButtonSource.Play();
		}
	}
}
