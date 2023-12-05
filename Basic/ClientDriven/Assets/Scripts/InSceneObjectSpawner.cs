using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class InSceneObjectSpawner : NetworkBehaviour
{
    public GameObject ObjectToSpawn;

    public bool DestroyNonOwnerInstance = false;

    private NetworkObject Instance;

    private NetworkVariable<bool> HasSpawnedInstance = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        GetComponent<NetworkObject>().SetSceneObjectStatus(true);
    }

    public override void OnNetworkSpawn()
    {
        if (IsSceneOwner)
        {
            if (!HasSpawnedInstance.Value)
            {
                var goInstance = Instantiate(ObjectToSpawn);
                goInstance.transform.position = transform.position;
                goInstance.transform.rotation = transform.rotation;
                goInstance.transform.localScale = transform.localScale;
                Instance = goInstance.GetComponent<NetworkObject>();
                Instance.SetSceneObjectStatus(true);
                Instance.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable);
                Instance.DontDestroyWithOwner = true;
                Instance.SetSceneObjectStatus();
                Debug.Log($"[{gameObject.name}][Is Scene Owner] Spawning {ObjectToSpawn.name} pool.");
                Instance.SpawnWithOwnership(NetworkManager.LocalClientId, false);
                
                HasSpawnedInstance.Value = true;
            }
            else
            {
                //if (DestroyNonOwnerInstance)
                //{
                //    Destroy(Instance);
                //}
                Debug.Log($"[{gameObject.name}][Is Scene Owner] Skipping spawn of {ObjectToSpawn.name} pool as it is already spawned.");
            }
        }
        else
        {
            //if (DestroyNonOwnerInstance)
            //{
            //    Destroy(Instance);
            //}
            Debug.Log($"[{gameObject.name}] Non-scene owner skipping spawn of {ObjectToSpawn.name} pool.");
        }
        base.OnNetworkSpawn();
    }
}
