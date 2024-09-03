using System.Collections.Generic;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    public class VFXPoolManager : MonoBehaviour
    {
        public static VFXPoolManager Instance { get; private set; }

        public GameObject potDestructionVFX;
        public GameObject crateDestructionVFX;
        public int initialPoolSize = 10;

        private Dictionary<string, VFXPool> vfxPools;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
                InitializePools();
            }
        }

        private void InitializePools()
        {
            vfxPools = new Dictionary<string, VFXPool>();

            if (potDestructionVFX != null)
            {
                Debug.Log("Initializing potDestructionVFX pool.");
                var potPool = new GameObject("PotVFXPool").AddComponent<VFXPool>();
                potPool.transform.parent = this.transform;
                potPool.vfxPrefab = potDestructionVFX;
                potPool.initialPoolSize = initialPoolSize;
                potPool.Initialize(); // Explicitly call Initialize to trigger Awake
                vfxPools.Add("Pot", potPool);
            }
            else
            {
                Debug.LogError("Pot destruction VFX prefab is not assigned!");
            }

            if (crateDestructionVFX != null)
            {
                Debug.Log("Initializing crateDestructionVFX pool.");
                var cratePool = new GameObject("CrateVFXPool").AddComponent<VFXPool>();
                cratePool.transform.parent = this.transform;
                cratePool.vfxPrefab = crateDestructionVFX;
                cratePool.initialPoolSize = initialPoolSize;
                cratePool.Initialize(); // Explicitly call Initialize to trigger Awake
                vfxPools.Add("Crate", cratePool);
            }
            else
            {
                Debug.LogError("Crate destruction VFX prefab is not assigned!");
            }
        }

        public GameObject GetVFXInstance(string poolType)
        {
            if (vfxPools.ContainsKey(poolType))
            {
                return vfxPools[poolType].GetVFXInstance();
            }

            Debug.LogError("No VFX pool of type " + poolType + " found.");
            return null;
        }

        public void ReturnVFXInstance(string poolType, GameObject vfxInstance)
        {
            if (vfxPools.ContainsKey(poolType))
            {
                vfxPools[poolType].ReturnVFXInstance(vfxInstance);
            }
            else
            {
                Debug.LogError("No VFX pool of type " + poolType + " found.");
            }
        }
    }
}
