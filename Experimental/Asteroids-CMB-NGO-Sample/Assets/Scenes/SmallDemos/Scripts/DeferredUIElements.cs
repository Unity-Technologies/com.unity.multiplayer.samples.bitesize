using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// When scene management is disabled, this helps
/// the asteroid instance find the disabled UI elements
/// </summary>
public class DeferredUIElements : MonoBehaviour
{
    public static DeferredUIElements Instance;
    public GameObject SpawnedInfo;
    public Text m_PerspectiveZoom;
    public Text m_PerspectiveName;

    public Text m_DeferredTicksValue;
    public Slider m_DeferredTicksSlider;

    private void Awake()
    {
        Instance = this;
    }
}
