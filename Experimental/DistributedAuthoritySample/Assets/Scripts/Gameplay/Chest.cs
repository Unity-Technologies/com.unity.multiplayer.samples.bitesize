using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    public class Chest : CarryableObject
    {
        public GameObject rubblePrefab; // Reference to the rubble prefab
        private GameObject spawnedRubble;

        protected override void DestroyObject()
        {
            Vector3 spawnPosition = transform.position;
            base.DestroyObject();
            SpawnRubble(spawnPosition);
        }

        protected override void SpawnRubble(Vector3 position)
        {
            if (rubblePrefab != null && spawnedRubble == null)
            {
                spawnedRubble = Instantiate(rubblePrefab, position, Quaternion.identity);
                if (spawnedRubble.TryGetComponent<NetworkObject>(out var networkObject))
                {
                    networkObject.Spawn();
                }

                ChangeRubbleVisuals(true);
            }
        }

        protected internal void ChangeRubbleVisuals(bool enable)
        {
            if (spawnedRubble != null)
            {
                var vector3 = spawnedRubble.gameObject.transform.position;
                vector3.y = 0f;
                spawnedRubble.gameObject.transform.position = vector3;
                // Enable or disable renderers
                Renderer[] renderers = spawnedRubble.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.enabled = enable;
                }

                // Enable or disable colliders
                Collider[] colliders = spawnedRubble.GetComponentsInChildren<Collider>();
                foreach (Collider collider in colliders)
                {
                    collider.enabled = enable;
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (spawnedRubble != null)
            {
                if (spawnedRubble.TryGetComponent<NetworkObject>(out var networkObject))
                {
                    networkObject.Despawn(true);
                }
                else
                {
                    Destroy(spawnedRubble);
                }

                spawnedRubble = null;
            }
        }
    }
}
