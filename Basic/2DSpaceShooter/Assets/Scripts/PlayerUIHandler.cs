using System;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerUIHandler : MonoBehaviour
{
    [SerializeField]
    UIDocument m_UIDocument;
    
    VisualElement m_RootVisualElement;
    
    ProgressBar m_HealthBar;
    
    ProgressBar m_EnergyBar;
    
    VisualElement m_PlayerUIWrapper;
    
    TextElement m_PlayerName;
    
    Camera m_MainCamera;

    void Awake()
    {
        m_RootVisualElement = m_UIDocument.rootVisualElement;
        m_PlayerUIWrapper = m_RootVisualElement.Q<VisualElement>("PlayerUIWrapper");
        m_HealthBar = m_RootVisualElement.Q<ProgressBar>(name:"HealthBar");
        m_EnergyBar = m_RootVisualElement.Q<ProgressBar>(name:"EnergyBar");
        m_PlayerName = m_RootVisualElement.Q<TextElement>("PlayerName");
        m_MainCamera = Camera.main;
    }
    
    public void SetWrapperPosition(Vector2 wrapperPosition)
    {
        Vector2 screenPosition = RuntimePanelUtils.CameraTransformWorldToPanel(m_PlayerUIWrapper.panel, wrapperPosition, m_MainCamera);
        m_PlayerUIWrapper.transform.position = screenPosition;
    }
    
    public void SetHealthBarValue(int healthBarValue)
    {
        m_HealthBar.value = healthBarValue;
    }

    public void SetEnergyBarValue(int resourceBarValue)
    {
        m_EnergyBar.value = resourceBarValue;
    }

    public void SetPlayerName(string playerName)
    {
        m_PlayerName.text = playerName;
    }
}
