using System;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(NetworkManager))]
[DisallowMultipleComponent]
public class NetworkManagerHud : MonoBehaviour
{
    NetworkManager m_NetworkManager;

    UnityTransport m_Transport;

    GUIStyle m_LabelTextStyle;

    // This is needed to make the port field more convenient. GUILayout.TextField is very limited and we want to be able to clear the field entirely so we can't cache this as ushort.
    string m_PortString = "7777";
    string m_ConnectAddress = "127.0.0.1";

    public Vector2 DrawOffset = new Vector2(10, 10);

    public Color LabelColor = Color.black;
    
    [SerializeField]
    UIDocument m_MainMenuUIDocument;

    [SerializeField]
    UIDocument m_InGameUIDocument;
    
    VisualElement m_MainMenuRootVisualElement;

    VisualElement m_InGameRootVisualElement;
    
    Button m_HostButton;
    
    Button m_ServerButton;
    
    Button m_ClientButton;

    Button m_ShutdownButton;
    
    TextField m_IPAddressField;
    
    TextField m_PortField;

    TextElement m_HostPortNumber;

    void Awake()
    {
        // Only cache networking manager but not transport here because transport could change anytime.
        m_NetworkManager = GetComponent<NetworkManager>();
        
        m_NetworkManager.OnClientConnectedCallback += OnOnClientConnectedCallback;
        m_NetworkManager.OnClientDisconnectCallback += OnOnClientDisconnectCallback;
        
        m_LabelTextStyle = new GUIStyle(GUIStyle.none);
        
        m_MainMenuRootVisualElement = m_MainMenuUIDocument.rootVisualElement;
        m_HostButton = m_MainMenuRootVisualElement.Q<Button>("HostButton");
        m_ServerButton = m_MainMenuRootVisualElement.Q<Button>("ServerButton");
        m_ClientButton = m_MainMenuRootVisualElement.Q<Button>("ClientButton");
        m_IPAddressField = m_MainMenuRootVisualElement.Q<TextField>("IPAddressField");
        m_PortField = m_MainMenuRootVisualElement.Q<TextField>("PortField");

        m_InGameRootVisualElement = m_InGameUIDocument.rootVisualElement;
        m_ShutdownButton = m_InGameRootVisualElement.Q<Button>("ShutdownButton");
        m_HostPortNumber = m_InGameRootVisualElement.Q<TextElement>("HostPortNumber");
        
        m_IPAddressField.value = m_ConnectAddress;
        m_PortField.value = m_PortString;
        
        m_HostButton.clickable.clickedWithEventInfo += HostButtonClicked;
        m_ServerButton.clickable.clickedWithEventInfo += ServerButtonClicked;
        m_ClientButton.clickable.clickedWithEventInfo += ClientButtonClicked;
        
        ShowMainMenuUI(true);
        ShowInGameUI(false);
    }

    void OnOnClientConnectedCallback(ulong obj)
    {
        ShowMainMenuUI(false);
        ShowInGameUI(true);
    }

    void OnOnClientDisconnectCallback(ulong clientId)
    {
        if ((m_NetworkManager.IsServer && clientId != m_NetworkManager.ServerClientId))
        {
            return;
        }
        ShowMainMenuUI(true);
        ShowInGameUI(false);
    }


    void OnGUI()
    {
        m_LabelTextStyle.normal.textColor = LabelColor;

        m_Transport = (UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport;

        GUILayout.BeginArea(new Rect(DrawOffset, new Vector2(200, 200)));

        if (IsRunning(m_NetworkManager))
        {
            DrawStatusGUI();
        }
        else
        {
            DrawConnectGUI();
        }

        GUILayout.EndArea();
    }

    void DrawConnectGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(10);
        GUILayout.Label("Address", m_LabelTextStyle);
        GUILayout.Label("Port", m_LabelTextStyle);

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        m_ConnectAddress = GUILayout.TextField(m_ConnectAddress);
        m_PortString = GUILayout.TextField(m_PortString);
        if (ushort.TryParse(m_PortString, out ushort port))
        {
            m_Transport.SetConnectionData(m_ConnectAddress, port);
        }
        else
        {
            m_Transport.SetConnectionData(m_ConnectAddress, 7777);
        }

        GUILayout.EndHorizontal();

        if (GUILayout.Button("Host (Server + Client)"))
        {
            m_NetworkManager.StartHost();
        }

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Server"))
        {
            m_NetworkManager.StartServer();
        }

        if (GUILayout.Button("Client"))
        {
            m_NetworkManager.StartClient();
        }

        GUILayout.EndHorizontal();
    }

    void DrawStatusGUI()
    {
        if (m_NetworkManager.IsServer)
        {
            var mode = m_NetworkManager.IsHost ? "Host" : "Server";
            GUILayout.Label($"{mode} active on port: {m_Transport.ConnectionData.Port.ToString()}", m_LabelTextStyle);
        }
        else
        {
            if (m_NetworkManager.IsConnectedClient)
            {
                GUILayout.Label($"Client connected {m_Transport.ConnectionData.Address}:{m_Transport.ConnectionData.Port.ToString()}", m_LabelTextStyle);
            }
        }

        if (GUILayout.Button("Shutdown"))
        {
            m_NetworkManager.Shutdown();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IsRunning(NetworkManager networkManager) => networkManager.IsServer || networkManager.IsClient;
    
    void ClientButtonClicked(EventBase obj)
    {
        
    }

    void ServerButtonClicked(EventBase obj)
    {
        
    }

    void HostButtonClicked(EventBase obj)
    {
        
    }
    
    void ShowMainMenuUI(bool visible)
    {
        m_MainMenuUIDocument.enabled = visible;
        Debug.Log($"main menu visibility {visible}");
    }

    void ShowInGameUI(bool visible)
    {
        m_InGameUIDocument.enabled = visible;
        Debug.Log($"in game visibility {visible}");
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
        
        m_NetworkManager.OnClientConnectedCallback -= OnOnClientConnectedCallback;
        m_NetworkManager.OnClientDisconnectCallback -= OnOnClientDisconnectCallback;
    }
}
