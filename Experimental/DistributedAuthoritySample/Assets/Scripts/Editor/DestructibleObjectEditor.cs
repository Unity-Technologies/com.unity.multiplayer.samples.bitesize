using Unity.Multiplayer.Samples.SocialHub.Gameplay;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.Editor
{
    /// <summary>
    /// The custom editor for the <see cref="PhysicsObjectMotion"/> component.
    /// </summary>
    [CustomEditor(typeof(DestructibleObject), true)]
    public class DesctructableObjectEditor : PhysicsObjectMotionEditor
    {
        public SerializedProperty m_StartingHealth;
        public SerializedProperty m_IntangibleDurationAfterDamage;
        public SerializedProperty m_SecondsUntilRespawn;
        public SerializedProperty m_DeferredDespawnTicks;
        public SerializedProperty m_TransferableObject;
        public SerializedProperty m_DestructionFX;
        public SerializedProperty m_RubblePrefab;
        public SerializedProperty m_DestructionVFXType;

        public override void OnEnable()
        {
            m_StartingHealth = serializedObject.FindProperty(nameof(DestructibleObject.StartingHealth));
            m_IntangibleDurationAfterDamage = serializedObject.FindProperty(nameof(DestructibleObject.IntangibleDurationAfterDamage));
            m_SecondsUntilRespawn = serializedObject.FindProperty(nameof(DestructibleObject.SecondsUntilRespawn));
            m_DeferredDespawnTicks = serializedObject.FindProperty(nameof(DestructibleObject.DeferredDespawnTicks));
            m_TransferableObject = serializedObject.FindProperty(nameof(DestructibleObject.TransferableObject));

            m_DestructionFX = serializedObject.FindProperty(nameof(DestructibleObject.DestructionFX));
            m_RubblePrefab = serializedObject.FindProperty(nameof(DestructibleObject.RubblePrefab));
            m_DestructionVFXType = serializedObject.FindProperty(nameof(DestructibleObject.DestructionVFXType));

            base.OnEnable();
        }

        private void DisplayDestructibleObjectProperties()
        {
            EditorGUILayout.PropertyField(m_StartingHealth);
            EditorGUILayout.PropertyField(m_IntangibleDurationAfterDamage);
            EditorGUILayout.PropertyField(m_SecondsUntilRespawn);
            EditorGUILayout.PropertyField(m_DeferredDespawnTicks);
            EditorGUILayout.PropertyField(m_TransferableObject);
            EditorGUILayout.PropertyField(m_DestructionFX);
            EditorGUILayout.PropertyField(m_RubblePrefab);
            EditorGUILayout.PropertyField(m_DestructionVFXType);
        }

        public override void OnInspectorGUI()
        {
            var destructibleObject = target as DestructibleObject;
            void SetExpanded(bool expanded) { destructibleObject.DestructibleObjectPropertiesVisible = expanded; };
            DrawFoldOutGroup<DestructibleObject>(destructibleObject.GetType(), DisplayDestructibleObjectProperties, destructibleObject.DestructibleObjectPropertiesVisible, SetExpanded);
            base.OnInspectorGUI();
        }
    }
}

