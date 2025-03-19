using System;
using System.IO;
using Unity.Multiplayer.Tools.Editor.MultiplayerToolsWindow;
using Unity.Plastic.Newtonsoft.Json.Linq;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEditor;
using Unity.Tutorials.Core.Editor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.Editor.Tutorials
{
    /// <summary>
    /// Implement your Tutorial callbacks here.
    /// </summary>
    [CreateAssetMenu(fileName = k_DefaultFileName, menuName = "Tutorials/" + k_DefaultFileName + " Instance")]
    public class TutorialCallbacks : ScriptableObject
    {
        [SerializeField] SceneAsset m_BootstrapScene;

        /// <summary>
        /// The default file name used to create asset of this class type.
        /// </summary>
        const string k_DefaultFileName = "TutorialCallbacks";

        const string k_SystemDataPath = "../Library/VP/SystemData.json";

        bool m_IsEditorWindowFocused;

        const float k_QuerySessionsInterval = 5f;

        bool m_IsSessionCreatedByVirtualPlayer;

        bool m_IsSessionJoinedByEditor;

        float m_TimeSinceLastSessionUpdate;

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
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(m_BootstrapScene));
        }

        public bool IsConnectedToUgs()
        {
            return CloudProjectSettings.projectBound;
        }

        public void ShowServicesSettings()
        {
            SettingsService.OpenProjectSettings("Project/Services");
        }

        [ContextMenu("Show Vivox Settings")]
        public void ShowVivoxSettings()
        {
            SettingsService.OpenProjectSettings("Project/Services/Vivox");
        }

        public bool IsVirtualPlayerCreated()
        {
            var path = Path.Combine(Application.dataPath, k_SystemDataPath);

            if (File.Exists(path))
            {
                string jsonContent = File.ReadAllText(path);

                // Parse the JSON content using JObject
                var jsonObject = JObject.Parse(jsonContent);

                // Access the "Data" property and then the "2" player's "Active" state
                var isPlayer2Active = jsonObject["Data"]["2"]["Active"].Value<bool>();

                return isPlayer2Active;
            }

            return false;
        }

        public void OnOpenMultiplayerToolsWindowTutorialStarted()
        {
            MultiplayerToolsWindow.Open();
            m_IsEditorWindowFocused = false;
        }

        public bool IsSceneViewFocused()
        {
            if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.titleContent.text == "Scene")
            {
                m_IsEditorWindowFocused = true;
            }

            return m_IsEditorWindowFocused;
        }

        VisualElement m_SceneRoot;

        public void OnEnableNetSceneVisTutorialStarted()
        {
            m_SceneRoot = EditorWindow.GetWindow<SceneView>().rootVisualElement;
            while (m_SceneRoot.parent != null)
            {
                m_SceneRoot = m_SceneRoot.parent;
            }
        }

        public bool IsNetworkVisualizationOverlayDisplayed()
        {
            return m_SceneRoot != null && m_SceneRoot.Q<VisualElement>("NetVisToolbarOverlay") != null;
        }

        public void ForceNetworkVisualizationOverlayDisplayed()
        {
            if (m_SceneRoot.Q<VisualElement>("NetVisToolbarOverlay") == null)
            {
                var netSceneVis = m_SceneRoot.Q<VisualElement>("Network Visualization");
                var netSceneVisButton = netSceneVis.Q<Button>();
                using (var e = new NavigationSubmitEvent())
                {
                    e.target = netSceneVisButton;
                    netSceneVisButton.SendEvent(e);
                }
            }
        }

        public bool IsVirtualPlayerSessionCreated()
        {
            return m_IsSessionCreatedByVirtualPlayer;
        }

        public void OnCreatingSessionTutorialStarted()
        {
            m_IsSessionCreatedByVirtualPlayer = false;
            m_TimeSinceLastSessionUpdate = Time.realtimeSinceStartup;
        }

        public void QuerySessions()
        {
            if (UnityServices.Instance == null || AuthenticationService.Instance == null || !AuthenticationService.Instance.IsAuthorized)
            {
                return;
            }

            if (Time.realtimeSinceStartup - m_TimeSinceLastSessionUpdate > k_QuerySessionsInterval)
            {
                m_TimeSinceLastSessionUpdate = Time.realtimeSinceStartup;
                QuerySessionsAsync();
            }
        }

        async void QuerySessionsAsync()
        {
            var task = MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());
            await task;
            if (task.IsCompleted)
            {
                // todo: add more criteria here
                m_IsSessionCreatedByVirtualPlayer = task.Result.Sessions.Count > 0;
            }
        }

        public bool IsSessionJoinedByEditor()
        {
            return m_IsSessionJoinedByEditor;
        }

        public void OnJoiningSessionTutorialStarted()
        {
            m_IsSessionJoinedByEditor = false;
            m_TimeSinceLastSessionUpdate = Time.realtimeSinceStartup;
        }

        public void QueryJoinedSessions()
        {
            if (UnityServices.Instance == null || AuthenticationService.Instance == null || !AuthenticationService.Instance.IsAuthorized)
            {
                return;
            }

            if (Time.realtimeSinceStartup - m_TimeSinceLastSessionUpdate > k_QuerySessionsInterval)
            {
                m_TimeSinceLastSessionUpdate = Time.realtimeSinceStartup;
                QueryJoinedSessionsAsync();
            }
        }

        async void QueryJoinedSessionsAsync()
        {
            var task = MultiplayerService.Instance.GetJoinedSessionIdsAsync();
            await task;
            if (task.IsCompleted)
            {
                if (task.Result.Count > 0)
                {
                    var joinedSessionId = task.Result[0];
                    m_IsSessionJoinedByEditor = true;
                    foreach (var session in MultiplayerService.Instance.Sessions)
                    {
                        if (session.Value.Id == joinedSessionId)
                        {
                            m_JoinedSession = session.Value;
                            break;
                        }
                    }
                }
            }
        }

        public bool IsOnlyEditorInSession()
        {
            return m_JoinedSession != null && m_JoinedSession.Players.Count == 1 && m_JoinedSession.IsHost;
        }
    }
}
