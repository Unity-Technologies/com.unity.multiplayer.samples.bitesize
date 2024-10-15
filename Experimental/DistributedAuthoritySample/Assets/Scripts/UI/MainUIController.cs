using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    namespace UIToolkitSamples
    {
        /// <summary>
        /// Main UI controller. Holds child views for sub views.
        /// </summary>
        [RequireComponent(typeof(UIDocument))]
        class MainUIController : UIView
        {
            UIDocument m_UIDocument;

            /// <summary>
            /// Home view: Main menu items
            /// </summary>
            [SerializeField]
            HomeScreenView m_HomeView;

            UIView m_CurrentView;

            void OnEnable()
            {
                m_UIDocument = GetComponent<UIDocument>();
                Initialize(m_UIDocument.rootVisualElement);

                m_HomeView.Initialize(m_Root.Q<VisualElement>("HomeScreen"));
                RegisterEvents();
                DisplayChildView(m_HomeView);
            }

            void OnDisable()
            {
                UnregisterEvents();
            }

            protected override void RegisterEvents() { }

            protected override void UnregisterEvents() { }
        }
    }
}
