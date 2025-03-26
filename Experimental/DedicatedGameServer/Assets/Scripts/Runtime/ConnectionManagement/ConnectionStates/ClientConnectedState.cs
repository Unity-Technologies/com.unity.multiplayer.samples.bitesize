using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to a connected client. When being disconnected, transitions to the Offline state.
    /// </summary>
    class ClientConnectedState : OnlineState
    {
        public override void Enter() { }

        public override void Exit() { }

        public override void OnClientDisconnect(ulong clientId)
        {
            var disconnectReason = ConnectionManager.NetworkManager.DisconnectReason;
            var connectStatus = string.IsNullOrEmpty(disconnectReason) ||
                (disconnectReason != k_HostDisconnectReason && disconnectReason != k_ServerDisconnectReason)
                    ? ConnectStatus.GenericDisconnect
                    : ConnectStatus.StartClientFailed;
            ConnectionManager.EventManager.Broadcast(new ConnectionEvent { status = connectStatus });
            ConnectionManager.ChangeState(ConnectionManager.m_Offline);
        }
    }
}
