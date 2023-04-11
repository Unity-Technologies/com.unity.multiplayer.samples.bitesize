using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class ClientManager : MonoBehaviour
{
	public static ClientManager Instance { get; private set; }

	//[Header("Multiplayer Settings")]
	private UnityTransport transport;
	public string JoinCode { get; set; }

	[Header("UI Settings")]
	[SerializeField] private TMP_InputField joinCodeInput;

	void Awake()
    {
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
		}
        else
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
    }

    public async void StartClient()
    {
		joinCodeInput.readOnly = true;
		JoinCode = joinCodeInput.text;

		JoinAllocation joinAllocation;
		try
		{
			joinAllocation = await RelayService.Instance.JoinAllocationAsync(JoinCode);
		}
		catch (Exception e)
		{
			Debug.LogError($"Retrieval of Relay JoinAllocation for Join Code {JoinCode} failed: {e.Message}");
			throw;
		}

		Debug.Log($"CLIENT INFO: {joinAllocation.ConnectionData[0]} {joinAllocation.ConnectionData[1]}");
		Debug.Log($"HOST INFO: {joinAllocation.HostConnectionData[0]} {joinAllocation.HostConnectionData[1]}");
		Debug.Log($"CLIENT INFO: {joinAllocation.AllocationId}");

		try
		{
			transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
			transport.SetClientRelayData(joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port, joinAllocation.AllocationIdBytes, joinAllocation.Key, joinAllocation.ConnectionData, joinAllocation.HostConnectionData);
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to fetch Transport from Network Manager for Client: {e.Message}");
			throw;
		}

		try
		{
			NetworkManager.Singleton.StartClient();
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to Start Network Client: {e.Message}");
			throw;
		}
    }
}
