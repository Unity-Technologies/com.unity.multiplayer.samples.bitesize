using System.Collections.Generic;
using UnityEngine;
public class ExplosionFx : BaseFxObject
{
    private ParticleSystem m_ParticleSystem;

    private Vector3 m_OriginalScale;

    private List<ParticleSystem> m_AllParticleSystems = new List<ParticleSystem>();

    private void Awake()
    {
        m_ParticleSystem = GetComponent<ParticleSystem>();
        m_OriginalScale = transform.localScale;
        var systemMain = m_ParticleSystem.main;
        systemMain.stopAction = ParticleSystemStopAction.Callback;
    }

    private void OnParticleSystemStopped()
    {
        foreach (var particleSystem in m_AllParticleSystems)
        {
            if (particleSystem.isPlaying) 
            {
                return;
            }
        }
        transform.localScale = m_OriginalScale;
        StopFx();
    }

    private void OnEnable()
    {
        m_ParticleSystem.Play();
    }

    private void OnTransformParentChanged()
    {
        // Keep our original scale always
        transform.localScale = m_OriginalScale;
    }
}

