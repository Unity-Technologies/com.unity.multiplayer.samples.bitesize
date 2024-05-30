using UnityEngine;
using UnityEngine.InputSystem;
namespace Unity.Netcode.Samples.MultiplayerUseCases.Common
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
            /* When an Network Object is spawned, you usally want to setup some if its components
             * so that they behave differently depending on whether this object is owned by the local player or by other clients. */
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
            //you don't want other players to be able to control your player
            if (inputManager)
            {
                inputManager.enabled = false;
            }
        }

        void OnLocalPlayerSpawned()
        {
            /* you want only the local player to be identified as such, and to have its input-related components enabled.
             * The same concept usually applies for cameras, UI, etc...*/
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
