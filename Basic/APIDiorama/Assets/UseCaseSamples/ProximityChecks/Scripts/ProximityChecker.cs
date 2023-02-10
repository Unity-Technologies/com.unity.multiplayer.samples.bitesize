using System;
using UnityEngine;

namespace Unity.Netcode.Samples.APIDiorama
{
    /// <summary>
    /// Informs about the proximity staus of the local player
    /// </summary>
    public class ProximityChecker : MonoBehaviour
    {
        [SerializeField, Tooltip("At which distance will the player be considered 'close'?")]
        float m_ActivationRadius = 1;
        Transform m_Transform;

        event Action<bool> OnLocalPlayerProximityStatusChanged;
        internal bool LocalPlayerIsClose { get; private set; }
        void Awake()
        {
            m_Transform = transform;
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
