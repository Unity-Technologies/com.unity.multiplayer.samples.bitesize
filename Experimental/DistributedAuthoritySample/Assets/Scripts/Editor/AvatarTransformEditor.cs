using com.unity.multiplayer.samples.socialhub.player;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.unity.multiplayer.samples.socialhub.editor
{
    [CustomEditor(typeof(AvatarTransform))]
    class DerivedComponentEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            // Create a new VisualElement to be the root of the inspector UI
            var root = new VisualElement();

            // Generate default inspector for AvatarTransform
            serializedObject.Update();
            SerializedProperty property = serializedObject.GetIterator();
            property.NextVisible(true); // Skip the script field
            while (property.NextVisible(false))
            {
                var propertyField = new PropertyField(property);
                root.Add(propertyField);
            }

            serializedObject.ApplyModifiedProperties();

            return root;
        }
    }
}
