using System;
using System.Collections;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    public class VFXBehaviour : MonoBehaviour
    {
        [SerializeField]
        string destructionVFXType;

        VFXPoolManager m_VFXPoolManager;
        GameObject m_VFXInstance;
        ParticleSystem m_ParticleSystem;

        void Start()
        {
            m_VFXInstance = gameObject;
            m_ParticleSystem = m_VFXInstance.GetComponent<ParticleSystem>();
            PlayDestructionVFX();
        }

        void PlayDestructionVFX()
        {
            if (m_VFXInstance != null)
            {
                if (m_ParticleSystem != null)
                {
                    m_ParticleSystem.Play();
                    StartCoroutine(ReturnVFXInstanceAfterDelay(destructionVFXType, m_VFXInstance, m_ParticleSystem.main.duration - 0.1f));
                }
            }
        }

        IEnumerator ReturnVFXInstanceAfterDelay(string vfxType, GameObject vfxInstance, float delay)
        {
            Debug.Log("Returning VFX instance after delay.");
            yield return new WaitForSeconds(delay);
            if (m_ParticleSystem != null)
            {
                m_ParticleSystem.Stop();
                m_VFXPoolManager?.ReturnVFXInstance(vfxType, vfxInstance);
            }
        }
    }
}
