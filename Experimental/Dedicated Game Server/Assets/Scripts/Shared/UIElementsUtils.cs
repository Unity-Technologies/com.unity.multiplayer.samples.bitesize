using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace Unity.Template.Multiplayer.NGO.Shared
{
    /// <summary>
    /// An utility class for common UIElements setup method
    /// </summary>
    public static class UIElementsUtils
    {
        public static readonly string k_UIResourcesPath = $"Packages/com.unity.template.multiplayer-ngo/Editor/UI/";
#if UNITY_EDITOR
        /// <summary>
        /// Loads an asset from the common UI resource folder.
        /// </summary>
        /// <typeparam name="T">type fo the file to load</typeparam>
        /// <param name="filename">name of the file</param>
        /// <returns>A reference to the loaded file</returns>
        public static T LoadUIAsset<T>(string filename) where T : UnityObject => AssetDatabase.LoadAssetAtPath<T>($"{k_UIResourcesPath}/{filename}");

        public static VisualTreeAsset LoadUXML(string fileName)
        {
            string path = $"{k_UIResourcesPath}{fileName}.uxml";
            return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
        }
#endif

        public static Button SetupButton(string buttonName, Action onClickAction, bool isEnabled, VisualElement parent, string text = "", string tooltip = "", bool showIfEnabled = true)
        {
            Button button = parent.Query<Button>(buttonName);
            button.SetEnabled(isEnabled);
            button.clickable = new Clickable(() => onClickAction?.Invoke());
            button.text = text;
            button.tooltip = string.IsNullOrEmpty(tooltip) ? button.text : tooltip;

            if (showIfEnabled && isEnabled)
            {
                Show(button);
            }

            return button;
        }

        public static EnumField SetupEnumField<T>(string enumName, string text, EventCallback<ChangeEvent<Enum>> onValueChanged, VisualElement parent, T defaultValue) where T : Enum
        {
            EnumField uxmlField = parent.Q<EnumField>(enumName);
            uxmlField.label = text;
            uxmlField.Init(defaultValue);
            uxmlField.value = defaultValue;
            uxmlField.RegisterCallback(onValueChanged);
            return uxmlField;
        }

        public static Toggle SetupToggle(string name, string label, string text, bool defaultValue, EventCallback<ChangeEvent<bool>> onValueChanged, VisualElement parent)
        {
            Toggle uxmlField = parent.Q<Toggle>(name);
            uxmlField.label = label;
            uxmlField.text = text;
            uxmlField.value = defaultValue;
            uxmlField.SetEnabled(true);
            uxmlField.RegisterCallback(onValueChanged);
            return uxmlField;
        }

        public static IntegerField SetupIntegerField(string name, int value, EventCallback<ChangeEvent<int>> onValueChanged, VisualElement parent)
        {
            IntegerField uxmlField = parent.Q<IntegerField>(name);
            uxmlField.value = value;
            uxmlField.SetEnabled(true);
            uxmlField.RegisterCallback(onValueChanged);
            return uxmlField;
        }

        public static TextField SetupStringField(string name, string label, string value, EventCallback<ChangeEvent<string>> onValueChanged, VisualElement parent)
        {
            TextField uxmlField = parent.Q<TextField>(name);
            uxmlField.label = label;
            uxmlField.value = value;
            uxmlField.SetEnabled(true);
            uxmlField.RegisterCallback(onValueChanged);
            return uxmlField;
        }

        public static void ShowOrHide(string elementName, VisualElement parent, bool show)
        {
            if (show)
            {
                Show(elementName, parent);
                return;
            }
            Hide(elementName, parent);
        }

        public static void Hide(string elementName, VisualElement parent)
        {
            Hide(parent.Query<VisualElement>(elementName));
        }

        public static void Show(string elementName, VisualElement parent)
        {
            Show(parent.Query<VisualElement>(elementName));
        }

        public static void Hide(VisualElement element)
        {
            element.style.display = DisplayStyle.None;
        }

        public static void Show(VisualElement element)
        {
            element.style.display = DisplayStyle.Flex;
        }
    }
}
