using UnityEngine;

namespace Unity.Netcode.Samples
{
    /// <summary>
    /// Class to manage UI to start host/client/server.
    /// </summary>
    public class BootstrapManager : MonoBehaviour
    {
        [SerializeField]
        GameObject m_ConnectionPanel;

        public void StartHost()
        {
            NetworkManager.Singleton.StartHost();
            m_ConnectionPanel.SetActive(false);
        }

        public void StartClient()
        {
            NetworkManager.Singleton.StartClient();
            m_ConnectionPanel.SetActive(false);
        }

        public void StartServer()
        {
            NetworkManager.Singleton.StartServer();
            m_ConnectionPanel.SetActive(false);
        }
    }
}
