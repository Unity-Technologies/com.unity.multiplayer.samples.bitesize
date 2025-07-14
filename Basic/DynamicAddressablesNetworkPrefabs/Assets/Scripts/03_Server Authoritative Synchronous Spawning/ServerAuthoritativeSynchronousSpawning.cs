using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;

namespace Game.ServerAuthoritativeSynchronousSpawning
{
    /// <summary>
    /// A dynamic prefab loading use-case where the server instructs all clients to load a single network prefab, and
    /// will only invoke a spawn once all clients have successfully completed their respective loads of said prefab. The
    /// server will initially send a ClientRpc to all clients, begin loading the prefab on the server, will await
    /// acknowledgement of a load via ServerRpcs from each client, and will only spawn the prefab over the network once
    /// it has received an acknowledgement from every client, within m_SynchronousSpawnTimeoutTimer seconds.
    /// </summary>
    /// <remarks>
    /// This use-case is recommended for scenarios where you'd want to guarantee the same world version across all
    /// connected clients. Since the server will wait until all clients have loaded the same dynamic prefab, the spawn
    /// of said dynamic prefab will be synchronous. Thus, we recommend using this technique for spawning game-changing
    /// gameplay elements, assuming you'd want all clients to be able to interact with said gameplay elements from the
    /// same point forward. For example, you wouldn't want to have an enemy only be visible (network side and/or
    /// visually) to some clients and not others -- you'd want to delay the enemy's spawn until all clients have
    /// dynamically loaded it and are able to see it before spawning it server side.
    /// </remarks>
    public sealed class ServerAuthoritativeSynchronousSpawning : NetworkBehaviour
    {
        [SerializeField]
        NetworkManager m_NetworkManager;

        [SerializeField] List<AssetReferenceGameObject> m_DynamicPrefabReferences;

        [SerializeField] InGameUI m_InGameUI;

        const int k_MaxConnectedClientCount = 4;

        const int k_MaxConnectPayload = 1024;

        float m_SynchronousSpawnTimeoutTimer;

        int m_SynchronousSpawnAckCount = 0;

        void Start()
        {
            DynamicPrefabLoadingUtilities.Init(m_NetworkManager);

            // In the use-cases where connection approval is implemented, the server can begin to validate a user's
            // connection payload, and either approve or deny connection to the joining client.
            // Note: we will define a very simplistic connection approval below, which will effectively deny all
            // late-joining clients unless the server has not loaded any dynamic prefabs. You could choose to not define
            // a connection approval callback, but late-joining clients will have mismatching NetworkConfigs (and  
            // potentially different world versions if the server has spawned a dynamic prefab).
            m_NetworkManager.NetworkConfig.ConnectionApproval = true;

            // Here, we keep ForceSamePrefabs disabled. This will allow us to dynamically add network prefabs to Netcode
            // for GameObject after establishing a connection.
            m_NetworkManager.NetworkConfig.ForceSamePrefabs = false;

            // This is a simplistic use-case of a connection approval callback. To see how a connection approval should
            // be used to validate a user's connection payload, see the connection approval use-case, or the
            // APIPlayground, where all post-connection techniques are used in harmony.
            m_NetworkManager.ConnectionApprovalCallback += ConnectionApprovalCallback;

            // hooking up UI callbacks
            m_InGameUI.TrySpawnSynchronouslyButtonPressed += OnClickedTrySpawnSynchronously;
        }

        public override void OnDestroy()
        {
            m_NetworkManager.ConnectionApprovalCallback -= ConnectionApprovalCallback;
            if (m_InGameUI)
            {
                m_InGameUI.TrySpawnSynchronouslyButtonPressed -= OnClickedTrySpawnSynchronously;
            }
            DynamicPrefabLoadingUtilities.UnloadAndReleaseAllDynamicPrefabs();
            base.OnDestroy();
        }

        void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            Debug.Log("Client is trying to connect " + request.ClientNetworkId);
            var connectionData = request.Payload;
            var clientId = request.ClientNetworkId;

            if (clientId == m_NetworkManager.LocalClientId)
            {
                // allow the host to connect
                Approve();
                return;
            }

