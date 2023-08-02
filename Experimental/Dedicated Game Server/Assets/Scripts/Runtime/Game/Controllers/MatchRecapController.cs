using System.Collections;
using Unity.Template.Multiplayer.NGO.Runtime.ApplicationLifecycle;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class MatchRecapController : Controller<GameApplication>
    {
        MatchRecapView View => App.View.MatchRecap;

        void Awake()
        {
            AddListener<MatchResultComputedEvent>(OnClientMatchResultComputed);
            AddListener<MatchEndAcknowledgedEvent>(OnClientMatchEndAcknowledged);
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<MatchResultComputedEvent>(OnClientMatchResultComputed);
            RemoveListener<MatchEndAcknowledgedEvent>(OnClientMatchEndAcknowledged);
        }

        void OnClientMatchResultComputed(MatchResultComputedEvent evt)
        {
            View.OnClientMatchResultComputed(evt);
        }

        void OnClientMatchEndAcknowledged(MatchEndAcknowledgedEvent evt)
        {
            ApplicationController.Singleton.OnClientDoPostMatchCleanupAndReturnToMetagame();
        }
    }
}
