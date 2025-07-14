using System;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Effects
{
    [RequireComponent(typeof(ParticleSystem))]
    class ReturnToPool : BaseFxObject
    {
        ParticleSystem m_ParticleSystem;

        void Awake()
        {
            m_ParticleSystem = GetComponent<ParticleSystem>();
            var systemMain = m_ParticleSystem.main;
            systemMain.stopAction = ParticleSystemStopAction.Callback;
        }

        void OnEnable()
        {
            m_ParticleSystem.Play();
        }

        void OnParticleSystemStopped()
        {
            StopFx();
        }
    }
}
