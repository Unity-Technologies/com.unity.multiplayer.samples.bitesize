using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Contains both client and server logic for a door that is opened when a player asks to.
    /// The visuals of the door animate as "opening" and "closing", but for physics purposes this is an illusion:
    /// whenever the door is open on the server, the door's physics are disabled, and vice versa.
    /// </summary>
    public class SwitchedDoor : NetworkBehaviour
    {
        const string k_OpenDoorAction = "OpenDoor";
        static readonly int s_AnimatorDoorOpenBoolID = Animator.StringToHash("IsOpen");

        [SerializeField]
        Animator m_Animator;

        [SerializeField]
        GameObject m_UI;

        public NetworkVariable<bool> IsOpen { get; } = new NetworkVariable<bool>();
        NetworkVariable<bool> m_CanBeOpened { get; } = new NetworkVariable<bool>();
        byte m_NearbyPlayers = 0;
        bool m_LocalPlayerIsNearby = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField]
        bool m_ForceOpen;
#endif

        [SerializeField]
        [Tooltip("This physics and navmesh obstacle is enabled when the door is closed.")]
        GameObject m_PhysicsObject;

        public override void OnNetworkSpawn()
        {
            IsOpen.OnValueChanged += OnDoorStateChanged;
            m_CanBeOpened.OnValueChanged += OnDoorCanBeOpenedChanged;

            if (IsClient)
            {
                // initialize visuals based on current server state (or else we default to "closed")
                m_PhysicsObject.SetActive(!IsOpen.Value);
            }

            if (IsServer)
            {
                OnDoorStateChanged(false, IsOpen.Value);
            }
            OnDoorCanBeOpenedChanged(false, false);
        }

        public override void OnNetworkDespawn()
        {
            IsOpen.OnValueChanged -= OnDoorStateChanged;
            m_CanBeOpened.OnValueChanged -= OnDoorCanBeOpenedChanged;
        }

        void Update()
        {
            if (IsServer && IsSpawned)
            {
                var forceOpen = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                forceOpen |= m_ForceOpen;
#endif
                if (forceOpen)
                {
                    OnServerOpenDoor();
                }
            }
            if (IsClient && m_LocalPlayerIsNearby && m_CanBeOpened.Value)
            {
                if (GameApplication.Instance.Model.PlayerCharacter.PlayerInput.actions[k_OpenDoorAction].WasPressedThisFrame())
                {
                    Debug.Log("[Client] Local player opening door");
                    ServerOpenRpc();
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (IsClient)
            {
                OnClientTriggerEnter(other);
            }
            if (IsServer)
            {
                OnServerTriggerEnter(other);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (IsClient)
            {
                OnClientTriggerExit(other);
            }
            if (IsServer)
            {
                OnServerTriggerExit(other);
            }
        }

        void OnServerTriggerEnter(Collider other)
        {
            if (other.GetComponent<ICharacter>() == null)
            {
                return;
            }
            Debug.Log("[Server] Player entered!");
            m_NearbyPlayers++;
            OnServerUpdateCanBeOpened();
        }

        void OnServerTriggerExit(Collider other)
        {
            if (other.GetComponent<ICharacter>() == null)
            {
                return;
            }
            Debug.Log("[Server] Player exited!");
            m_NearbyPlayers--;
            OnServerUpdateCanBeOpened();
        }

        void OnClientTriggerEnter(Collider other)
        {
            Debug.Log("[Client] Player entered!");
            var character = other.GetComponent<ICharacter>();
            if (character == null)
            {
                return;
            }
            if (character.NetworkObject.IsLocalPlayer)
            {
                m_LocalPlayerIsNearby = true;
                /*
                 * we do not use m_CanBeOpened here to predict if we can display the UI or not,
                 * because its value is being recalculated by the server
                 * at the same time and we could have an outdated value.
                 */
                m_UI.SetActive(!IsOpen.Value);
            }
        }

        void OnClientTriggerExit(Collider other)
        {
            var character = other.GetComponent<ICharacter>();
            if (character == null)
            {
                return;
            }
            Debug.Log("[Client] Player exited!");
            if (character.NetworkObject.IsLocalPlayer)
            {
                Debug.Log("[Client] Hiding UI!");
                m_LocalPlayerIsNearby = false;
                m_UI.SetActive(false);
            }
        }

        void OnServerUpdateCanBeOpened()
        {
            m_CanBeOpened.Value = m_NearbyPlayers > 0 && !IsOpen.Value;
        }

        void OnDoorStateChanged(bool wasDoorOpen, bool isDoorOpen)
        {
            if (IsServer)
            {
                m_Animator.SetBool(s_AnimatorDoorOpenBoolID, isDoorOpen);
            }

            if (IsClient)
            {
                m_PhysicsObject.SetActive(!isDoorOpen);
                if (isDoorOpen)
                {
                    m_UI.SetActive(false);
                }
            }
        }

        void OnDoorCanBeOpenedChanged(bool couldBeOpened, bool canBeOpened)
        {
            if (IsClient)
            {
                Debug.Log($"[Client] Door UI should be: {canBeOpened}");
                if (gameObject.activeSelf != canBeOpened)
                {
                    m_UI.SetActive(canBeOpened);
                }
            }
        }

        [Rpc(SendTo.Server)]
        void ServerOpenRpc()
        {
            OnServerOpenDoor();
        }

        void OnServerOpenDoor()
        {
            if (IsOpen.Value)
            {
                return;
            }
            Debug.Log("[Server] Opening door");
            IsOpen.Value = true;
        }
    }
}
