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

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            PlayDestructionVFX();
        }
        void PlayDestructionVFX()
        {
            GameObject vfxInstance = gameObject;
            if (vfxInstance != null)
            {
                Debug.Log(vfxInstance.gameObject.GetInstanceID());
                ParticleSystem system = vfxInstance.GetComponent<ParticleSystem>();
                StartCoroutine(ReturnVFXInstanceAfterDelay(destructionVFXType, vfxInstance, system.main.duration - 0.02f));;
            }
        }

        IEnumerator ReturnVFXInstanceAfterDelay(string vfxType, GameObject vfxInstance, float delay)
        {
            Debug.Log("Returning VFX instance after delay.");
            yield return new WaitForSeconds(delay);
            m_VFXPoolManager?.ReturnVFXInstance(vfxType, vfxInstance);
        }
    }
}
