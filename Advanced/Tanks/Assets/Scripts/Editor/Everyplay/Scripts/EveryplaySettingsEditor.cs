#if !(UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7)
#define UNITY_5_OR_LATER
#endif

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.IO;

[CustomEditor(typeof(EveryplaySettings))]
public class EveryplaySettingsEditor : Editor
{
    public const string settingsFile = "EveryplaySettings";
    public const string settingsFileExtension = ".asset";
    public const string testButtonsResourceFile = "everyplay-test-buttons.png";

    private static GUIContent labelClientId = new GUIContent("Client id");
    private static GUIContent labelClientSecret = new GUIContent("Client secret");
    private static GUIContent labelRedirectURI = new GUIContent("Redirect URI");
    private static GUIContent labelSupport_iOS = new GUIContent("iOS enabled [?]", "Check to enable Everyplay replay recording on iOS devices");
    private static GUIContent labelSupport_tvOS = new GUIContent("tvOS enabled [?]", "Check to enable Everyplay replay recording on tvOS devices");
    private static GUIContent labelSupport_Android = new GUIContent("Android enabled [?]", "Check to enable Everyplay replay recording on Android devices");
    private static GUIContent labelSupport_Standalone = new GUIContent("Mac enabled [?]", "Check to enable Everyplay replay recording on Mac Unity Editor or Standalone player");
    private static GUIContent labelTestButtons = new GUIContent("Enable test buttons [?]", "Check to overlay easy-to-use buttons for testing Everyplay in your game");
    private static GUIContent labelEarlyInitializer = new GUIContent("Enable early initializer [?]", "Initialize Everyplay automatically as early as possible");
    private EveryplaySettings currentSettings = null;
    private bool iosSupportEnabled;
    private bool tvosSupportEnabled;
    private bool androidSupportEnabled;
    private bool standaloneSupportEnabled;
    private bool testButtonsEnabled;
    private bool earlyInitializerEnabled;

    [MenuItem("Edit/Everyplay Settings")]
    public static void ShowSettings()
    {
        EveryplaySettings settingsInstance = LoadEveryplaySettings();

        if (settingsInstance == null)
        {
            settingsInstance = CreateEveryplaySettings();
        }

        if (settingsInstance != null)
        {
            EveryplayPostprocessor.ValidateAndUpdateFacebook();
            EveryplayLegacyCleanup.Clean(false);
            Selection.activeObject = settingsInstance;
        }
    }

