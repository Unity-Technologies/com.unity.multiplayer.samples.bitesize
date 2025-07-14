using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Effects
{
    class FireLight : MonoBehaviour
    {
        [SerializeField]
        AnimationCurve m_LightCurve;

        [SerializeField]
        float m_FireSpeed = 1f;

        Light m_Light;
        float m_InitialIntensity;

        void Awake()
        {
            m_Light = GetComponent<Light>();
            m_InitialIntensity = m_Light.intensity;
        }

        void Update()
        {
            m_Light.intensity = m_InitialIntensity * m_LightCurve.Evaluate(Time.time * m_FireSpeed);
        }
    }
}
