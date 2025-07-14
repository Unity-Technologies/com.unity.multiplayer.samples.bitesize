using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DedicatedGameServerSample.Runtime
{
    [RequireComponent(typeof(UIDocument))]
    public class WorldSpaceView : MonoBehaviour
    {
        [SerializeField]
        UIDocument m_WorldspaceUI;

        Camera m_Camera;

        internal static WorldSpaceView Instance;

        const string k_WorldSpaceUIVisualElement = "WorldspaceVisualElement";

        VisualElement m_WorldSpaceElement;

        Dictionary<Transform, VisualElement> m_PlayerVisualElements = new Dictionary<Transform, VisualElement>();

        // offset in Y to have the UI above the player
        const float k_VerticalOffset = 2.3f;

        void Awake()
        {
            m_Camera = FindFirstObjectByType<Camera>();
            if (Instance == null)
            {
                Instance = this;
            }

            // get the root VisualElement from the UIDocument
            m_WorldSpaceElement = m_WorldspaceUI.rootVisualElement.Q<VisualElement>(k_WorldSpaceUIVisualElement);
        }

        internal void AddUIElement(VisualElement visualElement, Transform worldspaceTransform)
        {
            // add it to the root visual element
            m_WorldSpaceElement.Add(visualElement);

            // track the player transform and its associated UI element
            m_PlayerVisualElements[worldspaceTransform] = visualElement;
        }

        internal void RemoveUIElement(Transform worldspaceTransform)
        {
            if (m_PlayerVisualElements.TryGetValue(worldspaceTransform, out var playerUI))
            {
                m_WorldSpaceElement.Remove(playerUI); // remove the UI element from the hierarchy
                m_PlayerVisualElements.Remove(worldspaceTransform);
            }
        }

        void Update()
        {
            foreach (var entry in m_PlayerVisualElements)
            {
                var playerPosition = entry.Key.position;
                var playerUI = entry.Value;

                // translate the UI element to the player's position in pixel space of the UI Document
                var pixelsPerUnit = m_WorldspaceUI.panelSettings.referenceSpritePixelsPerUnit;
                var x = new Length(playerPosition.x * pixelsPerUnit, LengthUnit.Pixel);
                var y = new Length((playerPosition.y + k_VerticalOffset)* pixelsPerUnit * -1f  , LengthUnit.Pixel);
                var z = playerPosition.z * -1f * pixelsPerUnit;
                playerUI.style.translate = new StyleTranslate(new Translate(x, y, z));

                // get Vector between the camera and the player
                var lookAtVector =  playerPosition - m_Camera.transform.position;
                // clear the Y component to only rotate around the Y axis
                lookAtVector.y = 0;
                playerUI.transform.rotation = Quaternion.LookRotation(lookAtVector, Vector3.up);
                // invert the rotation
                playerUI.transform.rotation = Quaternion.Inverse(playerUI.transform.rotation);
            }
        }
    }
}
