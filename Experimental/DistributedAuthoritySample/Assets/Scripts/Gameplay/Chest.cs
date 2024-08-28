using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    public class Chest : CarryableObject
    {
        public GameObject rubblePrefab;

        protected override void DestroyObject()
        {
            Vector3 spawnPosition = transform.position; // Store the position before base is called
            base.DestroyObject(); // Call the base method to propagate the event and play VFX

            SpawnRubble(spawnPosition);
        }

        private void SpawnRubble(Vector3 position)
        {
            Instantiate(rubblePrefab, position, Quaternion.identity);
        }

    }
}


