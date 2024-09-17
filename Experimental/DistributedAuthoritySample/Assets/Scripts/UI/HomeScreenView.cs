using System;
using UnityEngine;
using UnityEngine.UIElements;

public class HomeScreenView : UIView
{
    TextField m_PlayerNameField;
    TextField m_SessionNameField;
    Button m_StartButton;
    Button m_QuitButton;
    ServicesHelper m_ServicesHelper;

    public override void Initialize(VisualElement viewRoot)
    {
        base.Initialize(viewRoot);
        m_PlayerNameField = m_Root.Q<TextField>("tf_player_name");
        m_SessionNameField = m_Root.Q<TextField>("tf_session_name");
        m_StartButton = m_Root.Q<Button>("bt_start");
        m_QuitButton = m_Root.Q<Button>("bt_quit");

        // Assume ServicesHelper is attached to the same GameObject
        m_ServicesHelper = FindAnyObjectByType<ServicesHelper>();
        m_StartButton.SetEnabled(false);
    }

    public override void RegisterEvents()
    {
        m_PlayerNameField.RegisterValueChangedCallback(evt => OnFieldChanged());
        m_SessionNameField.RegisterValueChangedCallback(evt => OnFieldChanged());
        m_StartButton.RegisterCallback<ClickEvent>(HandleStartClicked);
        m_QuitButton.RegisterCallback<ClickEvent>(HandleQuitClicked);
    }

    public override void UnregisterEvents()
    {
        m_PlayerNameField.UnregisterValueChangedCallback(evt => OnFieldChanged());
        m_SessionNameField.UnregisterValueChangedCallback(evt => OnFieldChanged());
        m_StartButton.UnregisterCallback<ClickEvent>(HandleStartClicked);
        m_QuitButton.UnregisterCallback<ClickEvent>(HandleQuitClicked);
    }

    void OnFieldChanged()
    {
        string playerName = m_PlayerNameField.value;
        string sessionName = m_SessionNameField.value;
        m_StartButton.SetEnabled(!string.IsNullOrEmpty(playerName) && !string.IsNullOrEmpty(sessionName));
    }

    void HandleStartClicked(ClickEvent evt)
    {
        Debug.Log("Start button clicked");
        string sessionName = m_SessionNameField.value;

        // Call ServicesHelper to connect to the session
        m_ServicesHelper.SetSessionName(sessionName);

        // Start the async connection process
        StartSessionConnection();
    }

    async void StartSessionConnection()
    {
        try
        {
            await m_ServicesHelper.ConnectToSession();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error connecting to session: {ex.Message}");
        }
    }

    void HandleQuitClicked(ClickEvent evt)
    {
        Application.Quit();
    }
}
