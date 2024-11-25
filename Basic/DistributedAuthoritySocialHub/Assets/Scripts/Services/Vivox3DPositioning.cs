using UnityEngine;
using Unity.Netcode;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;

namespace Unity.Multiplayer.Samples.SocialHub.Services
{
    class Vivox3DPositioning : NetworkBehaviour
    {
        bool m_Initialized;
        float m_NextPosUpdate;

        void Start()
        {
            GameplayEventHandler.OnChatIsReady -= OnChatIsReady;
            GameplayEventHandler.OnChatIsReady += OnChatIsReady;

            GameplayEventHandler.OnExitedSession -= OnExitSession;
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
            if (IsOwner && m_Initialized)
            {
                if (Time.time > m_NextPosUpdate)
                {
                    VivoxManager.Instance.SetPlayer3DPosition(gameObject);
                    m_NextPosUpdate = Time.time + 0.3f;
                }
            }
        }

        public override void OnDestroy()
        {
            GameplayEventHandler.OnChatIsReady -= OnChatIsReady;
            GameplayEventHandler.OnExitedSession -= OnExitSession;
        }
    }
}
