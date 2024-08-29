using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    public class Chest : CarryableObject
    {
        protected override void DestroyObject()
        {
            Vector3 spawnPosition = transform.position; // Store the position before base is called
            base.DestroyObject(); // Call the base method to propagate the event and play VFX
            SpawnRubble(spawnPosition);
        }

        protected override void SpawnRubble(Vector3 position)
        {
            //set the childobject rubble to active

            GameObject rubblePrefab = gameObject.GetComponentInChildren<NetworkObject>().gameObject;
            rubblePrefab.SetActive(true);
            /*// set the rubble after 5 seconds to inactive
            var waitForSecondsEnumerable = RubbleDeactivate(rubblePrefab);*/
        }


        private IEnumerable<WaitForSeconds> RubbleDeactivate(GameObject rubblePrefab)
        {
            yield return new WaitForSeconds(5f);
            rubblePrefab.SetActive(false);
        }

        private void ChangeRubbleVisuals(bool enable)
        {
            // Disable all renderers to hide the chest visually
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = enable;
            }

            // Disable all colliders to make the chest non-interactive
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = enable;
            }
        }
    }
}
