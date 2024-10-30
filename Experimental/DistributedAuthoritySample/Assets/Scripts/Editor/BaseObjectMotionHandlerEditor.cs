using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Multiplayer.Samples.SocialHub.Physics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BaseObjectMotionHandler), editorForChildClasses: true)]
    class BaseObjectMotionHandlerEditor : UnityEditor.Editor
    {
        HashSet<string> m_DrawnFields = new HashSet<string>();

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            Type currentType = target.GetType();
            var types = new List<Type>();

            // Gather all types up to MonoBehaviour
            while (currentType != typeof(MonoBehaviour) && currentType != null)
            {
                types.Add(currentType);
                currentType = currentType.BaseType;
            }

            // Iterate types in reverse (base class first)
            types.Reverse();

            foreach (var type in types)
            {
                Foldout foldout = CreateFoldoutForType(serializedObject, type);
                root.Add(foldout);
            }

            root.Bind(serializedObject);
            return root;
        }

        Foldout CreateFoldoutForType(SerializedObject obj, Type type)
        {
            var foldout = new Foldout { text = type.Name };

            // Check for custom drawing method
            MethodInfo customDrawMethod = type.GetMethod("CreateInspectorGUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (customDrawMethod != null && type != typeof(BaseObjectMotionHandler)) // ie. only include overriden methods
            {
                VisualElement customElement = (VisualElement)customDrawMethod.Invoke(obj.targetObject, new object[] { obj });
                foldout.Add(customElement);
                return foldout;
            }

            // Fallback to default drawing if no custom drawing method is found
            FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (var fieldInfo in fieldInfos)
            {
                if (m_DrawnFields.Contains(fieldInfo.Name))
                {
                    continue;
                }

                SerializedProperty property = obj.FindProperty(fieldInfo.Name);
                if (property != null)
                {
                    var propertyField = new PropertyField(property);
                    foldout.Add(propertyField);
                    m_DrawnFields.Add(fieldInfo.Name);
                }
            }

            return foldout;
        }
    }
}
