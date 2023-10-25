using System;
using UnityEngine.UIElements;

namespace Unity.DedicatedGameServerSample.Runtime
{
    public class ClientConnectingView : View<MetagameApplication>
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
            Broadcast(new CancelConnectionEvent());
        }

        void Update()
        {
            var elapsedTime = TimeSpan.FromSeconds(App.Model.ClientConnecting.ElapsedTime);
            m_TimerLabel.text = ($"{elapsedTime.Minutes:D2}:{elapsedTime.Seconds:D2}");
        }
    }
}
