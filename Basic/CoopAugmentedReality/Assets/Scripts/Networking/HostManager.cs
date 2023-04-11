using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HostManager : MonoBehaviour
{
	public static HostManager Instance { get; private set; }

	[Header("Multiplayer Settings")]
	[SerializeField] private int maxPlayerConnections = 2;
	private UnityTransport transport;
	public string JoinCode { get; private set; }

	[Header("UI Settings")]
	[SerializeField] private TMP_InputField joinCodeInput;

	[Header("Scene Settings")]
	[SerializeField] private string sceneName = "ARMainScene";

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

	public async void StartHost()
	{
		Allocation receivedAllocation;
		try
		{
			receivedAllocation = await RelayService.Instance.CreateAllocationAsync(maxPlayerConnections);
		}
		catch (Exception e)
		{
			Debug.LogError($"Creation of Relay Allocation failed: {e.Message}");
			throw;
		}

		Debug.Log($"HOST INFO: {receivedAllocation.ConnectionData[0]} {receivedAllocation.ConnectionData[1]}");
		Debug.Log($"HOST INFO: {receivedAllocation.AllocationId}");

		try
		{
			JoinCode = await RelayService.Instance.GetJoinCodeAsync(receivedAllocation.AllocationId);
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to retrieve Join Code from Relay Service: {e.Message}");
			throw;
		}

		joinCodeInput.text = JoinCode;
		joinCodeInput.readOnly = true;

		try
		{
			transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
			transport.SetHostRelayData(receivedAllocation.RelayServer.IpV4, (ushort)receivedAllocation.RelayServer.Port, receivedAllocation.AllocationIdBytes, receivedAllocation.Key, receivedAllocation.ConnectionData);
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to fetch Transport from Network Manager: {e.Message}");
			throw;
		}

		try
		{
			transport.SetHostRelayData(receivedAllocation.RelayServer.IpV4, (ushort)receivedAllocation.RelayServer.Port, receivedAllocation.AllocationIdBytes, receivedAllocation.Key, receivedAllocation.ConnectionData);
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to set Host Relay Data: {e.Message}");
			throw;
		}

		try
		{
			NetworkManager.Singleton.StartHost();
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to Start Network Host: {e.Message}");
			throw;
		}

		LoadMainScene();
	}


	public void LoadMainScene()
	{
		try
		{
			NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to load Main Scene {sceneName}: {e.Message}");
			throw;
		}
	}
}
