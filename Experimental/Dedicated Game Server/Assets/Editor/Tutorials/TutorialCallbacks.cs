using UnityEngine;
using UnityEditor;
using Unity.Tutorials.Core.Editor;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Editor
{

    /// <summary>
    /// Implement your Tutorial callbacks here.
    /// </summary>
    [CreateAssetMenu(fileName = DefaultFileName, menuName = "Tutorials/" + DefaultFileName + " Instance")]
    public class TutorialCallbacks : ScriptableObject
    {
        [SerializeField] SceneAsset m_MetagameScene;

        /// <summary>
        /// The default file name used to create asset of this class type.
        /// </summary>
        public const string DefaultFileName = "TutorialCallbacks";

        /// <summary>
        /// Creates a TutorialCallbacks asset and shows it in the Project window.
        /// </summary>
        /// <param name="assetPath">
        /// A relative path to the project's root. If not provided, the Project window's currently active folder path is used.
        /// </param>
        /// <returns>The created asset</returns>
        public static ScriptableObject CreateAndShowAsset(string assetPath = null)
        {
            assetPath ??= $"{TutorialEditorUtils.GetActiveFolderPath()}/{DefaultFileName}.asset";
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

        public void FocusGameView()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
        }

        public void FocusSceneView()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Scene");
        }

        public bool IsRunningAsHost()
        {
            return NetworkManager.Singleton && NetworkManager.Singleton.IsHost;
        }

        public bool IsRunningAsServerOnly()
        {
            return NetworkManager.Singleton && NetworkManager.Singleton.IsServer
                                            && !NetworkManager.Singleton.IsClient;
        }

        public bool IsRunningAsClientOnly()
        {
            return NetworkManager.Singleton && !NetworkManager.Singleton.IsServer
                                            && NetworkManager.Singleton.IsClient;
        }

        public bool IsPlayerSelectedInRPCScene()
        {
            return Selection.activeObject && Selection.activeObject.name == "Player(Clone)";
        }

        public void OpenURL(string url)
        {
            TutorialEditorUtils.OpenUrl(url);
        }

        public void LoadMetagameScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(m_MetagameScene));
        }

        public bool IsBootstrapperSetupProperly(int requiredPlayers)
        {
            var boostrapperWindow = EditorWindow.GetWindow<BootstrapperWindow>();
            return boostrapperWindow.AutoConnectOnStartup
                && boostrapperWindow.MaxPlayers == requiredPlayers;
        }
    }
}