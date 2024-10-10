using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// Template class for all view controllers.
    /// </summary>
    abstract class UIView : MonoBehaviour
    {
        /// <summary>
        /// Root of the visual element that this view controls
        /// </summary>
        protected VisualElement m_Root;

        // Child reference to child view if any.
        protected UIView m_ChildView;

        /// <summary>
        /// Determines if the view is modal: Allowing parent windows to remain visable.
        /// </summary>
        [SerializeField]
        public bool IsModal;

        /// <summary>
        /// Displays a targeted child view.
        /// </summary>
        /// <param name="targetUI">Child ui view to show.</param>
        protected void DisplayChildView(UIView targetUI)
        {
            if (!targetUI.IsModal)
            {
                m_ChildView?.Hide();
            }

            m_ChildView?.UnregisterEvents();
            m_ChildView = targetUI;
            m_ChildView.Show();
            m_ChildView.RegisterEvents();
        }

        public virtual void Initialize(VisualElement root)
        {
            m_Root = root;
        }

        void Show()
        {
            m_Root.style.display = DisplayStyle.Flex;
            HandleOnShown();
        }

        void Hide()
        {
            m_Root.style.display = DisplayStyle.None;
            HandleOnHidden();
        }

        protected abstract void RegisterEvents();

        protected abstract void UnregisterEvents();

        protected virtual void HandleOnShown() { }
        protected virtual void HandleOnHidden() { }
    }
}
