using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    public class Chest : CarryableObject
    {
        internal GameObject rubble;

        private void Awake()
        {
            rubble = transform.Find("Rubble_Chest").gameObject; // Make sure the child object's name is correct
            if (rubble != null)
            {
                Vector3 spawnPosition = transform.position;
                ChangeRubbleVisuals(false, spawnPosition);
            }
        }

        /*protected override void DestroyObject()
        {
            Vector3 spawnPosition = transform.position;
            base.DestroyObject();
            SpawnRubble(spawnPosition);
        }*/

        /*protected override void SpawnRubble(Vector3 position)
        {
            ChangeRubbleVisuals(true, position);
        }*/

        protected internal void ChangeRubbleVisuals(bool enable, Vector3 transform)
        {
            if (rubble != null)
            {
                // Enable or disable renderers
                Renderer[] renderers = rubble.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.enabled = enable;
                }

                // Enable or disable colliders
                Collider[] colliders = rubble.GetComponentsInChildren<Collider>();
                foreach (Collider collider in colliders)
                {
                    collider.enabled = enable;
                    rubble.gameObject.transform.position = transform;
                }
            }
        }

        private IEnumerable<WaitForSeconds> RubbleDeactivate(GameObject rubblePrefab)
        {
            yield return new WaitForSeconds(5f);
            ChangeRubbleVisuals(false, transform.position);
        }
    }

}

