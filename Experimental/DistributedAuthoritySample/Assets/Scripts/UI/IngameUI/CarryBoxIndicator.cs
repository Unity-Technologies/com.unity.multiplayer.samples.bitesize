using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
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

        VisualElement m_CarryUI;

        Transform m_CarryTransform;

        [SerializeField]
        float m_PanelMaxSize = 1.5f;

        [SerializeField]
        float m_PanelMinSize = 0.7f;

        const string k_ActiveUSSClass = "show";
        const string k_InactiveUSSClass = "hide";

        void OnEnable()
        {
            // pick first child to avoid adding the root element
            m_CarryUI = m_CarryBoxIndicatorAsset.CloneTree().Children().ToArray()[0];
            m_CarryUI.AddToClassList(k_InactiveUSSClass);
            m_ScreenspaceUI.rootVisualElement.Q<VisualElement>("player-carry-container").Add(m_CarryUI);
            m_CarryUI.Q<Label>("call-to-action").text = "tab - drop \nhold - throw";
            m_CarryUI.AddToClassList("carrybox");
        }

        public void ShowCarry(Transform t)
        {
            m_CarryTransform = t;
            m_CarryUI.RemoveFromClassList(k_InactiveUSSClass);
            m_CarryUI.AddToClassList(k_ActiveUSSClass);
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
            m_CarryUI.RemoveFromClassList(k_ActiveUSSClass);
            m_CarryUI.AddToClassList(k_InactiveUSSClass);
        }

        void Update()
        {
            if(m_CarryTransform == null)
                return;

            UIUtils.TranslateVEWorldToScreenspace(m_Camera, m_CarryTransform, m_CarryUI, m_VerticalOffset);
            var distance = Vector3.Distance(m_Camera.transform.position, m_CarryTransform.position);
            var mappedScale  = Mathf.Lerp (m_PanelMaxSize, m_PanelMinSize, Mathf.InverseLerp (5, 20, distance));
            m_CarryUI.style.scale = new StyleScale(new Vector2(mappedScale, mappedScale));
        }
    }
}
