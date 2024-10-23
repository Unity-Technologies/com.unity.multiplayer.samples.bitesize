using Unity.Multiplayer.Samples.SocialHub.Player;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.Editor
{
    [CustomEditor(typeof(AvatarTransform))]
    class AvatarTransformEditor : UnityEditor.Editor
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
