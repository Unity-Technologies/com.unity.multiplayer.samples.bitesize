using System;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using Unity.Services.Vivox;
using UnityEngine;
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

        VisualElement m_SceenOverlay;

        void OnEnable()
        {
            m_Root = m_UIDocument.rootVisualElement.Q<VisualElement>("ingame-menu-container");
            m_Root.Add(m_IngameMenuAsset.CloneTree().GetFirstChild());
            m_Root.Q<Button>("burger-button").clicked += ShowMenu;

            m_Menu = m_Root.Q<VisualElement>("menu");
            m_SceenOverlay = m_Root.Q<VisualElement>("sceen-overlay");
            m_Menu.AddToClassList(UIUtils.s_InactiveUSSClass);

            m_Menu.Q<Button>("btn-exit").clicked += QuitGame;
            m_Menu.Q<Button>("btn-goto-main").clicked += GoToMainScene;
            m_Menu.Q<Button>("btn-close-menu").clicked += HideMenu;

            var muteCheckbox = m_Menu.Q<Toggle>("mute-checkbox");
            muteCheckbox.SetValueWithoutNotify(VivoxService.Instance.IsInputDeviceMuted);
            muteCheckbox.RegisterValueChangedCallback(evt => OnMuteCheckboxChanged(evt));

            var inputDevices = m_Menu.Q<DropdownField>("audio-input");
            foreach (var inputDevice in VivoxService.Instance.AvailableInputDevices)
            {
                inputDevices.choices.Add(inputDevice.DeviceName);
            }
            inputDevices.value = VivoxService.Instance.ActiveInputDevice.DeviceName;
            inputDevices.RegisterValueChangedCallback(evt => OnInputDeviceDropDownChanged(evt));

            var inputVolumeSlider = m_Menu.Q<Slider>("input-volume");
            inputVolumeSlider.value =  VivoxService.Instance.InputDeviceVolume + 50;
            // Volume is from -50 to 50
            inputVolumeSlider.RegisterValueChangedCallback(evt =>
            {
                var vol = evt.newValue - 50 ;
                VivoxService.Instance.SetInputDeviceVolume((int)vol);
            });

            // Volume is from -50 to 50
            var outputVolumeSlider = m_Menu.Q<Slider>("output-volume");
            outputVolumeSlider.value = VivoxService.Instance.OutputDeviceVolume + 50;
            outputVolumeSlider.RegisterValueChangedCallback(evt =>
            {
                var vol = evt.newValue - 50 ;
                VivoxService.Instance.SetInputDeviceVolume((int)vol);
            });

            var outputDevices = m_Menu.Q<DropdownField>("audio-output");
            foreach (var outputDevice in VivoxService.Instance.AvailableOutputDevices)
            {
                outputDevices.choices.Add(outputDevice.DeviceName);
            }
            outputDevices.value = VivoxService.Instance.ActiveOutputDevice.DeviceName;
            outputDevices.RegisterValueChangedCallback(evt => OnOutputDeviceDropdownChanged(evt));
            HideMenu();
        }

        void OnMuteCheckboxChanged(ChangeEvent<bool> evt)
        {
            if(evt.newValue)
                VivoxService.Instance.MuteInputDevice();
            else
                VivoxService.Instance.UnmuteInputDevice();
        }

        async void OnOutputDeviceDropdownChanged(ChangeEvent<string> evt)
        {
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
                Debug.LogWarning("Could not set Audio Output Device " +  newValue);
                dropdown.value = VivoxService.Instance.ActiveOutputDevice.DeviceName;
            }
        }

        async void OnInputDeviceDropDownChanged(ChangeEvent<string> evt)
        {
            // capture the values because we need them if something goes wrong.
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
                Debug.LogWarning("Could not set Audio Input Device " +  newValue);
                dropdown.value = VivoxService.Instance.ActiveOutputDevice.DeviceName;
            }
        }

        void OnDisable()
        {
            m_Root.Q<Button>("burger-button").clicked -= ShowMenu;
            m_Menu.Q<Button>("btn-exit").clicked -= QuitGame;
            m_Menu.Q<Button>("btn-goto-main").clicked -= GoToMainScene;
            m_Menu.Q<Button>("btn-close-menu").clicked -= HideMenu;
        }

        void HideMenu()
        {
            m_SceenOverlay.style.display = DisplayStyle.None;
            m_Menu.RemoveFromClassList(UIUtils.s_ActiveUSSClass);
            m_Menu.AddToClassList(UIUtils.s_InactiveUSSClass);
            m_Menu.SetEnabled(false);
        }

        static void GoToMainScene()
        {
            GameplayEventHandler.ReturnToMainMenuPressed();
        }

        static void QuitGame()
        {
            GameplayEventHandler.QuitGamePressed();
        }

        void ShowMenu()
        {
            m_Menu.RemoveFromClassList(UIUtils.s_InactiveUSSClass);
            m_Menu.AddToClassList(UIUtils.s_ActiveUSSClass);
            m_SceenOverlay.style.display = DisplayStyle.Flex;
            m_Menu.SetEnabled(true);
        }
    }
}
