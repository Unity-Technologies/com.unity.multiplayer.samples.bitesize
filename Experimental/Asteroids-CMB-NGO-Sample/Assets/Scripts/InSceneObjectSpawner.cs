using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Generic in-scene placed object spawner.
/// Use this to dynamically spawn a network prefab via an in-scene placed NetworkObject.
/// Useful for when scene management is disabled.
/// </summary>
public class InSceneObjectSpawner : NetworkBehaviour
{
    public GameObject ObjectToSpawn;

    public bool DestroyNonOwnerInstance = false;

    private NetworkObject Instance;

    private NetworkVariable<bool> HasSpawnedInstance = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        GetComponent<NetworkObject>().SetSceneObjectStatus(true);
        if (Instance == null)
        {
            var goInstance = Instantiate(ObjectToSpawn);
            Instance = goInstance.GetComponent<NetworkObject>();
            Instance.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable);
            Instance.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
            Instance.DontDestroyWithOwner = true;
            Instance.SetSceneObjectStatus();
            Instance.gameObject.SetActive(false);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsSessionOwner)
        {
            if (!HasSpawnedInstance.Value)
            {
                if(!NetworkManager.CMBServiceConnection && Instance == null)
                {
                    var goInstance = Instantiate(ObjectToSpawn);
                    Instance = goInstance.GetComponent<NetworkObject>();
                    Instance.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable);
                    Instance.DontDestroyWithOwner = true;
                    Instance.SetSceneObjectStatus();
                }
                else
                {
                    Instance.gameObject.SetActive(true);
                }
                NetworkManagerHelper.Instance.LogMessage($"[{gameObject.name}][Is Scene Owner] Spawning {ObjectToSpawn.name} pool.");
                Instance.Spawn(true);
                HasSpawnedInstance.Value = true;
            }
            else if (NetworkManager.CMBServiceConnection)
            {
                if (DestroyNonOwnerInstance)
                {
                    Destroy(Instance);
                }
                NetworkManagerHelper.Instance.LogMessage($"[{gameObject.name}][Is Scene Owner] Skipping spawn of {ObjectToSpawn.name} pool as it is already spawned.");
            }
        }
        else
        {
            Destroy(Instance);
            Instance = null;
        }
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        if (!NetworkManager.CMBServiceConnection && HasAuthority)
        {
            HasSpawnedInstance.Value = false;
        }
        base.OnNetworkDespawn();
    }
}
