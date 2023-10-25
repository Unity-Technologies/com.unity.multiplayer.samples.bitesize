using System;
using UnityEngine.UIElements;

namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class MatchView : View<GameApplication>
    {
        Button m_WinButton;
        Label m_TimerLabel;
        UIDocument m_UIDocument;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement;
            m_WinButton = root.Q<Button>("winButton");
            m_WinButton.RegisterCallback<ClickEvent>(OnClickWin);

            m_TimerLabel = root.Query<Label>("timerLabel");
        }

        void OnDisable()
        {
            m_WinButton.UnregisterCallback<ClickEvent>(OnClickWin);
        }

        internal void OnCountdownChanged(uint newValue)
        {
            m_TimerLabel.text = string.Format("{0:D2}:{1:D2}", newValue / 60, newValue % 60);
        }

        void OnClickWin(ClickEvent evt)
        {
            Broadcast(new WinButtonClickedEvent());
        }
    }
}
