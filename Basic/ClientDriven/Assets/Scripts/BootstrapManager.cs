using UnityEngine;

namespace Unity.Netcode.Samples
{
    /// <summary>
    /// Class to display helper buttons and status labels on the GUI, as well as buttons to start host/client/server.
    /// Once a connection has been established to the server, the local player can be teleported to random positions via a GUI button.
    /// </summary>
    public class BootstrapManager : MonoBehaviour
    {
        public static string IPToConnectTo = "127.0.0.1";
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));

            var networkManager = NetworkManager.Singleton;
            if (!networkManager.IsClient && !networkManager.IsServer)
            {
                IPToConnectTo = GUILayout.TextField(IPToConnectTo);
                if (GUILayout.Button("Host"))
                {
                    (networkManager.NetworkConfig.NetworkTransport as UnityTransport).SetConnectionData(IPToConnectTo, 9998);
                    networkManager.StartHost();
                }

                if (GUILayout.Button("Client"))
                {
                    (networkManager.NetworkConfig.NetworkTransport as UnityTransport).SetConnectionData(IPToConnectTo, 9998);
                    networkManager.StartClient();
                }

                if (GUILayout.Button("Server"))
                {
                    (networkManager.NetworkConfig.NetworkTransport as UnityTransport).SetConnectionData(IPToConnectTo, 9998);
                    networkManager.StartServer();
                }
            }

            GUILayout.EndArea();
        }
    }
}
