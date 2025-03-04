using System;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
#if UNITY_SERVER
using Unity.Services.Authentication.Server;
#endif
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime.ConnectionManagement
{
    /// <summary>
    /// Simple IP connection setup with UTP
    /// </summary>
    partial class ConnectionMethodIP
    {
        public override void SetupServerConnection()
        {
            SetConnectionPayload();
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(m_Ipaddress, m_Port);
        }

        public override Task ConnectServerAsync()
        {
            if (!m_ConnectionManager.NetworkManager.StartServer())
            {
                throw new Exception("NetworkManager StartServer failed");
            }
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// UTP's Relay connection setup using the Session integration
    /// </summary>
    partial class ConnectionMethodMatchmaker
    {
        public override void SetupServerConnection()
        {
            // nothing to set up here
        }

#if UNITY_SERVER && !UNITY_EDITOR
        public override async Task ConnectServerAsync()
        {
            await MatchmakerHandler.Instance.ConnectToDedicatedGameServer();
        }
#else
        public override Task ConnectServerAsync()
        {
            throw new NotImplementedException("Client should not be invoking ConnectServerAsync");
        }
#endif
    }
}
