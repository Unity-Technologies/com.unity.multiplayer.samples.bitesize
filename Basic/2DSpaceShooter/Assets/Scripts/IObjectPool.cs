using System.Linq.Expressions;
using System.Text;
using UnityEngine;
using UnityEngine.Pool;

// This component returns the particle system to the pool when the OnParticleSystemStopped event is received.
[RequireComponent(typeof(ParticleSystem))]
public class ReturnToPool : MonoBehaviour
{
    //public ObjectPool<ParticleSystem> _bulletPool;
    public ParticleSystem system;
    public IObjectPool<ParticleSystem> m_Pool;
    Bullet m_Bullet;

    void Start()
    {
        m_Bullet = GetComponent<Bullet>();
        system = GetComponent<ParticleSystem>();
        var main = system.main;
        main.stopAction = ParticleSystemStopAction.Callback;

        m_Pool = new ObjectPool<ParticleSystem>(CreateParticles, OnTakeParticlesFromPool, OnReturnParticlesToPool, OnDestroyParticles, true, 100, 1000);
    }

    // what to call when there are no objects in the pool
    private ParticleSystem CreateParticles()
    {
        ParticleSystem particles = Instantiate(m_Bullet.particles, m_Bullet.transform);

        return particles;
    }

    // what to call when you want to TAKE an object from the pool
    private void OnTakeParticlesFromPool(ParticleSystem particles)
    {
        Debug.Log("took particles from Pool");
        var transform1 = m_Bullet.transform;
        transform1.position = transform.position + new Vector3(0, 0, -2);
        transform1.rotation = Quaternion.identity;

        particles.gameObject.SetActive(true);
    }

    // what you call when you want to RETURN an object to the pool
    private static void OnReturnParticlesToPool(ParticleSystem particles)
    {
        Debug.Log("returned particles");
        particles.gameObject.SetActive(false);
    }

    // what to call when want to DESTROY an object of pool
    private static void OnDestroyParticles(ParticleSystem particles)
    {
        Debug.Log("destroyed particles");
        Destroy(particles.gameObject);
    }

    /*void OnParticleSystemStopped()
    {
        Debug.Log("stopped particles");
        // Return to the pool
        m_Pool.Release(system);
    }*/
}
