using Unity.Multiplayer.Samples.SocialHub.Physics;
using UnityEditor;

namespace Unity.Multiplayer.Samples.SocialHub.Editor
{
    /// <summary>
    /// The custom editor for the <see cref="PhysicsObjectMotion"/> component.
    /// </summary>
    [CustomEditor(typeof(PhysicsObjectMotion), true)]
    public class PhysicsObjectMotionEditor : BaseObjectMotionHandlerEditor
    {
        private SerializedProperty m_MaxAngularVelocity;
        private SerializedProperty m_MaxVelocity;

        public override void OnEnable()
        {
            m_MaxAngularVelocity = serializedObject.FindProperty(nameof(PhysicsObjectMotion.MaxAngularVelocity));
            m_MaxVelocity = serializedObject.FindProperty(nameof(PhysicsObjectMotion.MaxVelocity));
            base.OnEnable();
        }

        private void DisplayPhysicsObjectMotionProperties()
        {
            EditorGUILayout.PropertyField(m_MaxAngularVelocity);
            EditorGUILayout.PropertyField(m_MaxVelocity);
        }

        public override void OnInspectorGUI()
        {
            var physicsObject = target as PhysicsObjectMotion;
            void SetExpanded(bool expanded) { physicsObject.PhysicsObjectMotionPropertiesVisible = expanded; };
            DrawFoldOutGroup<PhysicsObjectMotion>(physicsObject.GetType(), DisplayPhysicsObjectMotionProperties, physicsObject.PhysicsObjectMotionPropertiesVisible, SetExpanded);
            base.OnInspectorGUI();
        }
    }
}
