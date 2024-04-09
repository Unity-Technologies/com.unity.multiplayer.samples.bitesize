using System;
using UnityEngine.UIElements;

namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class GameMenuView : View<GameApplication>
    {
        Button m_ResumeButton;
        Button m_QuitButton;
        UIDocument m_UIDocument;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement;
            m_ResumeButton = root.Q<Button>("resumeButton");
            m_ResumeButton.RegisterCallback<ClickEvent>(OnClickResume);
            m_QuitButton = root.Q<Button>("quitButton");
            m_QuitButton.RegisterCallback<ClickEvent>(OnClickQuit);
        }

        void OnDisable()
        {
            m_ResumeButton.UnregisterCallback<ClickEvent>(OnClickResume);
            m_QuitButton.UnregisterCallback<ClickEvent>(OnClickQuit);
        }

        void OnClickResume(ClickEvent evt)
        {
            Broadcast(new ResumeButtonClickedEvent());
        }

        void OnClickQuit(ClickEvent evt)
        {
            Broadcast(new QuitButtonClickedEvent());
        }
    }
}
