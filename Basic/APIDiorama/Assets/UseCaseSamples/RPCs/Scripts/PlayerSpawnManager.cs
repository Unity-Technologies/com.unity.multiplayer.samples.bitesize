using UnityEngine;

namespace Unity.Netcode.Samples.APIDiorama
{
    /// <summary>
    /// Manages how a player will be spawned
    /// </summary>
    class PlayerSpawnManager : NetworkBehaviour
    {
        void Start()
        {
            NetworkManager.ConnectionApprovalCallback = ConnectionApprovalCallback;
        }

        void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = true;
            response.CreatePlayerObject = true;

            // Position to spawn the player object (if null it uses default of Vector3.zero)
            response.Position = new Vector3(Random.Range(-3, 3), 0, Random.Range(-3, 3));
            
            response.Pending = false;
        }
    }
}
