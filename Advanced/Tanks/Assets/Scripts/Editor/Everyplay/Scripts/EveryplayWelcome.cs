using UnityEngine;
using UnityEditor;
using System.Collections;

public class EveryplayWelcome : EditorWindow
{
    private Texture2D welcomeTexture2d = null;
    private GUIStyle welcomeStyle = null;

    public static void ShowWelcome()
    {
        if (!EditorPrefs.HasKey("EveryplayWelcomeShown"))
        {
            Texture2D texture = (Texture2D) EditorGUIUtility.Load("Everyplay/Images/everyplay-welcome.png");

            if (texture != null)
            {
                GUIStyle style = new GUIStyle();
                style.margin = new RectOffset(0, 0, 0, 0);
                style.padding = new RectOffset(0, 0, 0, 0);
                style.alignment = TextAnchor.MiddleCenter;

                if (style != null)
                {
                    EditorPrefs.SetBool("EveryplayWelcomeShown", true);
                    EveryplayWelcome window = (EveryplayWelcome) GetWindow(typeof(EveryplayWelcome), true, "Welcome to Everyplay!");
                    window.position = new Rect(196, 196, texture.width, texture.height);
                    window.minSize = new Vector2(texture.width, texture.height);
                    window.maxSize = new Vector2(texture.width, texture.height);
                    window.welcomeTexture2d = texture;
                    window.welcomeStyle = style;
                    window.Show();

                    EveryplaySettingsEditor.ShowSettings();
                }
            }
        }
    }

    void OnGUI()
    {
        if (welcomeStyle == null || welcomeTexture2d == null)
        {
            return;
        }

        if (GUI.Button(new Rect(0, 0, welcomeTexture2d.width, welcomeTexture2d.height), welcomeTexture2d, welcomeStyle))
        {
            Close();
            EveryplaySettingsEditor.ShowSettings();
            Application.OpenURL("https://developers.everyplay.com/");
        }
    }
}
