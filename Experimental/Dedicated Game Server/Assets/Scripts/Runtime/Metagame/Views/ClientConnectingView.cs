using System;
using UnityEngine.UIElements;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class ClientConnectingView : View<MetagameApplication>
    {
        Button m_QuitButton;
        Label m_TimerLabel;
        VisualElement m_Root;
        UIDocument m_UIDocument;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            m_Root = m_UIDocument.rootVisualElement;
            m_QuitButton = m_Root.Q<Button>("quitButton");
            m_TimerLabel = m_Root.Query<Label>("timerLabel");
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
