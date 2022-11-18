using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] Button btnServer;
    [SerializeField] Button btnHost;
    [SerializeField] Button btnClient;

    void Awake()
    {
        btnServer.onClick.AddListener(StartServer);
        btnHost.onClick.AddListener(StartHost);
        btnClient.onClick.AddListener(StartClient);
    }

    void StartServer()
    {
        NetworkManager.Singleton.StartServer();
    }

    void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }
}
