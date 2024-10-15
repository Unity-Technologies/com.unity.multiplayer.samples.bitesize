using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// Panel that shows interaction options when character carries something.
    /// </summary>
    public class CarryBoxIndicator : MonoBehaviour
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

        void OnEnable()
        {
            // Pick first child to avoid adding the root element
            m_CarryUI = m_CarryBoxIndicatorAsset.CloneTree().GetFirstChild();
            m_CarryUI.AddToClassList(UIUtils.activeUSSClass);
            m_ScreenspaceUI.rootVisualElement.Q<VisualElement>("player-carry-container").Add(m_CarryUI);
            m_CarryUI.Q<Label>("call-to-action").text = "tab - drop \nhold - throw";
            m_CarryUI.AddToClassList("carrybox");
        }

        public void ShowCarry(Transform t)
        {
            m_CarryTransform = t;
            m_CarryUI.RemoveFromClassList(UIUtils.inactiveUSSClass);
            m_CarryUI.AddToClassList(UIUtils.activeUSSClass);
            StopCoroutine(HideAfterDelay(5f));
            StartCoroutine(HideAfterDelay(5f));
        }

        IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideCarry();
        }

        public void HideCarry()
        {
            StopCoroutine(HideAfterDelay(5f));
            m_CarryUI.RemoveFromClassList(UIUtils.activeUSSClass);
            m_CarryUI.AddToClassList(UIUtils.inactiveUSSClass);
        }

        void Update()
        {
            if (m_CarryTransform == null)
                return;

            UIUtils.TranslateVEWorldToScreenspace(m_Camera, m_CarryTransform, m_CarryUI, m_VerticalOffset);
            var distance = Vector3.Distance(m_Camera.transform.position, m_CarryTransform.position);
            var mappedScale = Mathf.Lerp(m_PanelMaxSize, m_PanelMinSize, Mathf.InverseLerp(5, 20, distance));
            m_CarryUI.style.scale = new StyleScale(new Vector2(mappedScale, mappedScale));
        }
    }
}
