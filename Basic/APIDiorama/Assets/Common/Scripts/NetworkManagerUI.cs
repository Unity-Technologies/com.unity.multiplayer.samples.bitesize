using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] Button m_ServerButton;
    [SerializeField] Button m_HostButton;
    [SerializeField] Button m_ClientButton;
    [SerializeField] Button m_DisconnectButton;
    [SerializeField] Color32 m_HighlightedButtonColor;
    [SerializeField] Color32 m_DisabledButtonColor;
    Button[] m_Buttons;

    void Awake()
    {
        m_ServerButton.onClick.AddListener(StartServer);
        m_HostButton.onClick.AddListener(StartHost);
        m_ClientButton.onClick.AddListener(StartClient);
        m_DisconnectButton.onClick.AddListener(Disconnect);
        m_Buttons = new Button[] { m_ServerButton, m_HostButton, m_ClientButton };
    }

    void Start()
    {
        SetButtonStateAndColor(m_DisconnectButton, false, false);
    }

    void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        EnableAndHighlightButtons(m_ServerButton, false);
        SetButtonStateAndColor(m_DisconnectButton, false, true);
    }

    void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        EnableAndHighlightButtons(m_HostButton, false);
        SetButtonStateAndColor(m_DisconnectButton, false, true);
    }

    void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        EnableAndHighlightButtons(m_ClientButton, false);
        SetButtonStateAndColor(m_DisconnectButton, false, true);
    }

    void Disconnect()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            foreach (var item in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.ToList())
            {
                item.Despawn(false);
            }
        }
        NetworkManager.Singleton.Shutdown();
        EnableAndHighlightButtons(null, true);
        SetButtonStateAndColor(m_DisconnectButton, false, false);
    }

    void EnableAndHighlightButtons(Button buttonToHighlight, bool enable)
    {
        foreach (var button in m_Buttons)
        {
            SetButtonStateAndColor(button, button == buttonToHighlight, enable);
        }
    }

    void SetButtonStateAndColor(Button button, bool highlight, bool enable)
    {
        ColorBlock colors = button.colors;
        colors.disabledColor = highlight ? m_HighlightedButtonColor
                                         : m_DisabledButtonColor;
        button.colors = colors;
        button.interactable = enable;
    }
}
