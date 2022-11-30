using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
// using Unity.Services.Authentication;
// using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Unity.Netcode.Samples
{
    /// <summary>
    /// Class to display helper buttons and status labels on the GUI, as well as buttons to start host/client/server.
    /// Once a connection has been established to the server, the local player can be teleported to random positions via a GUI button.
    /// </summary>
    public class BootstrapManager : MonoBehaviour
    {
        const int k_MaxPlayers = 4;
        async void CreateHostRelay()
        {
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            var networkManager = NetworkManager.Singleton;

            var utp = networkManager.NetworkConfig.NetworkTransport as UnityTransport;
            utp.UseWebSockets = true;

            Allocation hostAllocation = await RelayService.Instance.CreateAllocationAsync(k_MaxPlayers, region: "northamerica-northeast1"); //null should select us-central automatically?
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocation.AllocationId);
            Debug.Log($"join code: {joinCode}, region: {hostAllocation.Region}");

            // utp.SetRelayServerData(new RelayServerData(hostAllocation, UnityTransport.RelayConnectionTypes.WSS)); // TODO edu here, what's the connection type for secure websocket
            utp.SetRelayServerData(new RelayServerData(hostAllocation, "wss")); // TODO edu here, what's the connection type for secure websocket

            networkManager.StartHost();
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));

            var networkManager = NetworkManager.Singleton;
            if (!networkManager.IsClient && !networkManager.IsServer)
            {
                if (GUILayout.Button("Host"))
                {
                    CreateHostRelay();
                }

                if (GUILayout.Button("Client"))
                {
                    var utp = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
                    utp.UseWebSockets = true;
                    NetworkManager.Singleton.StartClient();
                }

                if (GUILayout.Button("Server"))
                {
                    var utp = networkManager.NetworkConfig.NetworkTransport as UnityTransport;
                    utp.UseWebSockets = true;
                    utp.UseEncryption = false;
                    networkManager.StartServer();
                }
            }
            else
            {
                GUILayout.Label($"Mode: {(networkManager.IsHost ? "Host" : networkManager.IsServer ? "Server" : "Client")}");

                // "Random Teleport" button will only be shown to clients
                if (networkManager.IsClient)
                {
                    if (GUILayout.Button("Random Teleport"))
                    {
                        if (networkManager.LocalClient != null)
                        {
                            // Get `BootstrapPlayer` component from the player's `PlayerObject`
                            if (networkManager.LocalClient.PlayerObject.TryGetComponent(out BootstrapPlayer bootstrapPlayer))
                            {
                                // Invoke a `ServerRpc` from client-side to teleport player to a random position on the server-side
                                bootstrapPlayer.RandomTeleportServerRpc();
                            }
                        }
                    }
                }
            }

            GUILayout.EndArea();
        }
    }
}
