using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    public class WorldspaceTest : MonoBehaviour
    {
        [SerializeField]
        Transform[] m_Transforms;

        [SerializeField]
        UIDocument m_UIDocument;
        VisualElement[] m_VisualElements;

        void Start()
        {

            m_UIDocument = GetComponent<UIDocument>();
            m_VisualElements = new VisualElement[m_Transforms.Length];
            for (int i = 0; i < m_Transforms.Length; i++)
            {
                m_VisualElements[i] = DummyElement();
                m_UIDocument.rootVisualElement.Add(m_VisualElements[i]);
            }
        }

        VisualElement DummyElement()
        {
            var label = new Label("Test");
            label.style.backgroundColor = Color.blue;
            label.style.position = Position.Absolute;
            return label;
        }

        void Update()
        {
            for (int i = 0; i < m_VisualElements.Length; i++)
            {
                var elm = m_VisualElements[i];
                elm.transform.rotation = UIUtils.LookAtCameraY(Camera.main, m_Transforms[i]);
                UIUtils.TranslateVEWorldspaceInPixelSpace(m_UIDocument, elm, m_Transforms[i],1f);
            }
        }
    }
}
