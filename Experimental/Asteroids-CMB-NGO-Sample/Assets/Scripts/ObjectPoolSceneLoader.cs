using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ObjectPoolSceneLoader : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    private List<string> ObjectPoolScenesToLoad = new List<string>();
    
    private List<string> ScenesToLoad = new List<string>();

    public NetworkManagerHelper NetworkManagerHelper;

#if UNITY_EDITOR
    public List<SceneAsset> ObjectPoolScenes;

    private void OnValidate()
    {
        ObjectPoolScenesToLoad.Clear();
        foreach (var scene in ObjectPoolScenes)
        {
            ObjectPoolScenesToLoad.Add(scene.name);
        }
    }
#endif

    // Start is called before the first frame update
    void Start()
    {
        NetworkManagerHelper.CanStartGame = false;
        ScenesToLoad.Clear();
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        foreach(var sceneName in ObjectPoolScenesToLoad)
        {
            ScenesToLoad.Add(sceneName);
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }
    }

    private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (ScenesToLoad.Contains(scene.name))
        {
            ScenesToLoad.Remove(scene.name);
        }

        if (ScenesToLoad.Count == 0)
        {
            NetworkManagerHelper.CanStartGame = true;
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
        }
    }
}
