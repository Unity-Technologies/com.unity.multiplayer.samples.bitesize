using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Effects
{
    /// <summary>
    /// To be used in conjunction with <see cref="FXPrefabPool"/>, derive your
    /// FX specific component that manages the FX instance from this.
    /// </summary>
    class BaseFxObject : MonoBehaviour
    {
        FXPrefabPool m_FXPrefabPool;

        public void SetFxPool(FXPrefabPool pool)
        {
            m_FXPrefabPool = pool;
        }

        internal void StopFx()
        {
            if (gameObject.activeInHierarchy)
            {
                m_FXPrefabPool.ReleaseInstance(gameObject);
            }
        }
    }
}
