using System;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// This is probably the most advanced scenario - we are spawning prefabs that aren't known to clients beforehand.
    ///
    /// Here we are not implementing client acknowledgement of the prefab loading completion -
    /// this would allow the server to wait for all the clients to have loaded the prefab and spawn it only then, which is safer.
    /// However, this is not implemented here for simplicity - there is a whole slew of potential failures that this would have to handle.
    ///
    /// Instead we load the prefab from an addressable, and then send an RPC to the clients, telling them to start loading the prefab.
    /// Then we wait for a configured amount of time, and then spawn the prefab on the server, which replicates it to the clients.
    /// Then we hope that the clients will be able to load the prefab during the (m_SpawnDelayInSeconds + m_SpawnTimeoutInSeconds) time window.
    /// 
    /// Currently latejoin doesn't work with dynamic prefabs, because the initial sync message doesn't allow the clients any time to load prefabs. That's a bug.
    /// To circumvent this issue we simply disallow latejoins (and reconnections :( ) and do not spawn dynamic things until all clients are connected and the gameplay is started by the server.
    ///
    /// This is not ideal, and in the future we will be able to deal with latejoiners who need to preload a bunch of
    /// prefabs before they can properly replicate.
    /// We will need to maintain a collection of loaded dynamic prefabs on the server, and they will need to be loaded when a new client connects.
    /// 
    /// </summary>
    public sealed class SparseLoadingNoLatejoinSample : NetworkBehaviour
    {
        [SerializeField]
        Button m_StartGameButton;
        
        [SerializeField] AssetReferenceGameObject m_DynamicPrefabRef;
        
        [SerializeField] NetworkManager _networkManager;

        [SerializeField]
        float m_SpawnTimeoutInSeconds;
        [SerializeField]
        float m_SpawnDelayInSeconds;
        
        bool m_isGameStarted = false;
        
        void Awake()
        {
            _networkManager.NetworkConfig.ForceSamePrefabs = false;
            _networkManager.NetworkConfig.SpawnTimeout = m_SpawnTimeoutInSeconds;
            _networkManager.ConnectionApprovalCallback = ConnectionApprovalCallback;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                m_StartGameButton.onClick.AddListener(StartGame);
            }
            else
            {
                m_StartGameButton.gameObject.SetActive(false);
            }
        }

        private void StartGame()
        {
            if (!m_isGameStarted)
            {
                m_isGameStarted = true;
                
                SpawnDynamicPrefabWithDelay();
                
                m_StartGameButton.gameObject.SetActive(false);
            }
        }

        void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            if (m_isGameStarted)
            {
                response.Approved = false;
            }
            else
            {
                response.Approved = true;
            }
        }

        private async void SpawnDynamicPrefabWithDelay()
        {
            if (IsServer || IsHost)
            {
                LoadAddressableClientRpc(m_DynamicPrefabRef.AssetGUID);

                var prefab = await LoadDynamicAddressablePrefab(m_DynamicPrefabRef.AssetGUID);
                await SpawnAfterDelay(_networkManager.LocalClientId, (int)(m_SpawnDelayInSeconds * 1000), prefab);
            }

            async Task SpawnAfterDelay(ulong ownerId, int delayMS, GameObject prefab)
            {
                await Task.Delay(delayMS);
                var obj = Instantiate(prefab).GetComponent<NetworkObject>();
                obj.SpawnWithOwnership(ownerId);
            }
        }

        [ClientRpc]
        private void LoadAddressableClientRpc(FixedString64Bytes guid, ClientRpcParams rpcParams = default)
        {
            if (!IsHost)
            {
                LoadPrefab(guid);
            }

            async void LoadPrefab(FixedString64Bytes guid)
            {
                await LoadDynamicAddressablePrefab(guid.ToString());
            }
        }

        private async Task<GameObject> LoadDynamicAddressablePrefab(string guid)
        {
            var op = Addressables.LoadAssetAsync<GameObject>(guid);
            var prefab = await op.Task;
            Addressables.Release(op);

            _networkManager.AddNetworkPrefab(prefab);

            return prefab;
        }
    }
}
