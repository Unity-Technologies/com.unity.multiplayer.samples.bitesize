using Unity.Multiplayer.Samples.SocialHub.Physics;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.Editor
{
    [CustomEditor(typeof(PhysicsObjectMotion))]
    class PhysicsObjectMotionEditor : UnityEditor.Editor
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

    /*/// <summary>
    /// The custom editor for the <see cref="PhysicsObjectMotion"/> component.
    /// </summary>
    [CustomEditor(typeof(PhysicsObjectMotion), true)]
    public class PhysicsObjectMotionEditor : BaseObjectMotionHandlerEditor
    {
        private SerializedProperty m_CollisionImpulseEntries;
        private SerializedProperty m_MaxAngularVelocity;
        private SerializedProperty m_MaxVelocity;
        private SerializedProperty m_MinMaxStartingTorque;
        private SerializedProperty m_MinMaxStartingForce;

        public override void OnEnable()
        {
            m_CollisionImpulseEntries = serializedObject.FindProperty(nameof(PhysicsObjectMotion.CollisionImpulseEntries));
            m_MaxAngularVelocity = serializedObject.FindProperty(nameof(PhysicsObjectMotion.MaxAngularVelocity));
            m_MaxVelocity = serializedObject.FindProperty(nameof(PhysicsObjectMotion.MaxVelocity));
            m_MinMaxStartingTorque = serializedObject.FindProperty(nameof(PhysicsObjectMotion.MinMaxStartingTorque));
            m_MinMaxStartingForce = serializedObject.FindProperty(nameof(PhysicsObjectMotion.MinMaxStartingForce));
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            var physicsObject = target as PhysicsObjectMotion;

            physicsObject.PhysicsObjectMotionPropertiesVisible = EditorGUILayout.BeginFoldoutHeaderGroup(physicsObject.PhysicsObjectMotionPropertiesVisible, $"{nameof(PhysicsObjectMotion)} Properties");
            if (physicsObject.PhysicsObjectMotionPropertiesVisible)
            {
                // End the header group since m_MinMaxStartingTorque and m_MinMaxStartingForce both use header groups
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.PropertyField(m_CollisionImpulseEntries);
                EditorGUILayout.PropertyField(m_MaxAngularVelocity);
                EditorGUILayout.PropertyField(m_MaxVelocity);
                EditorGUILayout.PropertyField(m_MinMaxStartingTorque);
                EditorGUILayout.PropertyField(m_MinMaxStartingForce);
            }
            else
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }
    }*/
}
