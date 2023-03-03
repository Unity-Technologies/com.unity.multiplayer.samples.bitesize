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
            response.Position = GetPlayerSpawnPosition();
        }

        Vector3 GetPlayerSpawnPosition()
        {
            /*
             * this is just an example, and you change this implementation to make players spawn on specific spawn points
             * depending on other factors (I.E: player's team)
             */
            return new Vector3(Random.Range(-3, 3), 0, Random.Range(-3, 3));
        }
    }
}
