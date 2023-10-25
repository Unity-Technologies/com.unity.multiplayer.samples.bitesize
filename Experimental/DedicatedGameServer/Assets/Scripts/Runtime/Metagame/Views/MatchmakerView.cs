using System;
using UnityEngine.UIElements;
namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class MatchmakerView : View<MetagameApplication>
    {
        Button m_QuitButton;
        Label m_TimerLabel;
        UIDocument m_UIDocument;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement;
            m_QuitButton = root.Q<Button>("quitButton");
            m_TimerLabel = root.Query<Label>("timerLabel");
            m_QuitButton.RegisterCallback<ClickEvent>(OnClickQuit);
        }

        void OnDisable()
        {
            m_QuitButton.UnregisterCallback<ClickEvent>(OnClickQuit);
        }

        void OnClickQuit(ClickEvent evt)
        {
            Broadcast(new ExitMatchmakerQueueEvent());
        }

        internal void UpdateTimer(int elapsedSeconds)
        {
            TimeSpan elapsedTime = TimeSpan.FromSeconds(elapsedSeconds);
            m_TimerLabel.text = (string.Format("{0:D2}:{1:D2}", elapsedTime.Minutes, elapsedTime.Seconds));
        }
    }
}
