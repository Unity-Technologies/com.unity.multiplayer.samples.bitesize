using Unity.Multiplayer.Samples.SocialHub.GameManagement;

using Unity.Services.Vivox;
using Unity.Multiplayer.Samples.SocialHub.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// Ingame Menu to show options like exit, go to main menu etc.
    /// </summary>
    class IngameMenu : MonoBehaviour
    {
        [SerializeField]
        UIDocument m_UIDocument;

        [SerializeField]
        VisualTreeAsset m_IngameMenuAsset;

        VisualElement m_Root;
        VisualElement m_Menu;
        VisualElement m_ScreenOverlay;

        Button m_BurgerButton;
        Button m_ExitButton;
        Button m_GotoMainButton;
        Button m_CloseMenuButton;

        Toggle m_MuteToggle;

        Slider m_InputVolumeSlider;
        Slider m_OutputVolumeSlider;

        DropdownField m_InputDevicesDropdown;
        DropdownField m_OutputDevicesDropdown;

        void OnEnable()
        {
            m_Root = m_UIDocument.rootVisualElement.Q<VisualElement>("ingame-menu-container");
            m_Root.Add(m_IngameMenuAsset.CloneTree().GetFirstChild());

            m_ScreenOverlay = m_Root.Q<VisualElement>("screen-overlay");

            m_BurgerButton = m_Root.Q<Button>("burger-button");
            m_BurgerButton.clicked += ShowMenu;

            m_Menu = m_Root.Q<VisualElement>("menu");
            m_Menu.AddToClassList(UIUtils.s_InactiveUSSClass);

            m_ExitButton = m_Menu.Q<Button>("btn-exit");
            m_ExitButton.clicked += QuitGame;

            m_GotoMainButton = m_Menu.Q<Button>("btn-goto-main");
            m_GotoMainButton.clicked += GoToMainMenuScene;

            m_CloseMenuButton = m_Menu.Q<Button>("btn-close-menu");
            m_CloseMenuButton.clicked += HideMenu;

            GameInput.Actions.Player.TogglePauseMenu.performed += OnTogglePauseMenu;

            // Audio settings

            // Input Selection
            m_InputDevicesDropdown = m_Menu.Q<DropdownField>("audio-input");
            PopulateAudioInputDevices();
            m_InputDevicesDropdown.RegisterValueChangedCallback(evt => OnInputDeviceDropDownChanged(evt));

            // Output Selection
            m_OutputDevicesDropdown = m_Menu.Q<DropdownField>("audio-output");
            PopulateAudioOutputDevices();
            m_OutputDevicesDropdown.value = VivoxService.Instance.ActiveOutputDevice.DeviceName;
            m_OutputDevicesDropdown.RegisterValueChangedCallback(evt => OnOutputDeviceDropdownChanged(evt));

            // Input Volume
            m_InputVolumeSlider = m_Menu.Q<Slider>("input-volume");
            m_InputVolumeSlider.value = VivoxService.Instance.InputDeviceVolume + 50;
            m_InputVolumeSlider.RegisterValueChangedCallback(evt => OnInputVolumeChanged(evt));

            // Output Volume
            m_OutputVolumeSlider = m_Menu.Q<Slider>("output-volume");
            m_OutputVolumeSlider.value = VivoxService.Instance.OutputDeviceVolume + 50;
            m_OutputVolumeSlider.RegisterValueChangedCallback(evt => OnOutputVolumeChanged(evt));

            // Mute Button
            m_MuteToggle = m_Menu.Q<Toggle>("mute-checkbox");
            m_MuteToggle.SetValueWithoutNotify(VivoxService.Instance.IsInputDeviceMuted);
            m_MuteToggle.RegisterValueChangedCallback(evt => OnMuteCheckboxChanged(evt));

            VivoxService.Instance.AvailableInputDevicesChanged += PopulateAudioInputDevices;
            VivoxService.Instance.AvailableOutputDevicesChanged += PopulateAudioOutputDevices;
            HideMenu();
        }

        void OnOutputVolumeChanged(ChangeEvent<float> evt)
        {
            // Vivox Volume is from  -50 to 50
            var vol = evt.newValue - 50;
            VivoxService.Instance.SetOutputDeviceVolume((int)vol);
        }

        void OnInputVolumeChanged(ChangeEvent<float> evt)
        {
            // Vivox Volume is from  -50 to 50
            var vol = evt.newValue - 50;
            VivoxService.Instance.SetInputDeviceVolume((int)vol);
        }

        void OnTogglePauseMenu(InputAction.CallbackContext _)
        {
            ShowMenu();
        }

        void ShowMenu()
        {
            InputSystemManager.Instance.EnableUIInputs();
            m_Menu.RemoveFromClassList(UIUtils.s_InactiveUSSClass);
            m_Menu.AddToClassList(UIUtils.s_ActiveUSSClass);
            m_ScreenOverlay.style.display = DisplayStyle.Flex;
            m_Menu.SetEnabled(true);
        }

        void HideMenu()
        {
            InputSystemManager.Instance.EnableGameplayInputs();
            m_ScreenOverlay.style.display = DisplayStyle.None;
            m_Menu.RemoveFromClassList(UIUtils.s_ActiveUSSClass);
            m_Menu.AddToClassList(UIUtils.s_InactiveUSSClass);
            m_Menu.SetEnabled(false);
        }

        void PopulateAudioInputDevices()
        {
            m_InputDevicesDropdown.choices.Clear();
            foreach (var inputDevice in VivoxService.Instance.AvailableInputDevices)
            {
                m_InputDevicesDropdown.choices.Add(inputDevice.DeviceName);
            }

            m_InputDevicesDropdown.SetValueWithoutNotify(VivoxService.Instance.ActiveInputDevice.DeviceName);
        }

        void PopulateAudioOutputDevices()
        {
            m_OutputDevicesDropdown.choices.Clear();
            foreach (var outputDevice in VivoxService.Instance.AvailableOutputDevices)
            {
                m_OutputDevicesDropdown.choices.Add(outputDevice.DeviceName);
            }

            m_OutputDevicesDropdown.SetValueWithoutNotify(VivoxService.Instance.ActiveOutputDevice.DeviceName);
        }

        void OnMuteCheckboxChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                VivoxService.Instance.MuteInputDevice();
            else
                VivoxService.Instance.UnmuteInputDevice();
        }

        async void OnOutputDeviceDropdownChanged(ChangeEvent<string> evt)
        {
            // Capture the values because we need them if something goes wrong.
            var newValue = evt.newValue;
            DropdownField dropdown = (DropdownField)evt.target;

            foreach (var outputDevice in VivoxService.Instance.AvailableOutputDevices)
            {
                if (outputDevice.DeviceName == newValue)
                {
                    await VivoxService.Instance.SetActiveOutputDeviceAsync(outputDevice);
                    break;
                }
            }

            if (VivoxService.Instance.ActiveOutputDevice.DeviceName != newValue)
            {
                Debug.LogWarning("Could not set Audio Output Device " + newValue);
                dropdown.value = VivoxService.Instance.ActiveOutputDevice.DeviceName;
            }
        }

        async void OnInputDeviceDropDownChanged(ChangeEvent<string> evt)
        {
            // Capture the values because we need them if something goes wrong.
            var newValue = evt.newValue;
            DropdownField dropdown = (DropdownField)evt.target;

            foreach (var inputDevice in VivoxService.Instance.AvailableInputDevices)
            {
                if (inputDevice.DeviceName == newValue)
                {
                    await VivoxService.Instance.SetActiveInputDeviceAsync(inputDevice);
                    break;
                }
            }

            if (VivoxService.Instance.ActiveInputDevice.DeviceName != newValue)
            {
                Debug.LogWarning("Could not set Audio Input Device " + newValue);
                dropdown.value = VivoxService.Instance.ActiveOutputDevice.DeviceName;
            }
        }

        void OnDisable()
        {
            m_BurgerButton.clicked -= ShowMenu;
            m_ExitButton.clicked -= QuitGame;
            m_GotoMainButton.clicked -= GoToMainMenuScene;
            m_CloseMenuButton.clicked -= HideMenu;

            m_InputDevicesDropdown.UnregisterValueChangedCallback(evt => OnInputDeviceDropDownChanged(evt));
            m_OutputDevicesDropdown.UnregisterValueChangedCallback(evt => OnOutputDeviceDropdownChanged(evt));

            m_InputVolumeSlider.UnregisterValueChangedCallback(evt => OnInputVolumeChanged(evt));
            m_OutputVolumeSlider.UnregisterValueChangedCallback(evt => OnOutputVolumeChanged(evt));

            VivoxService.Instance.AvailableInputDevicesChanged -= PopulateAudioInputDevices;
            VivoxService.Instance.AvailableOutputDevicesChanged -= PopulateAudioOutputDevices;

            GameInput.Actions.Player.TogglePauseMenu.performed -= OnTogglePauseMenu;
        }

        static void GoToMainMenuScene()
        {
            GameplayEventHandler.ReturnToMainMenuPressed();
        }

        static void QuitGame()
        {
            GameplayEventHandler.QuitGamePressed();
        }
    }
}
