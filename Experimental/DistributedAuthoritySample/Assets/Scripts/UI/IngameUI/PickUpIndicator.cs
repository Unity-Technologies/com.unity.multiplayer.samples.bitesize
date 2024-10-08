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

        [SerializeField]
        Transform m_CurrentPickup;

        [SerializeField]
        float m_VerticalOffset = 1.5f;

        VisualElement m_Root;

        VisualElement m_Pickup;
        UIDocument m_UIDocument;

        void OnEnable()
        {
            m_UIDocument = GetComponent<UIDocument>();
            m_Root = m_UIDocument.rootVisualElement;
            m_Pickup = m_PickupAsset.CloneTree().Children().First();
            m_Pickup.styleSheets.Add(m_PickupAsset.stylesheets.First());
            m_UIDocument.rootVisualElement.Q<VisualElement>("Pickup").Add(m_Pickup);
        }

        public void ShowPickup(Transform t)
        {
            m_CurrentPickup = t;
            m_Pickup.style.display = DisplayStyle.Flex;
            m_Pickup.RemoveFromClassList("hide");
            m_Pickup.AddToClassList("show");
        }


        public void ClearPickup()
        {
            m_Pickup.RemoveFromClassList("show");
            m_Pickup.AddToClassList("hide");
        }

        private void Update()
        {
            if (m_CurrentPickup != null)
            {
                UpdatePickup();
            }
        }

        void UpdatePickup()
        {
            m_Pickup.transform.rotation = WorldspaceUtils.LookAtCameraY(m_Camera, m_CurrentPickup.transform);
            WorldspaceUtils.TranslateVEWorldspaceInPixelSpace(m_UIDocument, m_Pickup, m_CurrentPickup, m_VerticalOffset);
        }
    }
}
