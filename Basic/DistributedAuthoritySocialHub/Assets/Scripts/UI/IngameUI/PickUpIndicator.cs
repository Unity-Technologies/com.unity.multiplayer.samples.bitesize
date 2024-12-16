using System;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// Indicator shown when a pickup is in range.
    /// </summary>
    class PickUpIndicator : MonoBehaviour
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
                    m_PickupUI.RemoveFromClassList(UIUtils.s_InactiveUSSClass);
                    m_PickupUI.AddToClassList(UIUtils.s_ActiveUSSClass);
                    return;
                }

                // fade out the pickup UI
                m_PickupUI.RemoveFromClassList(UIUtils.s_ActiveUSSClass);
                m_PickupUI.AddToClassList(UIUtils.s_InactiveUSSClass);
            }
        }

        void ShowPickup(Transform t)
        {
            m_NextPickup = t;
        }

        void ClearPickup()
        {
            m_NextPickup = null;
        }

        void OnEnable()
        {
            // pick first child to avoid adding the root element
            m_PickupUI = m_PickupAsset.CloneTree().GetFirstChild();
            m_PickupUI.AddToClassList(UIUtils.s_InactiveUSSClass);
            m_WorldspaceUI.rootVisualElement.Q<VisualElement>("Pickup").Add(m_PickupUI);
            GameplayEventHandler.OnPickupStateChanged += OnPickupStateChanged;
        }

        void OnPickupStateChanged(PickupState state, Transform pickupTransform)
        {
            switch (state)
            {
                case PickupState.PickupInRange:
                    ShowPickup(pickupTransform);
                    break;
                case PickupState.Inactive or PickupState.Carry:
                    ClearPickup();
                    break;
            }
        }

        void Update()
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

        void OnDisable()
        {
            GameplayEventHandler.OnPickupStateChanged -= OnPickupStateChanged;
        }
    }
}
