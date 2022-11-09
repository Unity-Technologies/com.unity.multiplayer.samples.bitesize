using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// In this scenario we are spawning prefabs that aren't known to the clients beforehand.
    ///
    /// NGO requires us to load the prefab before we spawn it.
    ///
    /// The inbuilt delay that is accessible through NetworkManager.NetworkConfig.SpawnTimeout is NOT MEANT to serve
    /// as a buffering time during which the clients attempt to catch up with the server's spawn command by loading the prefab and hoping that this load won't take more than the timeout.
    /// Such approach would inevitably lead to desyncs in production.
    /// 
    /// To be safe and to respect the NGO requirement of loading the prefab before spawning it, we:
    ///  - ensure that the clients acknowledge that they have loaded the prefab
    ///  - the server waits for a specified amount of time for the clients to acknowledge the load, and if all the clients are successful - it spawns the prefab
    ///  - otherwise the server runs out of time and the spawn is cancelled
    /// </summary>
    public sealed class SparseLoadingNoLatejoinSample : NetworkBehaviour
    {
        [SerializeField]
        Button m_StartGameButton;
        
        [SerializeField] AssetReferenceGameObject m_DynamicPrefabRef;
        
        [SerializeField] NetworkManager _networkManager;

        [SerializeField] float m_SpawnTimeoutInSeconds;
        
        bool m_IsGameStarted = false;
        int m_CountOfClientsThatLoadedThePrefab = 0;
        float m_SpawnTimeoutTimer = 0;
        Dictionary<string, GameObject> m_LoadedDynamicPrefabs = new Dictionary<string, GameObject>();
        
        void Awake()
        {
            _networkManager.NetworkConfig.ForceSamePrefabs = false;
            _networkManager.ConnectionApprovalCallback = ConnectionApprovalCallback;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                m_StartGameButton.onClick.AddListener(OnClickedSpawnButton);
            }
            else
            {
                m_StartGameButton.gameObject.SetActive(false);
            }
        }

        async void OnClickedSpawnButton()
        {
            if (!m_IsGameStarted)
            {
                m_IsGameStarted = true;
                m_StartGameButton.gameObject.SetActive(false);
                
                bool didManageToSpawn = await TrySpawnDynamicPrefab(m_DynamicPrefabRef.AssetGUID);

                if (!didManageToSpawn)
                {
                    m_StartGameButton.gameObject.SetActive(true);
                }
            }
        }

        void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            if (m_IsGameStarted)
            {
                response.Approved = false;
            }
            else
            {
                response.Approved = true;
            }
        }

        async Task<bool> TrySpawnDynamicPrefab(string guid)
        {
            if (IsServer)
            {
                if (m_LoadedDynamicPrefabs.ContainsKey(guid))
                {
                    Debug.Log("Prefab is already loaded by all peers, we can spawn it immediately");
                    await Spawn(guid);
                    return true;
                }
                
                m_CountOfClientsThatLoadedThePrefab = 0;
                m_SpawnTimeoutTimer = 0;
                
                Debug.Log("Loading dynamic prefab on the clients...");
                LoadAddressableClientRpc(m_DynamicPrefabRef.AssetGUID);

                int requiredAcknowledgementsCount = IsHost ? _networkManager.ConnectedClients.Count - 1 : _networkManager.ConnectedClients.Count;
                
                while (m_SpawnTimeoutTimer < m_SpawnTimeoutInSeconds)
                {
                    if (m_CountOfClientsThatLoadedThePrefab >= requiredAcknowledgementsCount)
                    {
                        Debug.Log($"All clients have loaded the prefab in {m_SpawnTimeoutTimer} seconds, spawning the prefab on the server...");
                        await Spawn(guid);
                        return true;
                    }
                    
                    m_SpawnTimeoutTimer += Time.deltaTime;
                    await Task.Yield();
                }
                
                Debug.LogError("Failed to spawn dynamic prefab - timeout");
                return false;
            }

            return false;

            async Task Spawn(string assetGuid)
            {
                var prefab = await EnsureDynamicPrefabIsLoaded(assetGuid);
                var obj = Instantiate(prefab).GetComponent<NetworkObject>();
                obj.SpawnWithOwnership(_networkManager.LocalClientId);
                Debug.Log("Spawned dynamic prefab");
            }
        }

        [ClientRpc]
        void LoadAddressableClientRpc(FixedString64Bytes guid, ClientRpcParams rpcParams = default)
        {
            if (!IsHost)
            {
                LoadPrefab(guid);
            }

            async void LoadPrefab(FixedString64Bytes guid)
            {
                Debug.Log("Loading dynamic prefab on the client...");
                await EnsureDynamicPrefabIsLoaded(guid.ToString());
                Debug.Log("Client loaded dynamic prefab");
                AcknowledgeSuccessfulPrefabLoadServerRpc(guid);
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        void AcknowledgeSuccessfulPrefabLoadServerRpc(FixedString64Bytes guid, ServerRpcParams rpcParams = default)
        {
            m_CountOfClientsThatLoadedThePrefab++;
            Debug.Log("Client acknowledged successful prefab load");
        }

        async Task<GameObject> EnsureDynamicPrefabIsLoaded(string guid)
        {
            if(m_LoadedDynamicPrefabs.ContainsKey(guid))
            {
                Debug.Log("Prefab has already been loaded, skipping loading this time");
                return m_LoadedDynamicPrefabs[guid];
            }
            
            var op = Addressables.LoadAssetAsync<GameObject>(guid);
            var prefab = await op.Task;
            Addressables.Release(op);

            _networkManager.AddNetworkPrefab(prefab);
            m_LoadedDynamicPrefabs.Add(guid, prefab);
            
            return prefab;
        }
    }
}
