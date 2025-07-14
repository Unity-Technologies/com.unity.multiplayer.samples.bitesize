using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.Netcode.Samples.MultiplayerUseCases.Tests.Editor
{
    /// <summary>
    /// A helper editor script for finding missing references to objects.
    /// </summary>
    public class MissingReferencesFinder
    {
        /// <summary>
        /// Finds all missing references to objects in the currently loaded scene.
        /// </summary>
        static void FindMissingReferencesInCurrentScene()
        {
            var sceneObjects = GetSceneObjects();
            FindMissingReferences(EditorSceneManager.GetActiveScene().path, sceneObjects);
        }

        /// <summary>
        /// Finds all missing references to objects in all enabled scenes in the project.
        /// This works by loading the scenes one by one and checking for missing object references.
        /// </summary>
        internal static void FindMissingReferencesInAllBuiltScenes()
        {
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled)
                {
                    continue;
                }

                EditorSceneManager.OpenScene(scene.path);
                FindMissingReferencesInCurrentScene();
            }
        }

        /// <summary>
        /// Finds all missing references to objects in assets (objects from the project window).
        /// </summary>
        internal static void FindMissingReferencesInAssets()
        {
            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            var filteredAssetPaths = new List<string>();

            foreach (var path in allAssetPaths)
            {
                if (path.StartsWith("Assets/"))
                {
                    filteredAssetPaths.Add(path);
                }
            }

            var gameObjects = new List<GameObject>();

            foreach (var assetPath in filteredAssetPaths)
            {
                var obj = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
                if (obj != null)
                {
                    gameObjects.Add(obj);
                }
            }

            FindMissingReferences("Project", gameObjects.ToArray());
        }

        static void FindMissingReferences(string context, GameObject[] gameObjects)
        {
            if (gameObjects == null)
            {
                return;
            }

            foreach (var gameObject in gameObjects)
            {
                Component[] components = gameObject.GetComponents<Component>();
                foreach (var component in components)
                {
                    // Missing components will be null, we can't find their type, etc.
                    if (!component)
                    {
                        Debug.LogErrorFormat(gameObject, "[{0}] Missing Component in GameObject: {1}", context, GetFullPath(gameObject));
                        continue;
                    }

                    SerializedProperty property = new SerializedObject(component).GetIterator();
                    PropertyInfo objRefValueMethod = typeof(SerializedProperty).GetProperty("objectReferenceStringValue", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                    // Iterate over the components' properties.
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference)
                        {
                            continue;
                        }

                        string objectReferenceStringValue = string.Empty;
                        if (objRefValueMethod != null)
                        {
                            objectReferenceStringValue = (string)objRefValueMethod.GetGetMethod(true).Invoke(property, new object[] { });
                        }

                        if (property.objectReferenceValue == null
                        && (property.objectReferenceInstanceIDValue != 0 || objectReferenceStringValue.StartsWith("Missing")))
                        {
                            ShowError(context, gameObject, component.GetType().Name, ObjectNames.NicifyVariableName(property.name));
                        }
                    }
                }
            }
        }

        static GameObject[] GetSceneObjects()
        {
            // Use this method since GameObject.FindObjectsOfType will not return disabled objects.
            GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            var matchingGameObjects = new List<GameObject>();

            // Counting the number of game objects that meet the criteria
            foreach (var go in allGameObjects)
            {
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go)) && go.hideFlags == HideFlags.None)
                {
                    matchingGameObjects.Add(go);
                }
            }

            return matchingGameObjects.ToArray();
        }

        static void ShowError(string context, GameObject go, string componentName, string propertyName)
        {
            var ERROR_TEMPLATE = "Missing Ref in: [{3}]{0}. Component: {1}, Property: {2}";
            Debug.LogError(string.Format(ERROR_TEMPLATE, GetFullPath(go), componentName, propertyName, context), go);
        }

        static string GetFullPath(GameObject go)
        {
            return go.transform.parent == null ? go.name
                                               : Path.Combine(GetFullPath(go.transform.parent.gameObject), go.name);
        }
    }
}
