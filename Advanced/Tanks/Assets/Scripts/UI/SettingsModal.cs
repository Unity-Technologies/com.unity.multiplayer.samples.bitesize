using UnityEngine;
using UnityEngine.UI;
using Tanks.Data;
using UnityEngine.Audio;

namespace Tanks.UI
{
	/// <summary>
	/// Modal that handles the Settings
	/// </summary>
	public class SettingsModal : Modal
	{
		//Game objects that are only enabled for touch devices
		[SerializeField]
		protected GameObject[] m_TouchDevicesOnly;
			
		//Game objects that are only enabled for Everyplay compatible devices
		[SerializeField]
		protected GameObject[] m_EveryplayCompatibleDevicesOnly;

		//Reference to UI elements
		[SerializeField]
		protected Slider m_Master, m_Music, m_Sfx, m_Thumbstick;
		[SerializeField]
		protected Toggle m_LeftyMode;
		[SerializeField]
		protected Toggle m_Everyplay;

		//Reference to the mixer for demonstrating volume change
		[SerializeField]
		protected AudioMixer m_AudioMixer;
		//Names of parameters on the audio mixer
		[SerializeField]
		protected string m_MusicVolume = "MusicVolume", m_SfxVolume = "SFXVolume", m_MasterVolume = "MasterVolume";

		//Reference to persistence framework
		private PlayerDataManager m_PlayerDataManager;

		//Caching the volumes
		private float m_InitialMasterVolume, m_InitialMusicVolume, m_InitialSfxVolume;

		//Save changes on Accept and close the modal
		public void AcceptNewSettings()
		{
			m_PlayerDataManager.isLeftyMode = m_LeftyMode.isOn;
			m_PlayerDataManager.everyplayEnabled = m_Everyplay.isOn;
			m_PlayerDataManager.thumbstickSize = m_Thumbstick.value;
			m_PlayerDataManager.musicVolume = 40f * Mathf.Log10(m_Music.value);
			m_PlayerDataManager.sfxVolume = 40f * Mathf.Log10(m_Sfx.value);
			m_PlayerDataManager.masterVolume = 40f * Mathf.Log10(m_Master.value);
			m_PlayerDataManager.SaveData();
			CloseModal();
		}

		//Close the modal without saving
		public void OnBackClick()
		{
			CloseModal();
		}

		//Adjust the mixer when the slider volumes have changed
		public void OnVolumeChanged()
		{
			m_AudioMixer.SetFloat(m_MusicVolume, 40f * Mathf.Log10(m_Music.value));
			m_AudioMixer.SetFloat(m_SfxVolume, 40f * Mathf.Log10(m_Sfx.value));
			m_AudioMixer.SetFloat(m_MasterVolume, 40f * Mathf.Log10(m_Master.value));
		}

		//Initialise volumes
		private void OnEnable()
		{
			if (m_PlayerDataManager != null)
			{
				m_InitialMusicVolume = m_PlayerDataManager.musicVolume;
				m_InitialSfxVolume = m_PlayerDataManager.sfxVolume;
				m_InitialMasterVolume = m_PlayerDataManager.masterVolume;

				m_Music.value = Mathf.Pow(10f, m_InitialMusicVolume / 40f);
				m_Sfx.value = Mathf.Pow(10f, m_InitialSfxVolume / 40f);
				m_Master.value = Mathf.Pow(10f, m_InitialMasterVolume / 40f);
			}
		}

		//On disable we set all fields to whatever was saved. If no changes have been saved via the Accept button, everything reverts to its old settings.
		private void OnDisable()
		{

			m_LeftyMode.isOn = m_PlayerDataManager.isLeftyMode;
			m_Thumbstick.value = m_PlayerDataManager.thumbstickSize;

			m_Everyplay.isOn = m_PlayerDataManager.everyplayEnabled;

			m_AudioMixer.SetFloat(m_MusicVolume, m_PlayerDataManager.musicVolume);
			m_AudioMixer.SetFloat(m_SfxVolume, m_PlayerDataManager.sfxVolume);
			m_AudioMixer.SetFloat(m_MasterVolume, m_PlayerDataManager.masterVolume);
		}

		//Configures which UI elements are enabled
		private void Awake()
		{
			bool touchDevice = Input.touchSupported;
			for (int i = 0; i < m_TouchDevicesOnly.Length; i++)
			{
				m_TouchDevicesOnly[i].SetActive(touchDevice);
			}

			bool everyplaySupported = Everyplay.IsSupported() && Everyplay.IsRecordingSupported();
			for (int i = 0; i < m_EveryplayCompatibleDevicesOnly.Length; i++)
			{
				m_EveryplayCompatibleDevicesOnly[i].SetActive(everyplaySupported);
			}
		}

		//Cache PlayerManager and initialise
		private void Start()
		{
			if (PlayerDataManager.s_InstanceExists)
			{
				m_PlayerDataManager = PlayerDataManager.s_Instance;
				m_LeftyMode.isOn = m_PlayerDataManager.isLeftyMode;
				m_Everyplay.isOn = m_PlayerDataManager.everyplayEnabled;
				m_Music.value = Mathf.Pow(10f, m_PlayerDataManager.musicVolume / 40f);
				m_Sfx.value = Mathf.Pow(10f, m_PlayerDataManager.sfxVolume / 40f);
				m_Master.value = Mathf.Pow(10f, m_PlayerDataManager.masterVolume / 40f);
				m_Thumbstick.value = m_PlayerDataManager.thumbstickSize;

				m_InitialMusicVolume = m_PlayerDataManager.musicVolume;
				m_InitialSfxVolume = m_PlayerDataManager.sfxVolume;
				m_InitialMasterVolume = m_PlayerDataManager.masterVolume;
			}
			else
			{
				Debug.LogWarning("No PlayerDataManager instance");
			}
		}
	}
}
