using UnityEngine;
using Tanks.TankControllers;
using Tanks.Analytics;
using Tanks.Networking;
using Tanks.Utilities;
using Tanks.Data;
using System;

namespace Tanks.UI
{
	/// <summary>
	/// Controls the display and functionality of the in-game pause/options menu. Applicable to single and multiplayer games.
	/// </summary>
	public class InGameOptionsMenu : Singleton<InGameOptionsMenu>
	{
		[Header("Settings menu")]
		//Settings menu modal object to instantiate (should be the prefab of the same one that appears in the main menu).
		[SerializeField]
		protected GameObject m_SettingsMenuPrefab;

		//Events to subscribe to for pause and resume.
		public event Action paused, resumed;

		//Internal list of input modules for activation/deactivation.
		private TankInputModule[] m_InputModules = null;

		//Original time scale for unpausing.
		private float m_OldTimeScale = 1f;

		//Internal reference to current game settings.
		protected GameSettings m_Settings;

		//Internal reference to settings modal instance
		private SettingsModal settingsModal;

		protected override void Awake()
		{
			base.Awake();
			m_Settings = GameSettings.s_Instance;
			gameObject.SetActive(false);
		}

		/// <summary>
		/// Displays the menu, disabling game input and pausing gameplay if possible.
		/// </summary>
		public void OnOptionsClicked()
		{
			HUDController.s_Instance.HideVPad();

			gameObject.SetActive(true);
			ToggleInput(false);
			if (paused != null)
			{
				paused();
			}
			if (m_Settings.isSinglePlayer)
			{
				m_OldTimeScale = Time.timeScale;
				Time.timeScale = 0f;
			}
		}

		/// <summary>
		/// Returns to game, reenabling input modules and unpausing if necessary.
		/// </summary>
		public void OnReturnToGameClicked()
		{
			gameObject.SetActive(false);
			ToggleInput(true);

			//We check our Everyplay settings and status, in case it's just been enabled in the settings menu.
			CheckEveryplay();

			if (resumed != null)
			{
				resumed();
			}
			if (m_Settings.isSinglePlayer)
			{
				if (m_OldTimeScale == 0f)
				{
					m_OldTimeScale = 1f;
				}
				Time.timeScale = m_OldTimeScale;
			}
		}

		/// <summary>
		/// Exits the active game and returns to menu.
		/// </summary>
		public void OnBailClicked()
		{
			NetworkManager netManager = NetworkManager.s_Instance;

			GameManager gameManager = GameManager.s_Instance;

			// There will be a local rules processor in single player
			if (m_Settings.isSinglePlayer)
			{
				AnalyticsHelper.SinglePlayerLevelQuit(m_Settings.map.id);
				Time.timeScale = m_OldTimeScale;

				gameManager.rulesProcessor.Bail();
			}
			else
			{
				netManager.Disconnect();
				netManager.ReturnToMenu(MenuPage.Home);

				AnalyticsHelper.MultiplayerGamePlayerBailed(m_Settings.map.id, m_Settings.mode.id, GameManager.s_Tanks.Count, Mathf.RoundToInt(Time.timeSinceLevelLoad), gameManager.GetLocalPlayerPosition(), gameManager.localPlayer.playerTankType.id);
			}
		}

		/// <summary>
		/// Displays the settings modal to tweak game options in-game.
		/// </summary>
		public void ShowSettingsMenu()
		{
			if (settingsModal == null)
			{
				//For consistency, we instantiate a copy of the main menu's settings prefab instead of having one ready-made in the scene.
				settingsModal = ((GameObject)Instantiate(m_SettingsMenuPrefab)).GetComponent<SettingsModal>();
				settingsModal.transform.SetParent(transform.parent, false);
				settingsModal.transform.localScale = Vector3.one;
			}

			settingsModal.Show();
		}

		/// <summary>
		/// Toggles input managers on and off.
		/// </summary>
		/// <param name="isActive">The state to which the input managers are set.</param>
		private void ToggleInput(bool isActive)
		{
			LazyLoadInputModules();
            
			if (m_InputModules == null)
			{
				return;
			}
			int length = m_InputModules.Length;
			for (int i = 0; i < length; i++)
			{
				TankInputModule inputModule = m_InputModules[i];
				if (inputModule != null)
				{
					inputModule.enabled = isActive;

					if (isActive)
					{
						TankTouchInput touchInput = inputModule as TankTouchInput;

						if (touchInput != null)
						{
							touchInput.InitDirectionAndSize();
							touchInput.OnStartAuthority();
						}
					}
				}
			}
		}

		private void LazyLoadInputModules()
		{
			if (!(m_InputModules == null || m_InputModules.Length == 0))
			{
				return;
			}
            
			TankShooting shooting = TankShooting.s_LocalTank;
			if (shooting == null)
			{
				Debug.Log("Cannot find local tank - unable to toggle input!");
				return;
			}

			m_InputModules = TankShooting.s_LocalTank.gameObject.GetComponentsInChildren<TankInputModule>();
		}

		//Change everyplay to suit current game settings.
		private void CheckEveryplay()
		{
			if (Everyplay.IsSupported())
			{
				if (PlayerDataManager.s_Instance.everyplayEnabled)
				{
					//If everyplay is enabled and not recording, set it going.
					if (!Everyplay.IsRecording() && Everyplay.IsReadyForRecording())
					{
						Everyplay.StartRecording();
					}
				}
				else
				{
					//If everyplay has been disabled and it's still recording, stop it.
					if (Everyplay.IsRecording())
					{
						Everyplay.StopRecording();
					}
				}
			}
		}
	}
}