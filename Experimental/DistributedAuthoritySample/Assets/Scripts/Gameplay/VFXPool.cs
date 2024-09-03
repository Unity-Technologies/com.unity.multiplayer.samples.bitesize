using UnityEngine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    public class VFXPool : MonoBehaviour
    {
        public GameObject vfxPrefab;
        public int initialPoolSize = 10;
        private Queue<GameObject> poolQueue;

        public void Initialize()
        {
            if (vfxPrefab == null)
            {
                Debug.LogError("vfxPrefab is not assigned for VFXPool attached to " + gameObject.name);
                return;
            }

            Debug.Log("Initializing pool with size: " + initialPoolSize);
            poolQueue = new Queue<GameObject>();
            InitializePool();
        }

        private void InitializePool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                GameObject vfxInstance = Instantiate(vfxPrefab);
                vfxInstance.SetActive(false);
                poolQueue.Enqueue(vfxInstance);
            }
        }

        public GameObject GetVFXInstance()
        {
            if (poolQueue.Count > 0)
            {
                GameObject vfxInstance = poolQueue.Dequeue();
                vfxInstance.SetActive(true);
                return vfxInstance;
            }
            else
            {
                // Optionally expand the pool
                GameObject vfxInstance = Instantiate(vfxPrefab);
                return vfxInstance;
            }
        }

        public void ReturnVFXInstance(GameObject vfxInstance)
        {
            vfxInstance.SetActive(false);
            poolQueue.Enqueue(vfxInstance);
        }
    }


}
