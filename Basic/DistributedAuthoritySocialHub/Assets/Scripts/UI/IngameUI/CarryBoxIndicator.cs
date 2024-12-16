using System;
using System.Collections;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// Panel that shows interaction options when character carries something.
    /// </summary>
    class CarryBoxIndicator : MonoBehaviour
    {
        [SerializeField]
        VisualTreeAsset m_CarryBoxIndicatorAsset;

        [SerializeField]
        Camera m_Camera;

        [SerializeField]
        float m_VerticalOffset = 1.5f;

        [SerializeField]
        UIDocument m_ScreenspaceUI;

        [SerializeField]
        float m_PanelMaxSize = 1.5f;

        [SerializeField]
        float m_PanelMinSize = 0.7f;

        VisualElement m_CarryUI;

        Transform m_CarryTransform;

        bool m_IsShown;

        void OnEnable()
        {
            // Pick first child to avoid adding the root element
            m_CarryUI = m_CarryBoxIndicatorAsset.CloneTree().GetFirstChild();
            m_ScreenspaceUI.rootVisualElement.Q<VisualElement>("player-carry-container").Add(m_CarryUI);
            m_CarryUI.Q<Label>("call-to-action").text = "tap - drop \nhold - throw";
            m_CarryUI.AddToClassList("carrybox");
            m_CarryUI.AddToClassList(UIUtils.s_InactiveUSSClass);

            GameplayEventHandler.OnPickupStateChanged += OnPickupStateChanged;
        }

        void OnPickupStateChanged(PickupState state, Transform pickupTransform)
        {
            if (state == PickupState.Carry)
            {
                ShowCarry(pickupTransform);
                return;
            }
            HideCarry();
        }

        void ShowCarry(Transform t)
        {
            if (m_IsShown)
                return;

            m_CarryTransform = t;
            m_CarryUI.RemoveFromClassList(UIUtils.s_InactiveUSSClass);
            m_CarryUI.AddToClassList(UIUtils.s_ActiveUSSClass);
            StopCoroutine(HideAfterDelay(5f));
            StartCoroutine(HideAfterDelay(5f));
            m_IsShown = true;
        }

        void HideCarry()
        {
            if(m_IsShown == false)
                return;

            StopCoroutine(HideAfterDelay(5f));
            m_CarryUI.RemoveFromClassList(UIUtils.s_ActiveUSSClass);
            m_CarryUI.AddToClassList(UIUtils.s_InactiveUSSClass);
            m_IsShown = false;
        }

        IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideCarry();
        }

        void Update()
        {
            if (m_CarryTransform == null)
                return;

            m_CarryUI.TranslateVEWorldToScreenspace(m_Camera, m_CarryTransform, m_VerticalOffset);
            var distance = Vector3.Distance(m_Camera.transform.position, m_CarryTransform.position);
            var mappedScale = Mathf.Lerp(m_PanelMaxSize, m_PanelMinSize, Mathf.InverseLerp(5, 20, distance));
            m_CarryUI.style.scale = new StyleScale(new Vector2(mappedScale, mappedScale));
        }

        void OnDisable()
        {
            GameplayEventHandler.OnPickupStateChanged -= OnPickupStateChanged;
        }
    }
}
