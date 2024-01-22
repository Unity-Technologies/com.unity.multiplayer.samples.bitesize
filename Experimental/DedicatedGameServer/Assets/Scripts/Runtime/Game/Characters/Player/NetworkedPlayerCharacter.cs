using System;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Networked script to handle player character logic that needs to be networked.
    /// Inherits from NetcodeHooks class to provide hooks for spawn and despawn events.
    /// </summary>
    public class NetworkedPlayerCharacter : NetcodeHooks, ICharacter
    {
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                OnServerSpawnPlayer();
            }

            base.OnNetworkSpawn();
        }

        void OnServerSpawnPlayer()
        {
            // this is done server side, so we have a single source of truth for our spawn point list
            var spawnPoint = ServerPlayerSpawnPoints.Instance.ConsumeNextSpawnPoint();
            var spawnPosition = spawnPoint ? spawnPoint.transform.position : Vector3.zero;
            transform.position = spawnPosition;

            // Side Note:  Specific to Owner Authoritative
            // Setting the position works as and can be set in OnNetworkSpawn server-side unless there is a
            // CharacterController that is enabled by default on the authoritative side. With CharacterController, it
            // needs to be disabled by default (i.e. in Awake), the server applies the position (OnNetworkSpawn), and then
            // the owner of the NetworkObject should enable CharacterController during OnNetworkSpawn. Otherwise,
            // CharacterController will initialize itself with the initial position (before synchronization) and updates the
            // transform after synchronization with the initial position, thus overwriting the synchronized position.
        }
    }
}
