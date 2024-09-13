using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;


    /// <summary>
    /// This is the main menu home view, which contains logic for all visual elements in the home screen uxml.
    /// </summary>
    public class HomeScreenView : UIView
    {
        Button m_StartButton;
        Button m_LoadButton;
        Button m_LeaderboardButton;
        Button m_SettingsButton;
        Button m_QuitButton;

        public override void Initialize(VisualElement viewRoot)
        {
            base.Initialize(viewRoot);

            m_StartButton = m_Root.Q<Button>("bt_start");
            m_QuitButton = m_Root.Q<Button>("bt_quit");
        }

        public override void RegisterEvents()
        {
            m_StartButton.RegisterCallback<ClickEvent>(HandleStartClicked);
            m_QuitButton.RegisterCallback<ClickEvent>(HandleQuitClicked);
        }

        public override void UnregisterEvents()
        {
            m_StartButton.UnregisterCallback<ClickEvent>(HandleStartClicked);
            m_QuitButton.UnregisterCallback<ClickEvent>(HandleQuitClicked);
        }

        void HandleStartClicked(ClickEvent evt)
        {
            Debug.Log("Start button clicked");
            SceneManager.LoadScene("HubScene_TownMarket", LoadSceneMode.Single);
        }
        void HandleQuitClicked(ClickEvent evt)
        {
            Application.Quit();
        }
    }