            // A sample-specific denial on clients after k_MaxConnectedClientCount clients have been connected
            if (m_NetworkManager.ConnectedClientsList.Count >= k_MaxConnectedClientCount)
            {
                ImmediateDeny();
                return;
            }

            if (connectionData.Length > k_MaxConnectPayload)
            {
                // If connectionData is too big, deny immediately to avoid wasting time on the server. This is intended as
                // a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage.
                ImmediateDeny();
                return;
            }

            // simple approval if the server has not loaded any dynamic prefabs yet
            if (DynamicPrefabLoadingUtilities.LoadedPrefabCount == 0)
            {
                Approve();
            }
            else
            {
                ImmediateDeny();
            }

            void Approve()
            {
                Debug.Log($"Client {clientId} approved");
                response.Approved = true;
                response.CreatePlayerObject = false; //we're not going to spawn a player object for this sample
            }

            void ImmediateDeny()
            {
                Debug.Log($"Client {clientId} denied connection");
                response.Approved = false;
                response.CreatePlayerObject = false;
            }
        }

        // invoked by UI
        void OnClickedTrySpawnSynchronously()
        {
            if (!m_NetworkManager.IsServer)
            {
                return;
            }

            TrySpawnSynchronously();
        }

        async void TrySpawnSynchronously()
        {
            var randomPrefab = m_DynamicPrefabReferences[Random.Range(0, m_DynamicPrefabReferences.Count)];
            await TrySpawnDynamicPrefabSynchronously(randomPrefab.AssetGUID, Random.insideUnitCircle * 5, Quaternion.identity);
        }

        /// <summary>
        /// This call attempts to spawn a prefab by it's addressable guid - it ensures that all the clients have loaded the prefab before spawning it,
        /// and if the clients fail to acknowledge that they've loaded a prefab - the spawn will fail.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        async Task<(bool Success, NetworkObject Obj)> TrySpawnDynamicPrefabSynchronously(string guid, Vector3 position, Quaternion rotation)
        {
            if (IsServer)
            {
                var assetGuid = new AddressableGUID()
                {
                    Value = guid
                };

                if (DynamicPrefabLoadingUtilities.IsPrefabLoadedOnAllClients(assetGuid))
                {
                    Debug.Log("Prefab is already loaded by all peers, we can spawn it immediately");
                    var obj = Spawn(assetGuid);
                    return (true, obj);
                }

                m_SynchronousSpawnAckCount = 0;
                m_SynchronousSpawnTimeoutTimer = 0;

                Debug.Log("Loading dynamic prefab on the clients...");
                LoadAddressableClientRpc(assetGuid);

                // server is starting to load a prefab, update UI
                m_InGameUI.ClientLoadedPrefabStatusChanged(NetworkManager.ServerClientId, assetGuid.GetHashCode(), "Undefined", InGameUI.LoadStatus.Loading);

                //load the prefab on the server, so that any late-joiner will need to load that prefab also
                await DynamicPrefabLoadingUtilities.LoadDynamicPrefab(assetGuid, m_InGameUI.ArtificialDelayMilliseconds);

                // server loaded a prefab, update UI with the loaded asset's name
                DynamicPrefabLoadingUtilities.TryGetLoadedGameObjectFromGuid(assetGuid, out var loadedGameObject);
                m_InGameUI.ClientLoadedPrefabStatusChanged(NetworkManager.ServerClientId, assetGuid.GetHashCode(), loadedGameObject.Result.name, InGameUI.LoadStatus.Loaded);

                var requiredAcknowledgementsCount = IsHost ? m_NetworkManager.ConnectedClients.Count - 1 :
                    m_NetworkManager.ConnectedClients.Count;

                while (m_SynchronousSpawnTimeoutTimer < m_InGameUI.NetworkSpawnTimeoutSeconds)
                {
                    if (m_SynchronousSpawnAckCount >= requiredAcknowledgementsCount)
                    {
                        Debug.Log($"All clients have loaded the prefab in {m_SynchronousSpawnTimeoutTimer} seconds, spawning the prefab on the server...");
                        var obj = Spawn(assetGuid);
                        return (true, obj);
                    }

                    m_SynchronousSpawnTimeoutTimer += Time.deltaTime;
                    await Task.Yield();
                }

                // left to the reader: you'll need to be reactive to clients failing to load -- you should either have
                // the offending client try again or disconnect it after a predetermined amount of failed attempts
                Debug.LogError("Failed to spawn dynamic prefab - timeout");
                return (false, null);
            }

            return (false, null);

            NetworkObject Spawn(AddressableGUID assetGuid)
            {
                if (!DynamicPrefabLoadingUtilities.TryGetLoadedGameObjectFromGuid(assetGuid, out var prefab))
                {
                    Debug.LogWarning($"GUID {assetGuid} is not a GUID of a previously loaded prefab. Failed to spawn a prefab.");
                    return null;
                }
                var obj = Instantiate(prefab.Result, position, rotation).GetComponent<NetworkObject>();
                obj.Spawn();
                Debug.Log("Spawned dynamic prefab");

                // every client loaded dynamic prefab, their respective ClientUIs in case they loaded first
                foreach (var client in m_NetworkManager.ConnectedClients.Keys)
                {
                    m_InGameUI.ClientLoadedPrefabStatusChanged(client,
                        assetGuid.GetHashCode(),
                        prefab.Result.name,
                        InGameUI.LoadStatus.Loaded);
                }

                return obj;
            }
        }

