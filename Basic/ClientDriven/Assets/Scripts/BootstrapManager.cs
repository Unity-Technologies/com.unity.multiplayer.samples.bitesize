using System.Collections;
using UnityEngine;

namespace Unity.Netcode.Samples
{
    /// <summary>
    /// Class to display helper buttons and status labels on the GUI, as well as buttons to start host/client/server.
    /// Once a connection has been established to the server, the local player can be teleported to random positions via a GUI button.
    /// </summary>
    public class BootstrapManager : MonoBehaviour
    {
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));

            var networkManager = NetworkManager.Singleton;
            if (!networkManager.IsClient && !networkManager.IsServer)
            {
                if (GUILayout.Button("Host"))
                {
                    networkManager.StartHost();
                }

                if (GUILayout.Button("Client"))
                {
                    networkManager.StartClient();
                }

                if (GUILayout.Button("Server"))
                {
                    networkManager.StartServer();
                }
            }

            GUILayout.EndArea();
        }

        private void Awake()
        {
            NetworkManager.Singleton.ConnectionApprovalCallback += ConnectionApproval;
        }

        private const int k_MaxPlayers = 4;
        private int m_TotalPlayers;

        // private class NoMoreRoomMessage : INetworkMessage
        // {
        //     public int maxPlayerCount;
        //     public void Serialize(ref FastBufferWriter writer)
        //     {
        //         writer.WriteValueSafe(maxPlayerCount);
        //     }
        //
        //     public static void Receive(ref FastBufferReader reader, NetworkContext context)
        //     {
        //
        //         var message = new NoMoreRoomMessage();
        //
        //         reader.ReadValueSafe(out message.maxPlayerCount);
        //         Debug.Log("no more room :( "+message.maxPlayerCount);
        //
        //     }
        // }

        void ConnectionApproval(byte[] payload, ulong clientID, NetworkManager.ConnectionApprovedDelegate approvedDelegate)
        {
            bool approved = m_TotalPlayers < k_MaxPlayers;
            if (!approved)
            {
                // NetworkManager.Singleton.SendMessage<NoMoreRoomMessage>(new NoMoreRoomMessage() { maxPlayerCount = k_MaxPlayers }, NetworkDelivery.Reliable, clientID);
            }
            else
            {
                m_TotalPlayers++;
            }

            StartCoroutine(ApproveLater(approvedDelegate, approved));
        }

        public IEnumerator ApproveLater(NetworkManager.ConnectionApprovedDelegate approvedDelegate, bool approved)
        {
            yield return new WaitForSeconds(0.2f);
            approvedDelegate(true, null, approved, null, null);
        }
    }
}
