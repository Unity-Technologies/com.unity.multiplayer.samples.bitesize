using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Effects
{
    class Watermill : MonoBehaviour
    {
        [SerializeField]
        AnimationCurve m_RotationRhythm;
        [SerializeField]
        Transform m_Wheel;
        [SerializeField]
        float m_Speed;

        void Update()
        {
            m_Wheel.Rotate(0f, 0f, m_RotationRhythm.Evaluate(Time.time) * m_Speed * Time.deltaTime);
        }
    }
}
