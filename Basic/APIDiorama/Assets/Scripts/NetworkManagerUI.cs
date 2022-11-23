using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] Button m_ServerButton;
    [SerializeField] Button m_HostButton;
    [SerializeField] Button m_ClientButton;
    [SerializeField] Color32 m_HighlightedButtonColor;
    [SerializeField] Color32 m_DisabledButtonColor;
    Button[] m_Buttons;

    void Awake()
    {
        m_ServerButton.onClick.AddListener(StartServer);
        m_HostButton.onClick.AddListener(StartHost);
        m_ClientButton.onClick.AddListener(StartClient);
        m_Buttons = new Button[] { m_ServerButton, m_HostButton, m_ClientButton };
    }

    void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        DisableAndHighlightButtons(m_ServerButton);
    }

    void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        DisableAndHighlightButtons(m_HostButton);
    }

    void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        DisableAndHighlightButtons(m_ClientButton);
    }

    void DisableAndHighlightButtons(Button buttonToHighlight)
    {
        foreach (var button in m_Buttons)
        {
            ColorBlock colors = button.colors;
            colors.disabledColor = button == buttonToHighlight ? m_HighlightedButtonColor
                                                               : m_DisabledButtonColor;
            button.colors = colors;
            button.interactable = false;
        }
    }
}
