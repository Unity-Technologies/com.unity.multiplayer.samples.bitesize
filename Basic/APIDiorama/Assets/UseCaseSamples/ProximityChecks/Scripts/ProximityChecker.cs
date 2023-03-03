using System;
using UnityEngine;

namespace Unity.Netcode.Samples.APIDiorama
{
    /// <summary>
    /// Informs about the proximity status of the local player
    /// </summary>
    public class ProximityChecker : MonoBehaviour
    {
        [SerializeField, Tooltip("At which distance will the player be considered 'close'?")]
        float m_ActivationRadius = 1;

        [SerializeField, Tooltip("A visual representation of the radius?")]
        Transform m_RadiusRepresentation;
        Transform m_Transform;

        event Action<bool> OnLocalPlayerProximityStatusChanged;
        internal bool LocalPlayerIsClose { get; private set; }
        void Awake()
        {
            m_Transform = transform;
            if (m_RadiusRepresentation)
            {
                const float k_OffsetFromGround = 0.01f;
                m_RadiusRepresentation.transform.localPosition = new Vector3(0, (m_Transform.lossyScale.y / -2) + k_OffsetFromGround, 0);
            }
        }

        internal void AddListener(Action<bool> callback)
        {
            OnLocalPlayerProximityStatusChanged += callback;
        }

        internal void RemoveListener(Action<bool> callback)
        {
            OnLocalPlayerProximityStatusChanged -= callback;
        }

        void Update()
        {
            if (m_RadiusRepresentation)
            {
                m_RadiusRepresentation.localScale = new Vector3(m_ActivationRadius * 2, m_RadiusRepresentation.localScale.y, m_ActivationRadius * 2);
            }
            bool oldValue = LocalPlayerIsClose;
            LocalPlayerIsClose = LocalPlayerIsCloseEnough(m_Transform.position, m_ActivationRadius);
            if (oldValue != LocalPlayerIsClose)
            {
                OnLocalPlayerProximityStatusChanged?.Invoke(LocalPlayerIsClose);
            }
        }

        bool LocalPlayerIsCloseEnough(Vector3 point, float range)
        {
            if (!PlayerManager.s_LocalPlayer)
            {
                return false;
            }
            return Vector3.Distance(point, PlayerManager.s_LocalPlayer.transform.position) < range;
        }
    }
}
