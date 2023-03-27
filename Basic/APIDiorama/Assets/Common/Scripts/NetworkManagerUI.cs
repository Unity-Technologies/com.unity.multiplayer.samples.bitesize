using System.Linq;
using System.Text.RegularExpressions;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Unity.Netcode.Samples.APIDiorama.Common
{

    public class NetworkManagerUI : MonoBehaviour
    {
        static readonly Regex s_IPRegex = new Regex("\\b(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(\\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}\\b");
        const string k_DefaultIP = "127.0.0.1";
        const string k_DefaultServerListenAddress = "0.0.0.0"; //note: this is not safe for real world usage and would limit you to IPv4-only addresses, but this goes out the scope of this sample.

        [SerializeField] Button m_ServerButton;
        [SerializeField] Button m_HostButton;
        [SerializeField] Button m_ClientButton;
        [SerializeField] Button m_DisconnectButton;
        [SerializeField] Button m_QuitSceneButton;
        [SerializeField] Color32 m_HighlightedButtonColor;
        [SerializeField] Color32 m_DisabledButtonColor;
        [SerializeField] string m_SelectionScreenSceneName;
        [SerializeField] GameObject m_ServerOnlyOverlay;
        [SerializeField] TMPro.TMP_InputField m_IPAddressInputField;
        [SerializeField] RectTransform m_LayoutToRebuild;
        Button[] m_Buttons;

        void Awake()
        {
            m_ServerButton.onClick.AddListener(StartServer);
            m_HostButton.onClick.AddListener(StartHost);
            m_ClientButton.onClick.AddListener(StartClient);
            m_DisconnectButton.onClick.AddListener(Disconnect);
            m_QuitSceneButton.onClick.AddListener(QuitScene);
            m_Buttons = new Button[] { m_ServerButton, m_HostButton, m_ClientButton };
            m_IPAddressInputField.text = k_DefaultIP;
            m_IPAddressInputField.onSubmit.AddListener(ValidateIP);
            m_IPAddressInputField.onEndEdit.AddListener(ValidateIP);
        }

        void Start()
        {
            SetButtonStateAndColor(m_DisconnectButton, false, false);
            m_ServerOnlyOverlay.gameObject.SetActive(false);
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_LayoutToRebuild); //nested layout groups need to be rebuilt at startup to work properly in UGUI.
        }

        void StartServer()
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.ServerListenAddress = k_DefaultServerListenAddress;
            NetworkManager.Singleton.StartServer();
            EnableAndHighlightButtons(m_ServerButton, false);
            SetButtonStateAndColor(m_DisconnectButton, false, true);
            m_ServerOnlyOverlay.gameObject.SetActive(true);
        }

        void StartHost()
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.ServerListenAddress = k_DefaultServerListenAddress;
            NetworkManager.Singleton.StartHost();
            EnableAndHighlightButtons(m_HostButton, false);
            SetButtonStateAndColor(m_DisconnectButton, false, true);
        }

        void StartClient()
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = m_IPAddressInputField.text;
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
            m_ServerOnlyOverlay.gameObject.SetActive(false);
        }

        void QuitScene()
        {
            Disconnect();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(m_SelectionScreenSceneName, LoadSceneMode.Single);
        }

        void OnSceneLoaded(Scene loadedScene, LoadSceneMode arg1)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (loadedScene.name == m_SelectionScreenSceneName)
            {
                if (NetworkManager.Singleton)
                {
                    Destroy(NetworkManager.Singleton.gameObject);
                }
            }
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

        void ValidateIP(string newIP)
        {
            if (string.IsNullOrEmpty(newIP) || !s_IPRegex.IsMatch(newIP))
            {
                Debug.LogError($"'{newIP}' is not a valid IP address, reverting to {k_DefaultIP}");
                m_IPAddressInputField.text = k_DefaultIP;
            }
        }

    }
}