        [ClientRpc]
        void LoadAddressableClientRpc(AddressableGUID guid, ClientRpcParams rpcParams = default)
        {
            if (!IsHost)
            {
                Load(guid);
            }

            async void Load(AddressableGUID assetGuid)
            {
                // loading prefab as a client, update UI
                m_InGameUI.ClientLoadedPrefabStatusChanged(m_NetworkManager.LocalClientId, assetGuid.GetHashCode(), "Undefined", InGameUI.LoadStatus.Loading);

                Debug.Log("Loading dynamic prefab on the client...");
                await DynamicPrefabLoadingUtilities.LoadDynamicPrefab(assetGuid, m_InGameUI.ArtificialDelayMilliseconds);
                Debug.Log("Client loaded dynamic prefab");

                DynamicPrefabLoadingUtilities.TryGetLoadedGameObjectFromGuid(assetGuid, out var loadedGameObject);
                m_InGameUI.ClientLoadedPrefabStatusChanged(m_NetworkManager.LocalClientId, assetGuid.GetHashCode(), loadedGameObject.Result.name, InGameUI.LoadStatus.Loaded);

                AcknowledgeSuccessfulPrefabLoadServerRpc(assetGuid.GetHashCode());
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void AcknowledgeSuccessfulPrefabLoadServerRpc(int prefabHash, ServerRpcParams rpcParams = default)
        {
            m_SynchronousSpawnAckCount++;
            Debug.Log($"Client acknowledged successful prefab load with hash: {prefabHash}");
            DynamicPrefabLoadingUtilities.RecordThatClientHasLoadedAPrefab(prefabHash,
                rpcParams.Receive.SenderClientId);

            // a quick way to grab a matching prefab reference's name via its prefabHash
            var loadedPrefabName = "Undefined";
            foreach (var prefabReference in m_DynamicPrefabReferences)
            {
                var prefabReferenceGuid = new AddressableGUID() { Value = prefabReference.AssetGUID };
                if (prefabReferenceGuid.GetHashCode() == prefabHash)
                {
                    // found the matching prefab reference
                    if (DynamicPrefabLoadingUtilities.LoadedDynamicPrefabResourceHandles.TryGetValue(
                            prefabReferenceGuid,
                            out var loadedGameObject))
                    {
                        // if it is loaded on the server, update the name on the ClientUI
                        loadedPrefabName = loadedGameObject.Result.name;
                    }
                    break;
                }
            }

            // client has successfully loaded a prefab, update UI
            m_InGameUI.ClientLoadedPrefabStatusChanged(rpcParams.Receive.SenderClientId, prefabHash, loadedPrefabName, InGameUI.LoadStatus.Loaded);
        }
    }
}
