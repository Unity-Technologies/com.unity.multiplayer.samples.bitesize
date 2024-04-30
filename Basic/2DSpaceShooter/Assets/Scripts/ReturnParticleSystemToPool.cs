using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Returns the particle system to the pool when the OnParticleSystemStopped event is received.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class ReturnParticleSystemToPool : MonoBehaviour
{
    ParticleSystem m_System;
    IObjectPool<ParticleSystem> m_Pool;

    internal void Initialize(ParticleSystem particleSystem, IObjectPool<ParticleSystem> pool)
    {
        m_Pool = pool;
        m_System = particleSystem;
        ParticleSystem.MainModule main = m_System.main;
        main.stopAction = ParticleSystemStopAction.Callback;
    }

    void OnParticleSystemStopped()
    {
        m_Pool.Release(m_System);
    }
}
