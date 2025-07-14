using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This can be added to the same GameObject the NetworkManager component is assigned to in order to prevent
/// multiple NetworkManager instances from being instantiated if the same scene is loaded.
/// </summary>
public class AutoLoadScene : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    private string m_SceneToLoad;

#if UNITY_EDITOR
    public SceneAsset SceneToLoad;
    private void OnValidate()
    {
        if (SceneToLoad)
        {
            m_SceneToLoad = SceneToLoad.name;
        }
    }
#endif

    // Start is called before the first frame update
    private void Start()
    {
        SceneManager.LoadSceneAsync(m_SceneToLoad, LoadSceneMode.Single);
    }
}
