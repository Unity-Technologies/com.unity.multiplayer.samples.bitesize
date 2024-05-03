using System;
using Cinemachine;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Assumes client authority
    /// </summary>
    [RequireComponent(typeof(NetworkedPlayerCharacter))]
    public class ClientPlayerCharacter : MonoBehaviour
    {
        [SerializeField]
        NetworkedPlayerCharacter m_NetworkedPlayerCharacter;

        [SerializeField]
        CharacterController m_CharacterController;

        [SerializeField]
        ThirdPersonController m_ThirdPersonController;

        [SerializeField]
        CapsuleCollider m_CapsuleCollider;

        [SerializeField]
        Transform m_CameraFollow;

        [SerializeField]
        PlayerInput m_PlayerInput;
        internal PlayerInput PlayerInput => m_PlayerInput;

        void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // ThirdPersonController & CharacterController are enabled only on owning clients. Ghost player objects have
            // these two components disabled, and will enable a CapsuleCollider. Per the CharacterController documentation: 
            // https://docs.unity3d.com/Manual/CharacterControllers.html, a Character controller can push rigidbody
            // objects aside while moving but will not be accelerated by incoming collisions. This means that a primitive
            // CapsuleCollider must instead be used for ghost clients to simulate collisions between owning players and 
            // ghost clients.
            m_ThirdPersonController.enabled = false;
            m_PlayerInput.enabled = false;
            m_CapsuleCollider.enabled = false;
            m_CharacterController.enabled = false;
            m_NetworkedPlayerCharacter.OnNetworkSpawnHook += OnNetworkSpawn;
        }

        void OnDestroy()
        {
            m_NetworkedPlayerCharacter.OnNetworkSpawnHook -= OnNetworkSpawn;
        }

        void OnNetworkSpawn()
        {
            if (!m_NetworkedPlayerCharacter.IsOwner)
            {
                enabled = false;
                m_CapsuleCollider.enabled = true;
                return;
            }

            // player input is only enabled on owning players
            m_PlayerInput.enabled = true;
            m_ThirdPersonController.enabled = true;

            // see the note inside NetworkedPlayerCharacter why this step is also necessary for synchronizing initial player
            // position on owning clients
            m_CharacterController.enabled = true;

            var cinemachineVirtualCamera = FindFirstObjectByType<CinemachineVirtualCamera>();
            cinemachineVirtualCamera.Follow = m_CameraFollow;

            GameApplication.Instance.Model.PlayerCharacter = this;
        }

        void OnMenuToggle(InputValue value)
        {
            if (value.isPressed)
            {
                GameApplication.Instance.Broadcast(new MenuToggleEvent());
            }
        }

        public void SetInputsActive(bool active)
        {
            m_PlayerInput.enabled = active;
            Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !active;
        }
    }
}
