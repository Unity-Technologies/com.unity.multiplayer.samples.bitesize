using Unity.Template.Multiplayer.NGO.Runtime.ConnectionManagement;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    internal class DirectIPController : Controller<MetagameApplication>
    {
        [SerializeField]
        ConnectionManager m_ConnectionManager;
        DirectIPView View => App.View.DirectIP;
        void Awake()
        {
            AddListener<EnterIPConnectionEvent>(OnEnterIPConnection);
            AddListener<ExitIPConnectionEvent>(OnExitIPConnection);
            AddListener<JoinThroughDirectIPEvent>(OnJoinGame);
        }

        void OnDestroy()
        {
            RemoveListeners();
        }
        
        internal override void RemoveListeners()
        {
            RemoveListener<EnterIPConnectionEvent>(OnEnterIPConnection);
            RemoveListener<ExitIPConnectionEvent>(OnExitIPConnection);
            RemoveListener<JoinThroughDirectIPEvent>(OnJoinGame);
        }

        void OnEnterIPConnection(EnterIPConnectionEvent evt)
        {
            View.Show();
        }

        void OnExitIPConnection(ExitIPConnectionEvent evt)
        {
            View.Hide();
        }

        void OnJoinGame(JoinThroughDirectIPEvent evt)
        {
            m_ConnectionManager.StartClient(evt.ipAddress, evt.port);
        }
    }
}
