using UnityEngine;
using UnityEngine.UIElements;

namespace UIToolkitSamples
{
    /// <summary>
    /// Main UI controller. Holds child views for sub views.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainUIController : UIView
    {
        UIDocument m_UIDocument;

        /// <summary>
        /// Home view: Main menu items
        /// </summary>
        [SerializeField]
        HomeScreenView m_HomeView;

        UIView m_CurrentView;

        public void OnEnable()
        {
            m_UIDocument = GetComponent<UIDocument>();
            Initialize(m_UIDocument.rootVisualElement);

            m_HomeView.Initialize(m_Root.Q<VisualElement>("HomeScreen"));
            RegisterEvents();
            DisplayChildView(m_HomeView);
        }

        public void OnDisable()
        {
            UnregisterEvents();
        }

        public void OnShowHomeScreen()
        {
            DisplayChildView(m_HomeView);
        }

        public override void RegisterEvents() { }

        public override void UnregisterEvents() { }
    }
}
