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
        VisualTreeAsset m_CarryAsset;

        [SerializeField]
        Camera m_Camera;

        [SerializeField]
        Transform m_CurrentPickup;

        [SerializeField]
        Transform m_CurrentCarry;

        [SerializeField]
        float m_VerticalOffset = 1.5f;


        VisualElement m_PickupUI;

        VisualElement m_CarryUI;

        [SerializeField]
        UIDocument m_WorldspaceUI;

        [SerializeField]
        UIDocument m_SceenspaceUI;

        void OnEnable()
        {
            m_PickupUI = m_PickupAsset.CloneTree().Children().First();
            m_PickupUI.styleSheets.Add(m_PickupAsset.stylesheets.First());
            m_PickupUI.AddToClassList("hide");
            m_WorldspaceUI.rootVisualElement.Q<VisualElement>("Pickup").Add(m_PickupUI);

            m_CarryUI = m_CarryAsset.CloneTree().Children().First();
            m_CarryUI.styleSheets.Add(m_CarryAsset.stylesheets.First());
            m_SceenspaceUI.rootVisualElement.Q<VisualElement>("Pickup").Add(m_CarryUI);
        }

        public void ShowPickup(Transform t)
        {
            m_CurrentPickup = t;
            m_PickupUI.style.display = DisplayStyle.Flex;
            m_PickupUI.RemoveFromClassList("hide");
            m_PickupUI.AddToClassList("show");
        }

        public void ShowCarry(Transform t)
        {
            m_CurrentCarry = t;
        }


        public void ClearPickup()
        {
            m_PickupUI.RemoveFromClassList("show");
            m_PickupUI.AddToClassList("hide");
        }



        private void Update()
        {
            if (m_CurrentPickup != null)
            {
                UpdatePickup();
            }

            if(m_CurrentCarry != null)
            {
                UpdateCarry();
            }
        }

        void UpdateCarry()
        {
            UIUtils.TranslateVEWorldToScreenspace(m_Camera, m_CurrentCarry, m_CarryUI);
        }

        void UpdatePickup()
        {
            UIUtils.TransformUIDocumentWorldspace(this.m_WorldspaceUI, m_CurrentPickup, m_VerticalOffset);
        }
    }
}
