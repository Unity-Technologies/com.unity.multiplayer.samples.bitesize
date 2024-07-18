using UnityEngine;

/// <summary>
/// To be used in conjunction with <see cref="FXPrefabPool"/>, derive your
/// FX specific component that manages the FX instance from this.
/// </summary>
public class BaseFxObject : MonoBehaviour
{
    private FXPrefabPool m_FXPrefabPool;
    public void SetFxPool(FXPrefabPool pool)
    {
        m_FXPrefabPool = pool;
    }

    public void StopFx()
    {
        if (gameObject.activeInHierarchy) 
        {
            m_FXPrefabPool.ReleaseInstance(gameObject);
        }        
    }
}
