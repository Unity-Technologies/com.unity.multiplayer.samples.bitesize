using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    [RequireComponent(typeof(NetworkedAICharacter))]
    public class ClientAICharacter : MonoBehaviour
    {
        [SerializeField]
        NetworkedAICharacter m_NetworkedAICharacter;

        [SerializeField]
        Animator m_Animator;

        [SerializeField]
        AudioClip[] FootstepAudioClips;

        [SerializeField]
        [Range(0, 1)]
        float FootstepAudioVolume = 0.5f;

        static readonly int k_AnimIdSpeed = Animator.StringToHash("Speed");
        static readonly int k_AnimIdMotionSpeed = Animator.StringToHash("MotionSpeed");
        const float k_AnimMotionSpeed = 1.0f;

        void Start()
        {
            // Setting this value to 1
            m_Animator.SetFloat(k_AnimIdMotionSpeed, k_AnimMotionSpeed);
        }

        void Update()
        {
            if (m_NetworkedAICharacter.IsSpawned)
            {
                m_Animator.SetFloat(k_AnimIdSpeed, m_NetworkedAICharacter.Speed);
            }
        }
        
        void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.position, FootstepAudioVolume);
                }
            }
        }
    }
}
