using Unity.Multiplayer.Samples.SocialHub.Physics;
using Unity.Netcode.Editor;
using UnityEditor;

namespace Unity.Multiplayer.Samples.SocialHub.Editor
{

    /// <summary>
    /// The custom editor for the <see cref="BaseObjectMotionHandler"/> component.
    /// </summary>
    [CustomEditor(typeof(BaseObjectMotionHandler), true)]
    public class BaseObjectMotionHandlerEditor : NetworkTransformEditor
    {
        private SerializedProperty m_IsPooled;
        private SerializedProperty m_CollisionType;
        private SerializedProperty m_CollisionDamage;
        private SerializedProperty m_DebugCollisions;
        private SerializedProperty m_DebugDamage;
        private SerializedProperty m_Colliders;

        public override void OnEnable()
        {
            m_IsPooled = serializedObject.FindProperty(nameof(BaseObjectMotionHandler.IsPooled));
            m_Colliders = serializedObject.FindProperty(nameof(BaseObjectMotionHandler.Colliders));
            m_CollisionType = serializedObject.FindProperty(nameof(BaseObjectMotionHandler.CollisionType));
            m_CollisionDamage = serializedObject.FindProperty(nameof(BaseObjectMotionHandler.CollisionDamage));
            m_DebugCollisions = serializedObject.FindProperty(nameof(BaseObjectMotionHandler.DebugCollisions));
            m_DebugDamage = serializedObject.FindProperty(nameof(BaseObjectMotionHandler.DebugDamage));

            base.OnEnable();
        }

        private void DisplayBaseObjectMotionHandlerProperties()
        {
            EditorGUILayout.PropertyField(m_IsPooled);
            EditorGUILayout.PropertyField(m_Colliders);
            EditorGUILayout.PropertyField(m_CollisionType);
            EditorGUILayout.PropertyField(m_CollisionDamage);
            EditorGUILayout.PropertyField(m_DebugCollisions);
            EditorGUILayout.PropertyField(m_DebugDamage);
        }

        public override void OnInspectorGUI()
        {
            var baseObjectMotionHandler = target as BaseObjectMotionHandler;
            void SetExpanded(bool expanded) { baseObjectMotionHandler.BaseObjectMotionHandlerPropertiesVisible = expanded; };
            DrawFoldOutGroup<BaseObjectMotionHandler>(baseObjectMotionHandler.GetType(), DisplayBaseObjectMotionHandlerProperties, baseObjectMotionHandler.BaseObjectMotionHandlerPropertiesVisible, SetExpanded);
            base.OnInspectorGUI();
        }
    }
}
