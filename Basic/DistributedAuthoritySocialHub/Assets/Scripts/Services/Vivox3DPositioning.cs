using UnityEngine;
using Unity.Netcode;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;

namespace Unity.Multiplayer.Samples.SocialHub.Services
{
    class Vivox3DPositioning : NetworkBehaviour
    {
        bool m_Initialized;
        float m_NextPosUpdate;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!HasAuthority)
            {
                enabled = false;
                return;
            }

            GameplayEventHandler.OnChatIsReady += OnChatIsReady;
            GameplayEventHandler.OnExitedSession += OnExitSession;
        }

        void OnChatIsReady(bool chatIsReady, string channelName)
        {
            m_Initialized = chatIsReady;
        }

        void OnExitSession()
        {
            m_Initialized = false;
        }

        void Update()
        {
            if (!m_Initialized)
            {
                return;
            }

            if (Time.time > m_NextPosUpdate)
            {
                VivoxManager.Instance.SetPlayer3DPosition(gameObject);
                m_NextPosUpdate = Time.time + 0.3f;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            GameplayEventHandler.OnChatIsReady -= OnChatIsReady;
            GameplayEventHandler.OnExitedSession -= OnExitSession;
        }
    }
}
