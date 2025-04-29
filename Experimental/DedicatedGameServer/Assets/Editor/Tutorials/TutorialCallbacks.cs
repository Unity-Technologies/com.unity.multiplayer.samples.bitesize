using System;
using System.IO;
using Newtonsoft.Json.Linq;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEditor;
using Unity.Tutorials.Core.Editor;
using UnityEditor.SceneManagement;

namespace Unity.DedicatedGameServerSample.Editor.Tutorials
{
    /// <summary>
    /// Implement your Tutorial callbacks here.
    /// </summary>
    [CreateAssetMenu(fileName = k_DefaultFileName, menuName = "Tutorials/" + k_DefaultFileName + " Instance")]
    public class TutorialCallbacks : ScriptableObject
    {
        [SerializeField]
        SceneAsset m_BootstrapScene;

        [SerializeField]
        SceneAsset m_GameScene;

        /// <summary>
        /// The default file name used to create asset of this class type.
        /// </summary>
        const string k_DefaultFileName = "TutorialCallbacks";

        const string k_SystemDataPath = "../Library/VP/SystemData.json";

        bool m_IsEditorWindowFocused;

        bool m_IsSessionCreatedByVirtualPlayer;

        bool m_IsSessionJoinedByEditor;

        ISession m_JoinedSession;

        /// <summary>
        /// Creates a TutorialCallbacks asset and shows it in the Project window.
        /// </summary>
        /// <param name="assetPath">
        /// A relative path to the project's root. If not provided, the Project window's currently active folder path is used.
        /// </param>
        /// <returns>The created asset</returns>
        public static ScriptableObject CreateAndShowAsset(string assetPath = null)
        {
            assetPath = assetPath ?? $"{TutorialEditorUtils.GetActiveFolderPath()}/{k_DefaultFileName}.asset";
            var asset = CreateInstance<TutorialCallbacks>();
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(assetPath));
            EditorUtility.FocusProjectWindow(); // needed in order to make the selection of newly created asset to really work
            Selection.activeObject = asset;
            return asset;
        }

        public void StartTutorial(Tutorial tutorial)
        {
            TutorialWindow.StartTutorial(tutorial);
        }

        public void OpenURL(string url)
        {
            TutorialEditorUtils.OpenUrl(url);
        }

        public void LoadBootstrapScene()
        {
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(m_BootstrapScene));
        }

        public void LoadGameScene()
        {
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(m_GameScene));
        }

        public void SelectObjectInHierarchyByName(string objecName)
        {
            var obj = GameObject.Find(objecName);
            if (obj != null)
            {
                Selection.activeObject = obj;
            }
            else
            {
                Debug.LogError($"Object with name {objecName} not found in scene.");
            }
        }

        public bool IsConnectedToUgs()
        {
            return CloudProjectSettings.projectBound;
        }

        public void ShowServicesSettings()
        {
            SettingsService.OpenProjectSettings("Project/Services");
        }

        public void ShowMultiplayerRolesSettings()
        {
            SettingsService.OpenProjectSettings("Project/Multiplayer");
        }

        public bool IsVirtualPlayer4Created()
        {
            var path = Path.Combine(Application.dataPath, k_SystemDataPath);

            if (File.Exists(path))
            {
                string jsonContent = File.ReadAllText(path);

                // Parse the JSON content using JObject
                var jsonObject = JObject.Parse(jsonContent);

                // Access the "Data" property and then the "2" player's "Active" state
                var isPlayer4Active = jsonObject["Data"]["4"]["Active"].Value<bool>();

                return isPlayer4Active;
            }

            return false;
        }

        public bool IsVirtualPlayerSessionCreated()
        {
            return m_IsSessionCreatedByVirtualPlayer;
        }

        public void OnCreatingSessionTutorialStarted()
        {
            m_IsSessionCreatedByVirtualPlayer = false;
        }

        public void OpenPrefabView(GameObject prefab)
        {
            AssetDatabase.OpenAsset(prefab);
            SceneView.FrameLastActiveSceneView();
        }

        public void ExitPrefabView()
        {
            StageUtility.GoToMainStage();
        }
    }
}
