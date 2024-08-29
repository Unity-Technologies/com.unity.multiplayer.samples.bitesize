using UnityEngine;

namespace Unity.Netcode.Samples.MultiplayerUseCases.RPC
{
    /// <summary>
    /// Keeps an object at a certain offset from another one
    /// </summary>
    public class PositionOffsetKeeper : MonoBehaviour
    {
        Transform m_TargetToFollow;
        Vector3 m_PositionOffsetToKeep;

        public void Initialize(Transform targetToFollow, Vector3 positionOffsetToKeep)
        {
            m_TargetToFollow = targetToFollow;
            m_PositionOffsetToKeep = positionOffsetToKeep;
        }

        void LateUpdate()
        {
            if (m_TargetToFollow)
            {
                transform.position = m_TargetToFollow.position + m_PositionOffsetToKeep;
            }
        }
    }
}
