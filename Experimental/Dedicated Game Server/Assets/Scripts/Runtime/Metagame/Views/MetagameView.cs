using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// The main view of the Metagame
    /// </summary>
    public class MetagameView : View<MetagameApplication>
    {
        internal MainMenuView MainMenu => m_MainMenuView;

        [SerializeField]
        MainMenuView m_MainMenuView;

        internal MatchmakerView Matchmaker => m_MatchmakerView;

        [SerializeField]
        MatchmakerView m_MatchmakerView;
        
        internal DirectIPView DirectIP => m_DirectIPView;

        [SerializeField]
        DirectIPView m_DirectIPView;
        
        internal ClientConnectingView ClientConnecting => m_ClientConnectingView;

        [SerializeField]
        ClientConnectingView m_ClientConnectingView;

        void Start()
        {
            if (App.IsDedicatedServer)
            {
                OnDedicatedServerDestroyViews();
            }
        }

        void OnDedicatedServerDestroyViews()
        {
            Destroy(gameObject);
        }
    }
}
