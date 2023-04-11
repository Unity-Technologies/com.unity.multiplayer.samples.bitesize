//using ParrelSync;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
//using Unity.Services.Lobbies;
//using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyMMConnection : MonoBehaviour
{
	[SerializeField] private string gameSceneName;
	[SerializeField] private GameObject _canvas;
	[SerializeField] private GameObject _startGameCanvas;
	[SerializeField] private GameObject _startGameButton;
	[SerializeField] private NetworkManager nwkMgr;

	//private Lobby _connectedLobby;
	//private QueryResponse _lobbies;
	//private UnityTransport _transport;
	//private const string _joinCodeKey = "j";
	//private string _playerId;

	//private void Awake() => _transport = FindObjectOfType<UnityTransport>();

	//public async void CreateOrJoinLobby()
	//{

	//	await Authenticate();

	//	_connectedLobby = await QuickJoinLobby() ?? await CreateLobby();

	//	// Disable Login Canvas
	//	_canvas.SetActive(!(_connectedLobby != null));
	//	// Enable Start Game Canvas
	//	_startGameCanvas.SetActive((_connectedLobby != null));
	//	if (nwkMgr.IsHost)
	//	{

	//		_startGameButton.SetActive((_connectedLobby != null));
	//	}
	//}

	//private async Task Authenticate()
	//{
	//	InitializationOptions options = new();

	//	//#if UNITY_EDITOR
	//	//        // Remove if you don't have ParrelSync installed.
	//	//        options.SetProfile(ParrelSync.ClonesManager.IsClone() ? ClonesManager.GetArgument() : "Primary");
	//	//#endif

	//	await UnityServices.InitializeAsync(options);
	//	await AuthenticationService.Instance.SignInAnonymouslyAsync();
	//	_playerId = AuthenticationService.Instance.PlayerId;
	//}

	//public async Task<Lobby> QuickJoinLobby()
	//{
	//	try
	//	{
	//		Lobby lobby = await Lobbies.Instance.QuickJoinLobbyAsync();

	//		JoinAllocation ja = await RelayService.Instance.JoinAllocationAsync(lobby.Data[_joinCodeKey].Value);

	//		SetTransformAsClient(ja);

	//		nwkMgr.StartClient();

	//		return lobby;
	//	}
	//	catch (Exception e)
	//	{
	//		Debug.Log($"No lobbies available via quick join. { e.Message}");
	//		return null;
	//	}
	//}

	//private void SetTransformAsClient(JoinAllocation joinAlloc)
	//{
	//	_transport.SetClientRelayData(joinAlloc.RelayServer.IpV4, (ushort)joinAlloc.RelayServer.Port, joinAlloc.AllocationIdBytes, joinAlloc.Key, joinAlloc.ConnectionData, joinAlloc.HostConnectionData);
	//}


	//public async Task<Lobby> CreateLobby()
	//{
	//	try
	//	{
	//		const int maxPlayers = 100;

	//		Allocation a = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
	//		string joinCode = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);


	//		CreateLobbyOptions options = new()
	//		{
	//			Data = new Dictionary<string, DataObject> { { _joinCodeKey, new DataObject(DataObject.VisibilityOptions.Public, joinCode) } }
	//		};


	//		Lobby lobby = await Lobbies.Instance.CreateLobbyAsync("Netcode Grave Walkers", maxPlayers, options);

	//		// Room KeepAlive - otherwise automatically shuts down after 30 seconds.
	//		StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));

	//		_transport.SetHostRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData);


	//		// May want to wait for the lobby to fill up here.
	//		nwkMgr.StartHost();
	//		return lobby;
	//	}
	//	catch (Exception e)
	//	{
	//		Debug.Log($"Failed to create a lobby. { e.Message}");
	//		return null;
	//	}
	//}




	//private void OnDestroy()
	//{
	//	try
	//	{
	//		StopAllCoroutines();
	//		if (_connectedLobby != null)
	//		{
	//			if (_connectedLobby.HostId == _playerId) Lobbies.Instance.DeleteLobbyAsync(_connectedLobby.Id);
	//			else Lobbies.Instance.RemovePlayerAsync(_connectedLobby.Id, _playerId);
	//		}
	//	}
	//	catch (Exception e)
	//	{
	//		Debug.Log($"Error closing down the lobby. { e.Message}");
	//	}
	//}

	//private static IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
	//{
	//	var delay = new WaitForSecondsRealtime(waitTimeSeconds);
	//	while (true)
	//	{
	//		Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
	//		yield return delay;
	//	}
	//}
}

