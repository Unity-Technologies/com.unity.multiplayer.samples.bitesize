using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ServerHostClientText : MonoBehaviour
{
    private Text m_DisplayText;

    private Color m_Color;
    private Color m_ColorAlpha;

    public void SetColor(Color color)
    {
        m_Color = color;
        m_ColorAlpha = color;
        m_ColorAlpha.a = 0.35f;

        m_LastFocusedValue = !Application.isFocused;
    }

    private void Awake()
    {
        m_DisplayText = GetComponent<Text>();
    }

    private void Start()
    {
        if (m_DisplayText != null)
        {
            m_DisplayText.text = string.Empty;
        }
        if (NetworkManager.Singleton.IsConnectedClient && NetworkManager.Singleton.IsListening)
        {
            UpdateClientText();
        }
        else
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        }

        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
    }

    private void UpdateClientText()
    {
        if (m_DisplayText != null)
        {
            SetColor(PlayerColor.GetPlayerColor(NetworkManager.Singleton.LocalClientId));
            var textLabel = "Client";
            var prefix = "";
            if (NetworkManager.Singleton.DistributedAuthorityMode)
            {
                prefix = "DA";
                if (!NetworkManager.Singleton.CMBServiceConnection && NetworkManager.Singleton.IsHost)
                {
                    textLabel = "Host";
                }
            }
            m_DisplayText.text = $"{prefix}{textLabel}-{NetworkManager.Singleton.LocalClientId}";
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
            UpdateClientText();
        }
    }

    private void OnClientStopped(bool wasHost)
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        if (m_DisplayText != null)
        {
            m_DisplayText.text = string.Empty;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        }
    }

    private bool m_LastFocusedValue;
    private void OnGUI()
    {
        if (!NetworkManager.Singleton.IsConnectedClient || m_LastFocusedValue == Application.isFocused)
        {
            return;
        }

        m_LastFocusedValue = Application.isFocused;

        if (m_LastFocusedValue)
        {
            m_DisplayText.color = m_Color;
        }
        else
        {
            m_DisplayText.color = m_ColorAlpha;
        }
    }
}
