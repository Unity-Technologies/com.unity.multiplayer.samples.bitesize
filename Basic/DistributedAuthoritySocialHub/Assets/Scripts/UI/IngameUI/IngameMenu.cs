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
            var audioDropdown = m_Menu.Q<DropdownField>();


            foreach (var inputDevice in VivoxService.Instance.AvailableInputDevices)
            {
                audioDropdown.choices.Add(inputDevice.DeviceName);
            }
            audioDropdown.value = VivoxService.Instance.ActiveInputDevice.DeviceName;
            audioDropdown.RegisterValueChangedCallback(evt =>
            {
                SetVivoxInputDevice(evt.newValue);
            });
            HideMenu();
        }

        async void SetVivoxInputDevice(string deviceName)
        {
            foreach (var inputDevice in VivoxService.Instance.AvailableInputDevices)
            {
                if (inputDevice.DeviceName == deviceName)
                {
                    await VivoxService.Instance.SetActiveInputDeviceAsync(inputDevice);
                    break;
                }
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
