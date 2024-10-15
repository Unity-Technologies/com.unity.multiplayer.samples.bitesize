using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// Ingame Menu to show options like exit, go to main menu etc.
    /// </summary>
    public class IngameMenu : MonoBehaviour
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
            m_Menu.AddToClassList(UIUtils.inactiveUSSClass);

            m_Menu.Q<Button>("btn-exit").clicked += QuitGame;
            m_Menu.Q<Button>("btn-goto-main").clicked += GoToMainScene;
            m_Menu.Q<Button>("btn-close-menu").clicked += HideMenu;

            HideMenu();
        }

        void HideMenu()
        {
            m_SceenOverlay.style.display = DisplayStyle.None;
            m_Menu.RemoveFromClassList(UIUtils.activeUSSClass);
            m_Menu.AddToClassList(UIUtils.inactiveUSSClass);
            m_Menu.SetEnabled(false);
        }

        void GoToMainScene()
        {
            //Todo: Use service Helper
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }

        void QuitGame()
        {
            NetworkManager.Singleton.Shutdown();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }

        void ShowMenu()
        {
            m_Menu.RemoveFromClassList(UIUtils.inactiveUSSClass);
            m_Menu.AddToClassList(UIUtils.activeUSSClass);
            m_SceenOverlay.style.display = DisplayStyle.Flex;
            m_Menu.SetEnabled(true);
        }
    }
}
