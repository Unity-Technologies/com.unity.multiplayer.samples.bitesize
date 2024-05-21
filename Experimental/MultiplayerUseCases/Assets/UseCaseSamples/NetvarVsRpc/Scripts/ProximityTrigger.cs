using Unity.Netcode.Samples.MultiplayerUseCases.Common;
using UnityEngine;

namespace Unity.Netcode.Samples.MultiplayerUseCases.NetVarVsRpc
{
    /// <summary>
    /// Toggles an object when the local player is close enough
    /// </summary>
    public class ProximityTrigger : MonoBehaviour
    {
        [SerializeField]
        GameObject objectToToggle;

        [SerializeField, Tooltip("At which distance will the trigger be triggered?")]
        float m_ActivationRadius = 1;

        Transform m_Transform;

        void Awake()
        {
            m_Transform = transform;
        }

        void Update()
        {
            objectToToggle.SetActive(LocalPlayerIsCloseEnough(m_Transform.position, m_ActivationRadius));
        }

        internal static bool LocalPlayerIsCloseEnough(Vector3 point, float range)
        {
            if (!PlayerManager.s_LocalPlayer)
            {
                return false;
            }
            return Vector3.Distance(point, PlayerManager.s_LocalPlayer.transform.position) < range;
        }
    }
}
