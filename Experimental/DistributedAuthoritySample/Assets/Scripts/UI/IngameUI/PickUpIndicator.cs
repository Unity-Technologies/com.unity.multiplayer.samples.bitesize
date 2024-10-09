using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
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

        const string k_ActiveUSSClass = "show";
        const string k_InactiveUSSClass = "hide";

        void OnEnable()
        {
            // pick first child to avoid adding the root element
            m_PickupUI = m_PickupAsset.CloneTree().Children().ToArray()[0];
            m_PickupUI.AddToClassList(k_InactiveUSSClass);
            m_WorldspaceUI.rootVisualElement.Q<VisualElement>("Pickup").Add(m_PickupUI);
        }

        public void ShowPickup(Transform t)
        {
            m_NextPickup = t;
            m_PickupUI.style.display = DisplayStyle.Flex;
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
                    m_PickupUI.RemoveFromClassList(k_InactiveUSSClass);
                    m_PickupUI.AddToClassList(k_ActiveUSSClass);
                    UpdatePickup();
                }
                return;
            }

            m_PickupUI.RemoveFromClassList(k_ActiveUSSClass);
            m_PickupUI.AddToClassList(k_InactiveUSSClass);
            if (m_PickupUI.resolvedStyle.opacity == 0)
            {
                m_CurrentPickup = m_NextPickup;
            }
        }

        void UpdatePickup()
        {
            UIUtils.TransformUIDocumentWorldspace(this.m_WorldspaceUI, m_CurrentPickup, m_VerticalOffset);
        }
    }
}
