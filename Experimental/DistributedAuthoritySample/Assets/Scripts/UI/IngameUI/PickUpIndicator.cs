using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// Indicator shown when a pickup is in range.
    /// </summary>
    public class PickUpIndicator : MonoBehaviour
    {
        [SerializeField]
        VisualTreeAsset m_PickupAsset;

        [SerializeField]
        Camera m_Camera;

        [SerializeField]
        float m_VerticalOffset = 1.5f;

        [SerializeField]
        UIDocument m_WorldspaceUI;

        VisualElement m_PickupUI;

        Transform m_CurrentPickup;

        Transform m_NextPickup;

        bool m_IsShown = false;

        bool isShown
        {
            set
            {
                // if the value is the same, do nothing
                if (m_IsShown == value)
                    return;

                m_IsShown = value;

                // fade in the pickup UI
                if (m_IsShown)
                {
                    m_PickupUI.RemoveFromClassList(UIUtils.inactiveUSSClass);
                    m_PickupUI.AddToClassList(UIUtils.activeUSSClass);
                    return;
                }

                // fade out the pickup UI
                m_PickupUI.RemoveFromClassList(UIUtils.activeUSSClass);
                m_PickupUI.AddToClassList(UIUtils.inactiveUSSClass);
            }
        }

        void OnEnable()
        {
            // pick first child to avoid adding the root element
            m_PickupUI = m_PickupAsset.CloneTree().GetFirstChild();
            m_PickupUI.AddToClassList(UIUtils.inactiveUSSClass);
            m_WorldspaceUI.rootVisualElement.Q<VisualElement>("Pickup").Add(m_PickupUI);
        }

        public void ShowPickup(Transform t)
        {
            m_NextPickup = t;
        }

        public void ClearPickup()
        {
            m_NextPickup = null;
        }

        private void Update()
        {
            if (m_CurrentPickup == m_NextPickup)
            {
                if (m_CurrentPickup != null)
                {
                    isShown = true;
                    UpdatePickup();
                }

                return;
            }

            isShown = false;
            if (m_PickupUI.resolvedStyle.opacity == 0)
            {
                m_CurrentPickup = m_NextPickup;
            }
        }

        void UpdatePickup()
        {
            UIUtils.TransformUIDocumentWorldspace(m_WorldspaceUI, m_Camera, m_CurrentPickup, m_VerticalOffset);
        }
    }
}
