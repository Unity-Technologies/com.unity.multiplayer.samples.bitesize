using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DedicatedGameServerSample.Runtime
{
    [RequireComponent(typeof(UIDocument))]
    public class WorldSpaceUIHandler : MonoBehaviour
    {
        [SerializeField]
        UIDocument m_WorldspaceUI;

        Camera m_Camera;

        internal static WorldSpaceUIHandler Instance;

        const string k_WorldSpaceUIVisualElement = "WorldspaceVisualElement";

        VisualElement m_WorldSpaceElement;

        Dictionary<Transform, VisualElement> playersUI = new Dictionary<Transform, VisualElement>();

        void Awake()
        {
            m_Camera = FindFirstObjectByType<Camera>();
            if (Instance == null)
            {
                Instance = this;
            }

            // Get the root VisualElement from the UIDocument
            m_WorldSpaceElement = m_WorldspaceUI.rootVisualElement.Q<VisualElement>(k_WorldSpaceUIVisualElement);
        }

        internal void AddUIElement(VisualElement visualElement, Transform worldspaceTransform)
        {
            // TODO: validate addition

            // Add it to the root visual element
            m_WorldSpaceElement.Add(visualElement);

            // Track the player transform and its associated UI element
            playersUI[worldspaceTransform] = visualElement;
        }

        internal void RemoveUIElement(Transform worldspaceTransform)
        {
            if (playersUI.TryGetValue(worldspaceTransform, out var playerUI))
            {
                m_WorldSpaceElement.Remove(playerUI); // Remove the UI element from the hierarchy
                playersUI.Remove(worldspaceTransform);
            }
        }

        // TODO: validate billboarding
        void Update()
        {

            // offset in Y to have the UI above the player
            var verticalOffset = 2f;


            foreach (var entry in playersUI)
            {
                var playerPosition = entry.Key.position;
                var playerUI = entry.Value;

                // Translate the UI element to the player's position in pixel space of the UI Document.
                var pixelsPerUnit = m_WorldspaceUI.panelSettings.referenceSpritePixelsPerUnit;
                var x = new Length(playerPosition.x * pixelsPerUnit, LengthUnit.Pixel);
                var y = new Length((playerPosition.y + verticalOffset)* pixelsPerUnit * -1f  , LengthUnit.Pixel);
                var z = playerPosition.z * -1f * pixelsPerUnit;
                playerUI.style.translate = new StyleTranslate(new Translate(x, y, z));

                // Get Vector between the camera and the player
                var lookAtVector =  playerPosition - m_Camera.transform.position;
                // clear the Y component to only rotate around the Y axis
                lookAtVector.y = 0;
                playerUI.transform.rotation = Quaternion.LookRotation(lookAtVector, Vector3.up);
                //invert the rotation ( not sure why we need that but it was flipped)
                playerUI.transform.rotation = Quaternion.Inverse(playerUI.transform.rotation);
            }
        }
    }
}
