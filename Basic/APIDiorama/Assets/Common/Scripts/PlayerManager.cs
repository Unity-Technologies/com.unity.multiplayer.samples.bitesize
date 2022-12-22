using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Unity.Netcode.Samples.APIDiorama
{
    public class PlayerManager : NetworkBehaviour
    {
        public static PlayerManager s_LocalPlayer;

        [SerializeField]
        PlayerInput inputManager;

        [SerializeField]
        float moveSpeed = 1;

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