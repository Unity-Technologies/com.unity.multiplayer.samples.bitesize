using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class MatchView : View<GameApplication>
    {
        Button m_WinButton;
        Label m_TimerLabel;
        VisualElement root;

        void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            root = uiDocument.rootVisualElement;

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
