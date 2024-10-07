using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class PickUpIndicator : MonoBehaviour
    {
        [SerializeField]
        VisualTreeAsset m_PickupAsset;

        [SerializeField]
        Camera m_Camera;

        Transform m_CurrentPickup;
        VisualElement m_Root;

        VisualElement m_Pickup;
        UIDocument m_UIDocument;

        void OnEnable()
        {
            m_UIDocument = GetComponent<UIDocument>();
            m_Root = m_UIDocument.rootVisualElement;
            m_Pickup = m_PickupAsset.CloneTree().Children().First();
            m_UIDocument.rootVisualElement.Q<VisualElement>("Pickup").Add(m_Pickup);
        }

        public void ShowPickup(Transform t)
        {
            m_CurrentPickup = t;
        }

        public void ClearPickup()
        {

        }

        private void Update()
        {
            if (m_CurrentPickup != null)
            {
                UpdatePickupPosition();
            }
        }

        void UpdatePickupPosition()
        {
            // Get position of nameplate in screen space.
            Vector2 screenSpacePosition = m_Camera.WorldToScreenPoint(m_CurrentPickup.position);
            // Convert position to panel space.
            Vector2 panelSpacePosition = RuntimePanelUtils.ScreenToPanel(m_Root.panel, new Vector2(screenSpacePosition.x, Screen.height - screenSpacePosition.y));
            // Debug.Log("Frame:"+Time.frameCount +"----"+ panelSpacePosition.x + "-" + panelSpacePosition.y);
            // Translate panel to that position.

            m_Pickup.style.left = panelSpacePosition.x;
            m_Pickup.style.top = panelSpacePosition.y;
        }
    }
}