    public override void OnInspectorGUI()
    {
        try
        {
            // Might be null when this gui is open and this file is being reimported
            if (target == null)
            {
                Selection.activeObject = null;
                return;
            }

            currentSettings = (EveryplaySettings) target;

            if (currentSettings != null)
            {
                bool settingsValid = currentSettings.IsValid;

                EditorGUILayout.HelpBox("1) Enter your game credentials", MessageType.None);

                if (!currentSettings.IsValid)
                {
                    EditorGUILayout.HelpBox("Invalid or missing game credentials, Everyplay disabled. Check your game credentials at https://developers.everyplay.com/", MessageType.Error);
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(labelClientId, GUILayout.Width(108), GUILayout.Height(18));
                currentSettings.clientId = TrimmedText(EditorGUILayout.TextField(currentSettings.clientId));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(labelClientSecret, GUILayout.Width(108), GUILayout.Height(18));
                currentSettings.clientSecret = TrimmedText(EditorGUILayout.TextField(currentSettings.clientSecret));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(labelRedirectURI, GUILayout.Width(108), GUILayout.Height(18));
                currentSettings.redirectURI = TrimmedText(EditorGUILayout.TextField(currentSettings.redirectURI));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.HelpBox("2) Enable recording on these platforms", MessageType.None);

                EditorGUILayout.BeginVertical();

                bool validityChanged = currentSettings.IsValid != settingsValid;
                bool selectedPlatformsChanged = false;

                iosSupportEnabled = EditorGUILayout.Toggle(labelSupport_iOS, currentSettings.iosSupportEnabled);

                if (iosSupportEnabled != currentSettings.iosSupportEnabled)
                {
                    selectedPlatformsChanged = true;
                    currentSettings.iosSupportEnabled = iosSupportEnabled;
                    EditorUtility.SetDirty(currentSettings);
                }

                if (CheckForSDK_tvOS())
                {
                    tvosSupportEnabled = EditorGUILayout.Toggle(labelSupport_tvOS, currentSettings.tvosSupportEnabled);

                    if (tvosSupportEnabled != currentSettings.tvosSupportEnabled)
                    {
                        selectedPlatformsChanged = true;
                        currentSettings.tvosSupportEnabled = tvosSupportEnabled;
                        EditorUtility.SetDirty(currentSettings);
                    }
                }

                if (CheckForSDK_Android())
                {
                    androidSupportEnabled = EditorGUILayout.Toggle(labelSupport_Android, currentSettings.androidSupportEnabled);

                    if (androidSupportEnabled != currentSettings.androidSupportEnabled)
                    {
                        selectedPlatformsChanged = true;
                        currentSettings.androidSupportEnabled = androidSupportEnabled;
                        EditorUtility.SetDirty(currentSettings);
                    }
                }

                if (CheckForSDK_Mac())
                {
                    standaloneSupportEnabled = EditorGUILayout.Toggle(labelSupport_Standalone, currentSettings.standaloneSupportEnabled);

                    if (standaloneSupportEnabled != currentSettings.standaloneSupportEnabled)
                    {
                        selectedPlatformsChanged = true;
                        currentSettings.standaloneSupportEnabled = standaloneSupportEnabled;
                        EditorUtility.SetDirty(currentSettings);
                    }
                }

                if (validityChanged || selectedPlatformsChanged)
                {
                    EveryplayPostprocessor.ValidateEveryplayState(currentSettings);
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.HelpBox("3) Try out Everyplay", MessageType.None);

                EditorGUILayout.BeginVertical();
                earlyInitializerEnabled = EditorGUILayout.Toggle(labelEarlyInitializer, currentSettings.earlyInitializerEnabled);
                if (earlyInitializerEnabled != currentSettings.earlyInitializerEnabled)
                {
                    currentSettings.earlyInitializerEnabled = earlyInitializerEnabled;
                    EditorUtility.SetDirty(currentSettings);
                }
                testButtonsEnabled = EditorGUILayout.Toggle(labelTestButtons, currentSettings.testButtonsEnabled);
                if (testButtonsEnabled != currentSettings.testButtonsEnabled)
                {
                    currentSettings.testButtonsEnabled = testButtonsEnabled;
                    EditorUtility.SetDirty(currentSettings);
                    EnableTestButtons(testButtonsEnabled);
                }
                EditorGUILayout.EndVertical();
            }
        }
        catch (Exception e)
        {
            if (e != null)
            {
            }
        }
    }

    private static string TrimmedText(string txt)
    {
        if (txt != null)
        {
            return txt.Trim();
        }
        return "";
    }

    private static EveryplaySettings CreateEveryplaySettings()
    {
        EveryplaySettings everyplaySettings = (EveryplaySettings) ScriptableObject.CreateInstance(typeof(EveryplaySettings));

        if (everyplaySettings != null)
        {
            if (!Directory.Exists(System.IO.Path.Combine(Application.dataPath, "Plugins/Everyplay/Resources")))
            {
                AssetDatabase.CreateFolder("Assets/Plugins/Everyplay", "Resources");
            }

            AssetDatabase.CreateAsset(everyplaySettings, "Assets/Plugins/Everyplay/Resources/" + settingsFile + settingsFileExtension);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return everyplaySettings;
        }

        return null;
    }

    public static EveryplaySettings LoadEveryplaySettings()
    {
        return (EveryplaySettings) Resources.Load(settingsFile);
    }

    public void EnableTestButtons(bool enable)
    {
        string dstFile = "Plugins/Everyplay/Resources/" + testButtonsResourceFile;

        if (enable)
        {
            string sourceFile = "Plugins/Everyplay/Images/" + testButtonsResourceFile;
            if (!File.Exists(System.IO.Path.Combine(Application.dataPath, dstFile)) && File.Exists(System.IO.Path.Combine(Application.dataPath, sourceFile)))
            {
                AssetDatabase.CopyAsset("Assets/" + sourceFile, "Assets/" + dstFile);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        else
        {
            if (File.Exists(System.IO.Path.Combine(Application.dataPath, dstFile)))
            {
                AssetDatabase.DeleteAsset("Assets/" + dstFile);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }

    public static bool CheckForSDK_tvOS()
    {
        if (System.IO.Directory.Exists(System.IO.Path.Combine(Application.dataPath, "Plugins/Everyplay/tvOS")))
        {
            return true;
        }
        return false;
    }

    public static bool CheckForSDK_Android()
    {
        if (System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "Plugins/Android/everyplay/AndroidManifest.xml")) ||
            System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "Plugins/Android/Everyplay/AndroidManifest.xml")))
        {
            return true;
        }
        return false;
    }

    public static bool CheckForSDK_Mac()
    {
#if UNITY_5_OR_LATER
        if (System.IO.Directory.Exists(System.IO.Path.Combine(Application.dataPath, "Plugins/Everyplay/Mac")))
        {
            return true;
        }
#endif
        return false;
    }

    void OnDisable()
    {
        if (currentSettings != null)
        {
            EditorUtility.SetDirty(currentSettings);
            currentSettings = null;
        }
    }
}
