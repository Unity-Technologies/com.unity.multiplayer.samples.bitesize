using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// This custom control replicate a Button Pressed/Released behaviour as it would work on a Gamepad Controller.
    /// </summary>
    [UxmlElement]
    public partial class PressedButton : Toggle
    {
        bool m_HasPointer;

        public PressedButton()
            : this(null)
        {
        }

        public PressedButton(string label)
            : base(label)
        {
            m_HasPointer = false;
            RegisterCallback<PointerCaptureEvent>(OnPointerCapture);
            RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            m_HasPointer = false;
            ToggleValue();
        }

        void OnPointerCapture(PointerCaptureEvent evt)
        {
            m_HasPointer = true;
            ToggleValue();
        }

        protected override void ToggleValue()
        {
            value = m_HasPointer;
        }
    }
}
