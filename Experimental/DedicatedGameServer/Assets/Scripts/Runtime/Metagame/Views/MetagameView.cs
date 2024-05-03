using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Main view of the <see cref="MetagameApplication"></see>
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
    }
}
