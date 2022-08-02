using System;
using UnityEngine;
using UnityEngine.UIElements;

public class InGameUIHandler : MonoBehaviour
{
    VisualElement m_RootVisualElement;
    Button m_HostButton;
    Button m_ServerButton;
    Button m_ClientButton;

    void Awake()
    {
        m_RootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        m_HostButton = m_RootVisualElement.Q<Button>("HostButton");
        m_ServerButton = m_RootVisualElement.Q<Button>("ServerButton");
        m_ClientButton = m_RootVisualElement.Q<Button>("ClientButton");
        
        m_HostButton.clickable.clickedWithEventInfo += HostButtonClicked;
        m_ServerButton.clickable.clickedWithEventInfo += ServerButtonClicked;
        m_ClientButton.clickable.clickedWithEventInfo += ClientButtonClicked;
    }

    void ClientButtonClicked(EventBase obj)
    {
        
    }

    void ServerButtonClicked(EventBase obj)
    {
        
    }

    void HostButtonClicked(EventBase obj)
    {
        
    }

    void OnDestroy()
    {
        if (m_HostButton != null)
        {
            m_HostButton.clickable.clickedWithEventInfo -= HostButtonClicked;
        }

        if (m_ServerButton != null)
        {
            m_ServerButton.clickable.clickedWithEventInfo -= ServerButtonClicked;
        }

        if (m_ClientButton != null)
        {
            m_ClientButton.clickable.clickedWithEventInfo -= ClientButtonClicked;
        }
    }
}
