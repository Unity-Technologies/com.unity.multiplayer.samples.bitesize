using System.Collections.Generic;
using System.Linq;
using Unity.Multiplayer.Samples.SocialHub.Gameplay;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "InScenePlacedAssetConverter", menuName = "ScriptableObjects/InScenePlacedAssetConverter")]
public class InScenePlacedAssetConverter : ScriptableObject
{
#if UNITY_EDITOR
    public SceneAsset SceneToScan;
    public GameObject ReplacementPrefab;
    public GameObject PrefabToReplace;


    [ContextMenu("Replace All Instances")]
    internal void FindAndReplace()
    {
        ConvertObjects();
    }
    private void ConvertObjects()
    {
        var prefabsInSceneToReplace = PrefabUtility.FindAllInstancesOfPrefab(PrefabToReplace, SceneManager.GetActiveScene()).ToList<GameObject>();
        for(int i = prefabsInSceneToReplace.Count - 1; i >= 0; i--)
        {
            var insceneObject = prefabsInSceneToReplace[i];
            var objectSpawner = insceneObject.GetComponent<SessionOwnerNetworkObjectSpawner>();
            if (objectSpawner.NetworkObjectToSpawn.gameObject != ReplacementPrefab)
            {
                prefabsInSceneToReplace.RemoveAt(i);
            }
        }
        PrefabUtility.ReplacePrefabAssetOfPrefabInstances(prefabsInSceneToReplace.ToArray(), ReplacementPrefab, InteractionMode.UserAction);
    }
#endif
}
