using System.Collections.Generic;
using System.Linq;
using System.Text;
#if MULTIPLAYER_TOOLS
using Unity.Multiplayer.Tools.NetStatsMonitor;
#endif
using Unity.Netcode;
using UnityEngine;

public class ManagementInput : NetworkBehaviour
{
#if MULTIPLAYER_TOOLS
    public RuntimeNetStatsMonitor RuntimeNetStatsMonitor;
#endif

    public GameObject SessionStateMarker;

    private NetworkVariable<bool> ObjectOwnerInstancesAreActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private bool m_PlayerTagsVisible = true;

    private void Awake()
    {
#if MULTIPLAYER_TOOLS
        RuntimeNetStatsMonitor?.gameObject.SetActive(false);
#endif
    }

    public override void OnNetworkSpawn()
    {
        ObjectOwnerColor.ToggleOwnerColorSphere(ObjectOwnerInstancesAreActive.Value);
        ObjectOwnerInstancesAreActive.OnValueChanged += OnObjectOwnerInstancesAreActiveChanged;
        if (InterestOverlayHandler.Singleton)
        {
            InterestOverlayHandler.Singleton.SetPlayerColor(PlayerColor.GetPlayerColor(NetworkManager.LocalClientId));
        }
        base.OnNetworkSpawn();
    }

    private void OnObjectOwnerInstancesAreActiveChanged(bool previous, bool newValue)
    {
        ObjectOwnerColor.ToggleOwnerColorSphere(newValue);
    }

    private void Update()
    {
        if (!IsSpawned || !Application.isFocused)
        {
            return;
        }

        // Toggles object ownership (client key colored) spheres
        if (Input.GetKeyDown(KeyCode.O))
        {
            ToggleAsteroidColorsRpc();
        }

        // Toggles runtime console log visibility
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            NetworkManagerHelper.Instance.ConsoleLogVisible = !NetworkManagerHelper.Instance.ConsoleLogVisible;
        }

        // Toggles map overview of everyting
        if (Input.GetKeyDown(KeyCode.M) && InterestOverlayHandler.Singleton != null)
        {
            // Cycles through interest view types
            InterestOverlayHandler.Singleton.NextState();
        }

        // Map overview Zoom in 
        if (Input.GetKey(KeyCode.LeftBracket))
        {
            InterestOverlayHandler.Singleton.ChangeInterestZoom(true);
        }

        // Map overview Zoom out 
        if (Input.GetKey(KeyCode.RightBracket))
        {
            InterestOverlayHandler.Singleton.ChangeInterestZoom(false);
        }

        if (Input.GetKeyDown(KeyCode.H) && SessionStateMarker != null)
        {
            var marker = Instantiate(SessionStateMarker);
            var localPlayer = NetworkManager.LocalClient.PlayerObject;
            marker.transform.position = localPlayer.transform.position + localPlayer.transform.forward * 5.0f;
            var markerNetworkObject = marker.GetComponent<NetworkObject>();
            marker.GetComponent<NetworkObject>().Spawn();
        }

        // For Debugging Purposes
        // Check SpawnObject Count
        if (Input.GetKeyDown(KeyCode.C) && !m_SpawnCountInProgress)
        {
            m_SpawnCountInProgress = true;
            m_RemoteSpawnCount.Clear();
            m_RemoteInvalidSpawnCount.Clear();
            m_SpawnCountForQuery = NetworkManager.SpawnManager.SpawnedObjects.Count;
            m_SpawnCountClientCount = NetworkManager.ConnectedClientsIds.Count;
            QuerySpawnCountRpc(m_SpawnCountForQuery);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            m_PlayerTagsVisible = !m_PlayerTagsVisible;
            foreach (var client in NetworkManager.ConnectedClientsList)
            {
                if (client.PlayerObject != null && client.PlayerObject.IsSpawned)
                {
                    var clientShip = client.PlayerObject.GetComponent<ShipController>();
                    clientShip.PlayerTagVisibility(m_PlayerTagsVisible);
                }
            }
        }

#if MULTIPLAYER_TOOLS
        // Toggle the RNSM tool
        if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.Tab))
        {
            RuntimeNetStatsMonitor?.gameObject.SetActive(!RuntimeNetStatsMonitor.gameObject.activeInHierarchy);
        }
#endif
    }

    private bool m_SpawnCountInProgress;
    private int m_SpawnCountForQuery;
    private int m_SpawnCountClientCount;
    private Dictionary<ulong, int> m_RemoteSpawnCount = new Dictionary<ulong, int>();
    private Dictionary<ulong, List<ulong>> m_RemoteInvalidSpawnCount = new Dictionary<ulong, List<ulong>>();

    [Rpc(SendTo.NotMe)]
    private void QuerySpawnCountRpc(int count, RpcParams rpcParams = default)
    {
        var localCount = NetworkManager.SpawnManager.SpawnedObjects.Count;
        if (count != localCount)
        {
            QuerySpawnCountResponseRpc(count, NetworkManager.SpawnManager.SpawnedObjects.Keys.ToArray(), RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
        }
        else
        {
            QuerySpawnCountResponseRpc(count, new ulong[0], RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void QuerySpawnCountResponseRpc(int count, ulong[] spawnedObjectIds, RpcParams rpcParams = default)
    {
        var senderId = rpcParams.Receive.SenderClientId;
        m_RemoteSpawnCount.Add(senderId, count);
        if (count != m_SpawnCountForQuery)
        {
            m_RemoteInvalidSpawnCount.Add(senderId, spawnedObjectIds.ToList());
        }
        else
        {
            NetworkManagerHelper.Instance.LogMessage($"[Client-{senderId}] Spawn count {count} matches the query spawn count {m_SpawnCountForQuery}.");
        }

        // Last response received
        if ((m_SpawnCountClientCount - 1) == m_RemoteSpawnCount.Count)
        {
            // Parse and display results for all responses
            var matchingClients = m_SpawnCountClientCount - m_RemoteInvalidSpawnCount.Count;
            NetworkManagerHelper.Instance.LogMessage($"[SpawnCount Check Complete] Clients Matching: {matchingClients} | Clients Not Matching: {m_RemoteInvalidSpawnCount.Count}");
            var builder = new StringBuilder();
            var localSpawnObjects = NetworkManager.SpawnManager.SpawnedObjects;
            foreach (var clientEntry in m_RemoteInvalidSpawnCount)
            {
                builder.AppendLine($"[Client-{clientEntry.Key}] Mismatched spawn objects:");
                builder.Append("Missing Local Objects: ");
                // Check for remote vs local missing NetworkObjects
                foreach (var networkObjectId in clientEntry.Value)
                {
                    if (!localSpawnObjects.ContainsKey(networkObjectId))
                    {
                        builder.Append($"[{networkObjectId}]");
                    }
                }
                builder.Append($"\n");
                foreach (var spawnedObject in localSpawnObjects)
                {
                    if (!clientEntry.Value.Contains(spawnedObject.Key))
                    {
                        builder.AppendLine($"{spawnedObject.Value.name} is missing on Client-{clientEntry.Key}!");
                    }
                }
                Debug.Log(builder.ToString());
                builder.Clear();
            }

            m_SpawnCountInProgress = false;
        }
    }

    private void ToggleOwnershipSpheres()
    {
        var areActive = ObjectOwnerInstancesAreActive.Value;
        areActive = !areActive;
        ObjectOwnerInstancesAreActive.Value = areActive;
    }

    [Rpc(SendTo.Authority)]
    private void ToggleAsteroidColorsRpc()
    {
        if (IsOwner)
        {
            ToggleOwnershipSpheres();
        }
    }
}
