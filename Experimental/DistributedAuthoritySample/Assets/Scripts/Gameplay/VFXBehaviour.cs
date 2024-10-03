using System;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    public class VFXBehaviour : MonoBehaviour
    {
        [SerializeField]
        string destructionVFXType;

        GameObject m_VFXInstance;
        ParticleSystem m_ParticleSystem;

        void Awake()
        {
            m_ParticleSystem = GetComponent<ParticleSystem>();
            var systemMain = m_ParticleSystem.main;
            systemMain.stopAction = ParticleSystemStopAction.Callback;
        }

        void OnEnable()
        {
            m_VFXInstance = gameObject;
            m_ParticleSystem.Play();
        }

        void OnParticleSystemStopped()
        {
            VFXPoolManager.Instance.ReturnVFXInstance(destructionVFXType, m_VFXInstance);
        }
    }
}
