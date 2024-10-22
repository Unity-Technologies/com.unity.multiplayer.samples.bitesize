using Unity.Multiplayer.Samples.SocialHub.Player;
using Unity.Multiplayer.Samples.SocialHub.Physics;
using UnityEditor;

namespace Unity.Multiplayer.Samples.SocialHub.Editor
{
    /// <summary>
    /// The custom editor for the <see cref="PhysicsObjectMotion"/> component.
    /// </summary>
    [CustomEditor(typeof(PhysicsObjectMotion), true)]
    public class AvatarTransformEditor : PhysicsObjectMotionEditor
    {
        public SerializedProperty m_PlayerInput;

        public SerializedProperty m_AvatarInputs;

        public SerializedProperty m_AvatarInteractions;

        public SerializedProperty m_PhysicsPlayerController;

        public override void OnEnable()
        {
            m_PlayerInput = serializedObject.FindProperty(nameof(AvatarTransform.PlayerInput));
            m_AvatarInputs = serializedObject.FindProperty(nameof(AvatarTransform.AvatarInputs));
            m_AvatarInteractions = serializedObject.FindProperty(nameof(AvatarTransform.AvatarInteractions));
            m_PhysicsPlayerController = serializedObject.FindProperty(nameof(AvatarTransform.PhysicsPlayerController));
            base.OnEnable();
        }

        private void DisplayAvatarTransformProperties()
        {
            EditorGUILayout.PropertyField(m_PlayerInput);
            EditorGUILayout.PropertyField(m_AvatarInputs);
            EditorGUILayout.PropertyField(m_AvatarInteractions);
            EditorGUILayout.PropertyField(m_PhysicsPlayerController);
        }

        public override void OnInspectorGUI()
        {
            var avatarObject = target as AvatarTransform;
            void SetExpanded(bool expanded) { avatarObject.AvatarTransformPropertiesVisible = expanded; };
            DrawFoldOutGroup<AvatarTransform>(avatarObject.GetType(), DisplayAvatarTransformProperties, avatarObject.AvatarTransformPropertiesVisible, SetExpanded);
            base.OnInspectorGUI();
        }
    }
}
