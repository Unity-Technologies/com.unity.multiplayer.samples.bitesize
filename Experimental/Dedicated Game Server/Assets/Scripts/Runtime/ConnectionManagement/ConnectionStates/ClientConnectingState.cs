using System;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime.ConnectionManagement
{
    class ClientConnectingState : OnlineState
    {
        string m_IPAddress;
        ushort m_Port;
        
        public void Configure(string iPAddress, ushort port)
        {
            m_IPAddress = iPAddress;
            m_Port = port;
        }
        
        public override void Enter()
        {
            ConnectionManager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.Connecting });
            ConnectClient();
        }

        public override void Exit() { }

        public override void OnClientConnected(ulong _)
        {
            ConnectionManager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.Success });
            ConnectionManager.ChangeState(ConnectionManager.m_ClientConnected);
        }

        public override void OnClientDisconnect(ulong _)
        {
            // client ID is for sure ours here
            StartingClientFailed();
        }
        
        void StartingClientFailed()
        {
            var disconnectReason = ConnectionManager.NetworkManager.DisconnectReason;
            if (string.IsNullOrEmpty(disconnectReason))
            {
                ConnectionManager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.StartClientFailed });
            }
            else
            {
                var connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                ConnectionManager.EventManager.Broadcast(new ConnectionEvent { status = connectStatus });
            }
            ConnectionManager.ChangeState(ConnectionManager.m_Offline);
        }
        
        void ConnectClient()
        {
            try
            {
                // Setup NGO with current connection method
                SetConnectionPayload(GetPlayerId());
                var utp = (UnityTransport)ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
                utp.SetConnectionData(m_IPAddress, m_Port);

                // NGO's StartClient launches everything
                if (!ConnectionManager.NetworkManager.StartClient())
                {
                    throw new Exception("NetworkManager StartClient failed");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error connecting client, see following exception");
                Debug.LogException(e);
                StartingClientFailed();
                throw;
            }
        }

        void SetConnectionPayload(string playerId)
        {
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                playerId = playerId,
                applicationVersion = Application.version
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            ConnectionManager.NetworkManager.NetworkConfig.ConnectionData = payloadBytes;
        }

        /// Using authentication, this makes sure your session is associated with your account and not your device. This means you could reconnect 
        /// from a different device for example. A playerId is also a bit more permanent than player prefs. In a browser for example, 
        /// player prefs can be cleared as easily as cookies.
        /// The forked flow here is for debug purposes and to make UGS optional in Boss Room. This way you can study the sample without 
        /// setting up a UGS account. It's recommended to investigate your own initialization and IsSigned flows to see if you need 
        /// those checks on your own and react accordingly. We offer here the option for offline access for debug purposes, but in your own game you
        /// might want to show an error popup and ask your player to connect to the internet.
        string GetPlayerId()
        {
            string profile = ProfileManager.Singleton.Profile;
            
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                return ClientPrefs.GetGuid() + profile;
            }

            return AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : ClientPrefs.GetGuid() + profile;
        }
    }
}
