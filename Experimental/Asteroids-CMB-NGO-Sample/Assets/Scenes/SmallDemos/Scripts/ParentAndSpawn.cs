using Unity.Netcode;
using UnityEngine;

public class ParentAndSpawn : MonoBehaviour
{
    public GameObject ChildToParent;
    public NetworkManager NetworkManager;


    [HideInInspector]
    public NetworkObject ChildInstance;

    private void Awake()
    {
        NetworkManager.OnClientStarted += OnClientStarted;
        NetworkManager.OnClientStopped += OnClientStopped;
    }

    private void OnClientStarted()
    {        
        NetworkManager.OnConnectionEvent += OnConnectionEvent;
    }

    private void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData eventData)
    {
        if (networkManager == NetworkManager && eventData.EventType == ConnectionEvent.ClientConnected && eventData.ClientId == NetworkManager.LocalClientId) 
        {
            NetworkManager.OnConnectionEvent -= OnConnectionEvent;
            SpawnChild();
        }
    }

    private void OnClientStopped(bool obj)
    {
        NetworkManager.OnConnectionEvent -= OnConnectionEvent;
    }

    private void SpawnChild()
    {
        if (ChildToParent == null)
        {
            NetworkManagerHelper.Instance.LogMessage($"[Cannot Spawn] No child network prefab is assigned to {name}!");
            return;
        }

        var instance = Instantiate(ChildToParent);
        ChildInstance = instance.GetComponent<NetworkObject>();
        if (ChildInstance != null)
        {
            ChildInstance.Spawn(true);
        }
    }
}
