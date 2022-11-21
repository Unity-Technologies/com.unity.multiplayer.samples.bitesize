using UnityEngine;

namespace Unity.Netcode.Samples.APIDiorama
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
            objectToToggle.SetActive(PlayerIsCloseEnough());
        }

        bool PlayerIsCloseEnough()
        {
            if (!PlayerManager.s_LocalPlayer)
            {
                return false;
            }
            return Vector3.Distance(m_Transform.position, PlayerManager.s_LocalPlayer.transform.position) < m_ActivationRadius;
        }
    }
}
