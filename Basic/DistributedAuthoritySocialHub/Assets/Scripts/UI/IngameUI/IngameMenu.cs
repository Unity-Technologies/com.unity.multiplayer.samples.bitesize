using System;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
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

        VisualElement m_SceenOverlay;

        void OnEnable()
        {
            m_Root = m_UIDocument.rootVisualElement.Q<VisualElement>("ingame-menu-container");
            m_Root.Add(m_IngameMenuAsset.CloneTree().GetFirstChild());
            GameInput.Actions.Player.TogglePauseMenu.performed += OnTogglePauseMenu;

            m_Menu = m_Root.Q<VisualElement>("menu");
            m_SceenOverlay = m_Root.Q<VisualElement>("sceen-overlay");
            m_Menu.AddToClassList(UIUtils.s_InactiveUSSClass);

            m_Menu.Q<Button>("btn-exit").clicked += QuitGame;
            m_Menu.Q<Button>("btn-goto-main").clicked += GoToMainMenuScene;
            m_Menu.Q<Button>("btn-close-menu").clicked += HideMenu;

            HideMenu();
        }

        void OnTogglePauseMenu(InputAction.CallbackContext _)
        {
            ShowMenu();
        }

        void OnDisable()
        {
            GameInput.Actions.Player.TogglePauseMenu.performed -= OnTogglePauseMenu;
            m_Menu.Q<Button>("btn-exit").clicked -= QuitGame;
            m_Menu.Q<Button>("btn-goto-main").clicked -= GoToMainMenuScene;
            m_Menu.Q<Button>("btn-close-menu").clicked -= HideMenu;
        }

        void HideMenu()
        {
            InputSystemManager.Instance.EnableGameplayInputs();
            m_SceenOverlay.style.display = DisplayStyle.None;
            m_Menu.RemoveFromClassList(UIUtils.s_ActiveUSSClass);
            m_Menu.AddToClassList(UIUtils.s_InactiveUSSClass);
            m_Menu.SetEnabled(false);
        }

        static void GoToMainMenuScene()
        {
            GameplayEventHandler.ReturnToMainMenuPressed();
        }

        static void QuitGame()
        {
            GameplayEventHandler.QuitGamePressed();
        }

        void ShowMenu()
        {
            InputSystemManager.Instance.EnableUIInputs();
            m_Menu.RemoveFromClassList(UIUtils.s_InactiveUSSClass);
            m_Menu.AddToClassList(UIUtils.s_ActiveUSSClass);
            m_SceenOverlay.style.display = DisplayStyle.Flex;
            m_Menu.SetEnabled(true);
        }
    }
}
