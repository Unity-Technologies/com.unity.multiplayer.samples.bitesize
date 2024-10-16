using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.Multiplayer.Samples.SocialHub.Effects
{
    /// <summary>
    /// Intended to be used as a client-side only pool
    /// </summary>
    class FXPrefabPool : MonoBehaviour
    {
        [SerializeField]
        GameObject m_Prefab;

        static Dictionary<GameObject, FXPrefabPool> m_FxPool = new Dictionary<GameObject, FXPrefabPool>();

        ObjectPool<GameObject> m_Pool;

        public static FXPrefabPool GetFxPool(GameObject prefab)
        {
            if (!m_FxPool.ContainsKey(prefab))
            {
                var instance = new GameObject($"{prefab.name}-FxPool");
                var fxPool = instance.AddComponent<FXPrefabPool>();
                fxPool.Initialize(prefab);
                m_FxPool.Add(prefab, fxPool);

                // Move the pool far above the players (i.e. out of sight)
                instance.transform.position = Vector3.up * 5000;
                DontDestroyOnLoad(instance);
            }

            return m_FxPool[prefab];
        }

        void Initialize(GameObject gameObject, int startCapacity = 10, int maxCapacity = 100)
        {
            m_Prefab = gameObject;

            GameObject CreateFunc()
            {
                var pooledInstance = Instantiate(m_Prefab);
                pooledInstance.SetActive(false);
                var fxBase = pooledInstance.GetComponent<BaseFxObject>();
                fxBase.SetFxPool(this);
                fxBase.transform.parent = transform;
                return pooledInstance;
            }

            void OnGet(GameObject obj)
            {
                if (obj)
                {
                    obj.SetActive(true);
                }
            }

            void OnRelease(GameObject obj)
            {
                if (obj)
                {
                    obj.SetActive(false);
                }
            }

            void OnDestroyPoolObject(GameObject obj)
            {
                if (obj)
                {
                    OnDestroyObject(obj);
                    Destroy(obj);
                }
            }

            m_Pool = new ObjectPool<GameObject>(createFunc: CreateFunc, actionOnGet: OnGet, actionOnRelease: OnRelease, actionOnDestroy: OnDestroyPoolObject,
                defaultCapacity: startCapacity, maxSize: maxCapacity);
        }

        protected virtual void OnDestroyObject(GameObject obj) { }

        protected virtual void OnGetInstance(GameObject obj) { }

        protected virtual void OnReleaseInstance(GameObject obj) { }

        internal GameObject GetInstance()
        {
            var objInstance = m_Pool.Get();
            objInstance.transform.parent = null;
            OnGetInstance(objInstance);
            return objInstance;
        }

        internal void ReleaseInstance(GameObject gameObject)
        {
            gameObject.transform.parent = null;
            OnReleaseInstance(gameObject);
            m_Pool.Release(gameObject);
            gameObject.transform.parent = transform;
        }
    }
}
