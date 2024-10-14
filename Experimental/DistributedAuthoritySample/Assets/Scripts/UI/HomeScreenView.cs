using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    class HomeScreenView : UIView
    {
        TextField m_PlayerNameField;
        TextField m_SessionNameField;
        Button m_StartButton;
        Button m_QuitButton;

        internal static event Action<string> StartButtonPressed;

        public override void Initialize(VisualElement viewRoot)
        {
            base.Initialize(viewRoot);
            m_PlayerNameField = m_Root.Q<TextField>("tf_player_name");
            m_SessionNameField = m_Root.Q<TextField>("tf_session_name");
            m_StartButton = m_Root.Q<Button>("bt_start");
            m_QuitButton = m_Root.Q<Button>("bt_quit");
            m_StartButton.SetEnabled(false);
        }

        protected override void RegisterEvents()
        {
            m_PlayerNameField.RegisterValueChangedCallback(evt => OnFieldChanged());
            m_SessionNameField.RegisterValueChangedCallback(evt => OnFieldChanged());
            m_StartButton.RegisterCallback<ClickEvent>(HandleStartButtonPressed);
            m_QuitButton.RegisterCallback<ClickEvent>(HandleQuitButtonPressed);
        }

        protected override void UnregisterEvents()
        {
            m_PlayerNameField.UnregisterValueChangedCallback(evt => OnFieldChanged());
            m_SessionNameField.UnregisterValueChangedCallback(evt => OnFieldChanged());
            m_StartButton.UnregisterCallback<ClickEvent>(HandleStartButtonPressed);
            m_QuitButton.UnregisterCallback<ClickEvent>(HandleQuitButtonPressed);
        }

        void OnFieldChanged()
        {
            string playerName = m_PlayerNameField.value;
            string sessionName = m_SessionNameField.value;
            m_StartButton.SetEnabled(!string.IsNullOrEmpty(playerName) && !string.IsNullOrEmpty(sessionName));
        }

        void HandleStartButtonPressed(ClickEvent evt)
        {
            string sessionName = m_SessionNameField.value;
            //this should be reset if something goes wrong on connect
            m_StartButton.enabledSelf = false;
            StartButtonPressed?.Invoke(sessionName);
        }

        void HandleQuitButtonPressed(ClickEvent evt)
        {
            Application.Quit();
        }
    }
}
