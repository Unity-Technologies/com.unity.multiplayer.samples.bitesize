using System;
using System.Net;
using Tanks.Networking;
using Tanks.UI;
using UnityEngine;
using UnityEngine.UI;

public class JoinGameDebugModal : Modal
{

    [SerializeField]
    protected InputField m_IpAddressInput;
    [SerializeField]
    protected InputField m_PortInput;
    [SerializeField]
    protected Button m_JoinButton;

    string m_IPAddress;
    bool m_HasValidIP = true;
    int m_Port;
    bool m_HasValidPort = true;
    
    private MainMenuUI m_MenuUi;
    private NetworkManager m_NetManager;

    void Awake()
    {
        m_MenuUi = MainMenuUI.s_Instance;
        m_NetManager = NetworkManager.s_Instance;
        
        if (m_IpAddressInput != null)
        {
            m_IpAddressInput.onValueChanged.AddListener(OnIpAddressChanged);
            OnIpAddressChanged(m_IpAddressInput.text);
        }

        if (m_PortInput != null)
        {
            m_PortInput.onValueChanged.AddListener(OnPortChanged);
            OnPortChanged(m_PortInput.text);
        }
    }

    private void OnIpAddressChanged(string value)
    {
        if (IPAddress.TryParse(value, out IPAddress address))
        {
            m_IPAddress = address.ToString();
            m_HasValidIP = true;
        }
        else
        {
            m_IPAddress = value;
            m_HasValidIP = false;
        }
        
        RefreshJoinButton();
    }

    private void OnPortChanged(string value)
    {
        m_HasValidPort = int.TryParse(value, out m_Port);
        RefreshJoinButton();
    }
    
    public void OnJoinClick()
    {
        m_NetManager.JoinMultiplayerGame(m_IPAddress, m_Port);
        CloseModal();
        m_MenuUi.ShowLobbyPanel();
    }

    void RefreshJoinButton()
    {
        m_JoinButton.interactable = m_HasValidIP && m_HasValidPort;
    }
}
