using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime.ConnectionManagement
{
    class ClientConnectedState : OnlineState
    {
        public override void Enter() { }

        public override void Exit() { }

        public override void OnClientDisconnect(ulong clientId)
        {
            var disconnectReason = ConnectionManager.NetworkManager.DisconnectReason;
            if (string.IsNullOrEmpty(disconnectReason))
            {
                ConnectionManager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.GenericDisconnect });
            }
            else
            {
                var connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                ConnectionManager.EventManager.Broadcast(new ConnectionEvent { status = connectStatus });
            }
            ConnectionManager.ChangeState(ConnectionManager.m_Offline);
        }
    }
}
