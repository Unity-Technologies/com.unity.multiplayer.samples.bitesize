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

        VisualElement m_WorldSpaceElement;             // Root UI container

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
            // Create a new VisualElement for the player name
            var playerNameLabel = new Label("Name");
            playerNameLabel.style.fontSize = 24;
            playerNameLabel.style.color = new StyleColor(Color.white);
            playerNameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            playerNameLabel.style.position = Position.Absolute; // Allow explicit positioning

            // Add it to the root visual element
            m_WorldSpaceElement.Add(playerNameLabel);

            // Track the player transform and its associated UI element
            playersUI[worldspaceTransform] = playerNameLabel;
        }

        internal void RemoveUIElement(Transform worldspaceTransform)
        {
            if (playersUI.TryGetValue(worldspaceTransform, out var playerUI))
            {
                m_WorldSpaceElement.Remove(playerUI); // Remove the UI element from the hierarchy
                playersUI.Remove(worldspaceTransform);
            }
        }

        void Update()
        {
            // Update the position of each player's UI element to follow their transform
            foreach (var entry in playersUI)
            {
                var playerTransform = entry.Key;
                var playerUI = entry.Value;

                // Convert player's world position to screen position
                var screenPoint = m_Camera.WorldToViewportPoint(playerTransform.position + Vector3.up * 2f); // Offset above the player's head

                if (screenPoint.z > 0) // Ensure player is visible (in front of the camera)
                {
                    // Update the position of the UI
                    playerUI.style.left = new Length(screenPoint.x * m_WorldSpaceElement.resolvedStyle.width, LengthUnit.Pixel);
                    playerUI.style.top = new Length((1 - screenPoint.y) * m_WorldSpaceElement.resolvedStyle.height, LengthUnit.Pixel);
                    playerUI.style.display = DisplayStyle.Flex; // Ensure the UI is visible
                }
                else
                {
                    // Hide UI elements when player is behind the camera
                    playerUI.style.display = DisplayStyle.None;
                }
            }
        }
    }
}
