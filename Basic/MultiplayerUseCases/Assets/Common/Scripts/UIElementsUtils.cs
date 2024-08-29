using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.UIElements;

namespace Unity.Netcode.Samples.MultiplayerUseCases.Common
{
    /// <summary>
    /// An utility class for common UIElements setup method
    /// </summary>
    public static class UIElementsUtils
    {
#if UNITY_EDITOR
        static readonly string k_UIFilesPathInTemplate = Path.Combine("Assets", Path.Combine("Editor", "UI"));

        /// <summary>
        /// Loads an UXML file from an editor folder
        /// </summary>
        /// <param name="fileName">The name fo the file to load</param>
        /// <returns></returns>
        public static VisualTreeAsset LoadUXML(string fileName)
        {
            string path = $"{Path.Combine(k_UIFilesPathInTemplate, fileName)}.uxml";
            return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
        }
#endif

        /// <summary>
        /// Initializes a Button
        /// </summary>
        /// <param name="buttonName">Name of the button in the document</param>
        /// <param name="onClickAction">method to execute on click</param>
        /// <param name="isEnabled">Is the button enabled?</param>
        /// <param name="parent">Parent of the button</param>
        /// <param name="text">Text to display on the button</param>
        /// <param name="tooltip">Tooltip of the button</param>
        /// <param name="showIfEnabled">Enables the element if it supposed to be enabled</param>
        /// <returns>The initialized button</returns>
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

        /// <summary>
        /// Initializes an EnumField
        /// </summary>
        /// <typeparam name="T">Type of the values in the EnumField</typeparam>
        /// <param name="enumName">Name of the EnumField in the document</param>
        /// <param name="text">Text to display on the EnumField's label</param>
        /// <param name="onValueChanged">method to execute when the vlaue of EnumField changes</param>
        /// <param name="parent">Parent of the EnumField</param>
        /// <param name="defaultValue">Default vlaue of the Enumfield</param>
        /// <returns>The initialized EnumField</returns>
        public static EnumField SetupEnumField<T>(string enumName, string text, EventCallback<ChangeEvent<Enum>> onValueChanged, VisualElement parent, T defaultValue) where T : Enum
        {
            EnumField uxmlField = parent.Q<EnumField>(enumName);
            uxmlField.label = text;
            uxmlField.Init(defaultValue);
            uxmlField.value = defaultValue;
            uxmlField.RegisterCallback(onValueChanged);
            return uxmlField;
        }

        /// <summary>
        /// Initializes a Toggle
        /// </summary>
        /// <param name="name">Name of the toggle</param>
        /// <param name="label">Text of the toggle's label</param>
        /// <param name="text">Text of the toggle</param>
        /// <param name="defaultValue">Default value of the toggle</param>
        /// <param name="onValueChanged">Method to call when the value of the toggle changes</param>
        /// <param name="parent">Parent of the toggle</param>
        /// <returns>The initializedToggle</returns>
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

        /// <summary>
        /// Initializes an IntegerField
        /// </summary>
        /// <param name="name">Name of the IntegerField</param>
        /// <param name="value">Start value of the IntegerField</param>
        /// <param name="onValueChanged">Method to call when the value changes</param>
        /// <param name="parent">Parent of the IntegerField</param>
        /// <returns>The initialized IntegerField</returns>
        public static IntegerField SetupIntegerField(string name, int value, EventCallback<ChangeEvent<int>> onValueChanged, VisualElement parent)
        {
            IntegerField uxmlField = parent.Q<IntegerField>(name);
            uxmlField.value = value;
            uxmlField.SetEnabled(true);
            uxmlField.RegisterCallback(onValueChanged);
            return uxmlField;
        }

        /// <summary>
        /// Initializes an StringField
        /// </summary>
        /// <param name="name">Name of the StringField</param>
        /// <param name="label">Text of the StringField's label</param>
        /// <param name="value">Start value of the StringField</param>
        /// <param name="onValueChanged">Method to call when the value changes</param>
        /// <param name="parent">Parent of the StringField</param>
        /// <returns>The initialized StringField</returns>
        public static TextField SetupStringField(string name, string label, string value, EventCallback<ChangeEvent<string>> onValueChanged, VisualElement parent)
        {
            TextField uxmlField = parent.Q<TextField>(name);
            uxmlField.label = label;
            uxmlField.value = value;
            uxmlField.SetEnabled(true);
            uxmlField.RegisterCallback(onValueChanged);
            return uxmlField;
        }

        /// <summary>
        /// Makes a visual element invisible
        /// </summary>
        /// <param name="element">The element</param>
        public static void Hide(VisualElement element)
        {
            element.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Makes a visual element visible
        /// </summary>
        /// <param name="element">The element</param>
        public static void Show(VisualElement element)
        {
            element.style.display = DisplayStyle.Flex;
        }
    }
}
