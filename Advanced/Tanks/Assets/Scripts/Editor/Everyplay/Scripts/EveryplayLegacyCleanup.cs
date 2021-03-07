using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;

[InitializeOnLoad]
public class EveryplayLegacyCleanup
{
    private static string[] filesToRemove =
    {
        "Plugins/Everyplay/Scripts/EveryplayLegacy.cs",
        "Editor/PostprocessBuildPlayer_EveryplaySDK",
        "Editor/UpdateXcodeEveryplay.pyc",
        "Plugins/iOS/EveryplayGlesSupport.mm",
        "Plugins/iOS/EveryplayGlesSupport.h",
        "Plugins/iOS/EveryplayUnity.mm",
        "Plugins/iOS/EveryplayUnity.h",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-editor-panel-landscape.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-editor-panel-landscape@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-editor-panel-portrait.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-editor-panel-portrait@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-editor-topbar.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-editor-topbar@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-menu-notifcation-gradient.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-menu-notifcation-gradient@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-player-bottombar.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-player-bottombar@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-player-coverflow-gradient.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-player-coverflow-gradient@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-player-playcontrols.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-player-playcontrols@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-player-topbar.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-player-topbar@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-topbar-auth.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-topbar-auth@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-topbar.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-topbar@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-video-description-gradient.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-video-description-gradient@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-video-description-gradient~ipad.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/bg-video-description-gradient~ipad@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-delete.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-delete@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-landscape-share.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-landscape-share@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-landscape-share~ipad.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-landscape-share~ipad@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-landscape-toggled.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-landscape-toggled@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-landscape.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-landscape@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-player-back.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-player-back@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-player-done-press.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-player-done-press@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-player-done.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-player-done@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-player-trim.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-player-trim@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-portrait-share.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-portrait-share@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-portrait-share~ipad.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-portrait-share~ipad@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-portrait-toggled.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-portrait-toggled@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-portrait.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-portrait@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-retry.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-retry@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-topbar-green.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-topbar-green@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-undo-trim.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/btn-undo-trim@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-audio-active.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-audio-active@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-audio.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-audio@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-back-press.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-back-press@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-back.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-back@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-cam-active.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-cam-active@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-cam.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-cam@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-close-press.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-close-press@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-everyplay-large.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-everyplay-large@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-everyplay.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-everyplay@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-loading-error.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-loading-error@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-menu-press.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-menu-press@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-player-pause-press.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-player-pause-press@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-player-pause.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-player-pause@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-player-play-press.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-player-play-press@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-player-play-thumbnail-press.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-player-play-thumbnail-press@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-player-play.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-player-play@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-player-watch-again-press.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-player-watch-again-press@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-share-large.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-share-large@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-share.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/icon-share@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/menu-bottom-bg.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/menu-bottom-bg@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/menu-list-bg-active.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/menu-list-bg-active@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/menu-list-bg-large-active.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/menu-list-bg-large-active@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/menu-list-bg-large.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/menu-list-bg-large@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/menu-list-bg.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/menu-list-bg@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-editor-handle-highlighted.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-editor-handle-highlighted@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-editor-handle.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-editor-handle@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-handle-highlighted-left.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-handle-highlighted-left@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-handle-highlighted-right.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-handle-highlighted-right@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-handle-highlighted.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-handle-highlighted@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-handle-left.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-handle-left@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-handle-right.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-handle-right@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-handle.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-handle@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-track.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-track@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-trackBackground-loading.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-trackBackground-loading@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-trackBackground.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/slider-trackBackground@2x.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/splash-screen-bg.jpg",
        "Plugins/Everyplay/iOS/Everyplay.bundle/topbar-shadow.png",
        "Plugins/Everyplay/iOS/Everyplay.bundle/topbar-shadow@2x.png",
        "Plugins/Android/everyplay/res/drawable/everyplay_sidemenu_bg.png",
        "Plugins/Android/everyplay/res/drawable/everyplay_sidemenu_button_bg.png",
        "Plugins/Android/everyplay/res/drawable/everyplay_sidemenu_button_bg_active.png",
        "Plugins/Android/everyplay/res/values/everyplay_dimens.xml",
        "Plugins/Android/everyplay/res/values/everyplay_values.xml",
        "Plugins/Android/everyplay/res/layout-port/everyplay_editor_buttons.xml"
    };
    private const string oldPrefab = "Plugins/Everyplay/Everyplay.prefab";
    private const string newTestPrefab = "Plugins/Everyplay/Helpers/EveryplayTest.prefab";

    static EveryplayLegacyCleanup()
    {
        EditorApplication.update += Update;
    }

    private static int editorFrames = 0;
    private static int editorFramesToWait = 5;

    private static void Update()
    {
        if (editorFrames > editorFramesToWait)
        {
            Clean(true);

            EveryplayPostprocessor.ValidateEveryplayState(EveryplaySettingsEditor.LoadEveryplaySettings());
            EveryplayWelcome.ShowWelcome();

            EditorApplication.update -= Update;
        }
        else
        {
            editorFrames++;
        }
    }

    public static void Clean(bool silenceErrors)
    {
        foreach (string fileName in filesToRemove)
        {
            if (File.Exists(System.IO.Path.Combine(Application.dataPath, fileName)))
            {
                AssetDatabase.DeleteAsset(System.IO.Path.Combine("Assets", fileName));
                Debug.Log("Removed legacy Everyplay file: " + fileName);
            }
        }

        if (File.Exists(System.IO.Path.Combine(Application.dataPath, oldPrefab)))
        {
            if (File.Exists(System.IO.Path.Combine(Application.dataPath, newTestPrefab)))
            {
                AssetDatabase.DeleteAsset(System.IO.Path.Combine("Assets", oldPrefab));
                Debug.Log("Removed legacy Everyplay prefab: " + oldPrefab);
            }
            else
            {
                string src = System.IO.Path.Combine("Assets", oldPrefab);
                string dst = System.IO.Path.Combine("Assets", newTestPrefab);
                if ((AssetDatabase.ValidateMoveAsset(src, dst) == "") && (AssetDatabase.MoveAsset(src, dst) == ""))
                {
                    Debug.Log("Renamed and updated legacy Everyplay prefab " + oldPrefab + " to " + newTestPrefab);
                }
                else if (!silenceErrors)
                {
                    Debug.LogError("Updating the old Everyplay prefab failed. Please rename Plugins/Everyplay/Everyplay prefab to EveryplayTest and move it to the Plugins/Everyplay/Everyplay/Helpers folder.");
                }
            }
        }
    }
}
