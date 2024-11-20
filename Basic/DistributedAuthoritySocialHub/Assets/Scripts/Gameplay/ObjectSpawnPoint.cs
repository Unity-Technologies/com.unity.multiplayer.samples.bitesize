using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub
{
    public class ObjectSpawnPoint : MonoBehaviour
    {
#if UNITY_EDITOR
        // Add gizmo to show the spawn position of the network object
        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.848f, 0.501f, 0.694f));
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }
#endif
    }

}
