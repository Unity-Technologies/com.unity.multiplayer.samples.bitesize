using System;
using UnityEngine.UIElements;

namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class MatchView : View<GameApplication>
    {
        UIDocument m_UIDocument;
        Label m_TimerLabel;
        Label m_PlayersConnectedLabel;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement;
            m_TimerLabel = root.Query<Label>("timerLabel");
            m_PlayersConnectedLabel = root.Query<Label>("playersConnectedLabel");
        }

        internal void OnCountdownChanged(uint newValue)
        {
            m_TimerLabel.text = string.Format("{0:D2}:{1:D2}", newValue / 60, newValue % 60);
        }

        internal void OnPlayersConnectedChanged(int newValue)
        {
            m_PlayersConnectedLabel.text = $"Players connected: {newValue}";
        }
    }
}
