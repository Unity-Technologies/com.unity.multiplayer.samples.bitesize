using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
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

        const int k_AuthenticationMaxNameLength = 50;

        void Start()
        {
            GameplayEventHandler.OnConnectToSessionCompleted += OnConnectToSessionCompleted;
        }

        void OnDestroy()
        {
            GameplayEventHandler.OnConnectToSessionCompleted -= OnConnectToSessionCompleted;
        }

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
            m_StartButton.clicked += HandleStartButtonPressed;
            m_QuitButton.clicked += HandleQuitButtonPressed;
        }

        protected override void UnregisterEvents()
        {
            m_PlayerNameField.UnregisterValueChangedCallback(evt => OnFieldChanged());
            m_SessionNameField.UnregisterValueChangedCallback(evt => OnFieldChanged());
            m_StartButton.clicked -= HandleStartButtonPressed;
            m_QuitButton.clicked -= HandleQuitButtonPressed;
        }

        void OnFieldChanged()
        {
            m_PlayerNameField.value = SanitizePlayerName(m_PlayerNameField.value);
            string sessionName = m_SessionNameField.value;
            m_StartButton.SetEnabled(!string.IsNullOrEmpty(m_PlayerNameField.value) && !string.IsNullOrEmpty(sessionName));
        }

        void HandleStartButtonPressed()
        {
            string playerName = m_PlayerNameField.value;
            string sessionName = m_SessionNameField.value;
            m_StartButton.enabledSelf = false;
            GameplayEventHandler.StartButtonPressed(playerName, sessionName);
        }

        static string SanitizePlayerName(string dirtyString)
        {
            var output = Regex.Replace(dirtyString, @"\s", "");
            return output[..Math.Min(output.Length, k_AuthenticationMaxNameLength)];
        }

        void HandleQuitButtonPressed()
        {
            GameplayEventHandler.QuitGamePressed();
        }

        void OnConnectToSessionCompleted(Task task, string sessionName )
        {
            if (!task.IsCompletedSuccessfully)
            {
                m_StartButton.enabledSelf = true;
            }
        }
    }
}
