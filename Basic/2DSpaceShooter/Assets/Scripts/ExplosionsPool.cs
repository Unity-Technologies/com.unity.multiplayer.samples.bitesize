using System;
using UnityEngine;
using UnityEngine.Pool;

public class ExplosionsPool : MonoBehaviour
{
    public static ExplosionsPool s_Singleton;
    /// <summary>
    /// Collection checks will throw errors if we try to release an item that is already in the pool.
    /// </summary>
    [SerializeField]
    bool m_CollectionChecks = true;
    [SerializeField]
    int m_MaxPoolSize = 100;
    [SerializeField]
    GameObject m_ExplosionPrefab;
    ObjectPool<ParticleSystem> m_Pool;

    public ObjectPool<ParticleSystem> Pool
    {
        get
        {
            if (m_Pool == null)
            {
                m_Pool = new ObjectPool<ParticleSystem>(CreatePooledItem, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, m_CollectionChecks, 50, m_MaxPoolSize);
            }
            return m_Pool;
        }
    }

    void Awake()
    {
        if (s_Singleton != null && s_Singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        s_Singleton = this;
    }

    void Start()
    {
        // create pool item instances at the start of a game to avoid performance loss throughout the game session
        for (int i = 0; i < 10; i++)
        {
           s_Singleton.Pool.Get();
        }
    }

    void OnDestroy()
    {
        if (s_Singleton == this)
        {
            s_Singleton = null;
        }
    }

    ParticleSystem CreatePooledItem()
    {
        var explosionParticles = Instantiate(m_ExplosionPrefab, transform.position + new Vector3(0, 0, -2), Quaternion.identity).GetComponent<ParticleSystem>();
        explosionParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        //This is used to return ParticleSystems to the pool when they have stopped.
        var returnToPool = explosionParticles.gameObject.AddComponent<ReturnParticleSystemToPool>();
        returnToPool.Initialize(explosionParticles, Pool);
        return explosionParticles;
    }

    // Called when an item is returned to the pool using Release
    void OnReturnedToPool(ParticleSystem system)
    {
        system.gameObject.SetActive(false);
        system.gameObject.GetComponent<AudioSource>().enabled = false;
    }

    // Called when an item is taken from the pool using Get
    void OnTakeFromPool(ParticleSystem system)
    {
        system.gameObject.SetActive(true);
        system.gameObject.GetComponent<AudioSource>().enabled = true;
    }

    // If the pool capacity is reached then any items returned will be destroyed.
    // We can control what the destroy behavior does, here we destroy the GameObject.
    void OnDestroyPoolObject(ParticleSystem system)
    {
        Destroy(system.gameObject);
    }
}
