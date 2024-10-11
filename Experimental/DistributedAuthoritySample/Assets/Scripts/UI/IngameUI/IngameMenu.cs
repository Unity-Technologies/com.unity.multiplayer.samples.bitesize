using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    public class IngameMenu : MonoBehaviour
    {
        [SerializeField]
        UIDocument m_UIDocument;

        [SerializeField]
        VisualTreeAsset m_IngameMenuAsset;

        VisualElement m_Root;

        VisualElement menu;
        VisualElement sceenOverlay;

        void OnEnable()
        {
            m_Root = m_UIDocument.rootVisualElement.Q<VisualElement>("ingame-menu-container");
            m_Root.Add(m_IngameMenuAsset.CloneTree().Children().ToArray()[0]);
            m_Root.Q<Button>("burger-button").clicked += ShowMenu;

            menu = m_Root.Q<VisualElement>("menu");
            sceenOverlay = m_Root.Q<VisualElement>("sceen-overlay");
            menu.AddToClassList("hide");

            menu.Q<Button>("btn-exit").clicked += QuitGame;
            menu.Q<Button>("btn-goto-main").clicked += GoToMainScene;
            menu.Q<Button>("btn-close-menu").clicked += HideMenu;

            HideMenu();
        }

        void HideMenu()
        {
            sceenOverlay.style.display = DisplayStyle.None;
            menu.RemoveFromClassList("show");
            menu.AddToClassList("hide");
        }

        void GoToMainScene()
        {
            //Todo: Use service Helper
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }

        void QuitGame()
        {
            Debug.Log("Quit Game");
            NetworkManager.Singleton.Shutdown();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }

        void ShowMenu()
        {
            Debug.Log("Show Menu");
            menu.RemoveFromClassList("hide");
            menu.AddToClassList("show");
            sceenOverlay.style.display = DisplayStyle.Flex;
        }
    }
}
