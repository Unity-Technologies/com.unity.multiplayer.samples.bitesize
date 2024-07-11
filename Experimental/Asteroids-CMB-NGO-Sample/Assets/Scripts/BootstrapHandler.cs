using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapHandler : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    private List<RuntimeSceneEntry> ScenesToLoad;

    [Serializable]
    public class RuntimeSceneEntry
    {
        public string SceneToLoad;
        public string DisplayName;
    }

#if UNITY_EDITOR
    [Serializable]
    public class SceneEntry
    {
        public SceneAsset SceneAsset;
        public string DisplayName;
    }

    public List<SceneEntry> SceneEntries;
    private void OnValidate()
    {
        ScenesToLoad = new List<RuntimeSceneEntry>();
        foreach (var sceneEntry in SceneEntries)
        {
            var displayName = sceneEntry.DisplayName == null || sceneEntry.DisplayName == string.Empty ? sceneEntry.SceneAsset.name : sceneEntry.DisplayName;
            ScenesToLoad.Add(new RuntimeSceneEntry()
            {
                DisplayName = displayName,
                SceneToLoad = sceneEntry.SceneAsset.name,
            });
        }
    }
#endif

    private bool IsLoadingScene;

    private void OnGUI()
    {
        if (IsLoadingScene)
        {
            return;
        }
        GUILayout.BeginArea(new Rect(10, 10, 300, 800));

        foreach (var sceneEntry in ScenesToLoad)
        {
            if (GUILayout.Button(sceneEntry.DisplayName))
            {
                SceneManager.LoadScene(sceneEntry.SceneToLoad);
            }
        }
        GUILayout.EndArea();
    }
}
