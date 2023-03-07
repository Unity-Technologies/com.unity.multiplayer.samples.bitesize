using UnityEngine;
using UnityEngine.InputSystem;
namespace Unity.Netcode.Samples.APIDiorama
{
    /// <summary>
    /// A generic player manager that manages the lifecycle of a player
    /// </summary>
    public class PlayerManager : NetworkBehaviour
    {
        /// <summary>
        /// The localplayer instance
        /// </summary>
        /// <remarks> You could use <c>NetworkManager.Singleton.LocalClient.PlayerObject</c> if you don't want to maintain this flag,
        /// but keep in mind that you'll also have to check that the NetworkManager is available and that a local client is running</remarks>
        public static PlayerManager s_LocalPlayer;

        [SerializeField]
        PlayerInput inputManager;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsOwner)
            {
                OnLocalPlayerSpawned();
                return;
            }
            OnNonLocalPlayerSpawned();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsOwner)
            {
                OnLocalPlayerDeSpawned();
                return;
            }
            OnNonLocalPlayerDeSpawned();
        }

        void OnNonLocalPlayerSpawned()
        {
            if (inputManager)
            {
                inputManager.enabled = false;
            }
        }

        void OnLocalPlayerSpawned()
        {
            s_LocalPlayer = this;
            if (inputManager)
            {
                inputManager.enabled = IsOwner;
            }
        }

        void OnLocalPlayerDeSpawned()
        {
            s_LocalPlayer = null;
        }

        void OnNonLocalPlayerDeSpawned() { }
    }
}