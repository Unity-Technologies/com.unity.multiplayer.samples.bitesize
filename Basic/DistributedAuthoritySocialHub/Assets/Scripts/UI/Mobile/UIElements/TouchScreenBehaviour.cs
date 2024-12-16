using System;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using Unity.Multiplayer.Samples.SocialHub.Input;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// This class is managing the data binding between the TouchScreen
    /// </summary>
    class TouchScreenBehaviour : MonoBehaviour
    {
        static class UIElementNames
        {
            internal const string JoystickMove = "JoystickMove";
            internal const string JoystickLook = "JoystickLook";
            internal const string ButtonMenu = "ButtonMenu";
            internal const string ButtonJump = "ButtonJump";
            internal const string ButtonSprint = "ButtonSprint";
            internal const string ButtonInteract = "ButtonInteract";
            internal const string PlayerContainer = "PlayerContainer";
        }

        [SerializeField]
        UIDocument m_Document;

        [SerializeField]
        VisualTreeAsset m_TouchscreenUI;

        VirtualJoystick m_JoystickLeft;
        VirtualJoystick m_JoystickRight;
        MobileGamepadState m_RuntimeState;
        PressedButton m_ButtonInteract;

        async void OnEnable()
        {
            var isMobile = await InputSystemManager.IsMobile;
            if (!isMobile)
            {
                return;
            }

            var root = m_Document.rootVisualElement.Q("touch-ui-container");
            m_TouchscreenUI.CloneTree(root);
            m_RuntimeState = MobileGamepadState.GetOrCreate;

            // Bindings
            root.dataSource = m_RuntimeState;
            var joystickMove = root.Q<VisualElement>(UIElementNames.JoystickMove);
            m_JoystickLeft = new VirtualJoystick(joystickMove, OnJoystickLeftMoved,
                m_RuntimeState.LeftJoystickTopName, m_RuntimeState.LeftJoystickLeftName);
            var joystickLook = root.Q<VisualElement>(UIElementNames.JoystickLook);
            m_JoystickRight = new VirtualJoystick(joystickLook, OnJoystickRightMoved,
                m_RuntimeState.RightJoystickTopName, m_RuntimeState.RightJoystickLeftName);

            var buttonMenu = root.Q<PressedButton>(UIElementNames.ButtonMenu);
            buttonMenu.SetBinding("value", new DataBinding
            {
                dataSourcePath = new PropertyPath(nameof(MobileGamepadState.ButtonMenu)),
                bindingMode = BindingMode.ToSource,
            });

            var buttonJump = root.Q<PressedButton>(UIElementNames.ButtonJump);
            buttonJump.SetBinding("value", new DataBinding
            {
                dataSourcePath = new PropertyPath(nameof(MobileGamepadState.ButtonJump)),
                bindingMode = BindingMode.ToSource,
            });

            m_ButtonInteract = root.Q<PressedButton>(UIElementNames.ButtonInteract);
            m_ButtonInteract.SetBinding("value", new DataBinding
            {
                dataSourcePath = new PropertyPath(nameof(MobileGamepadState.ButtonInteract)),
                bindingMode = BindingMode.ToSource,
            });

            var buttonSprint = root.Q<Toggle>(UIElementNames.ButtonSprint);
            buttonSprint.SetBinding("value", new DataBinding
            {
                dataSourcePath = new PropertyPath(nameof(MobileGamepadState.ButtonSprint)),
                bindingMode = BindingMode.ToSource,
            });

            GameplayEventHandler.OnPickupStateChanged += OnPickupStateChanged;
        }

        void OnPickupStateChanged(PickupState state, Transform _)
        {
            m_ButtonInteract.enabledSelf = state != PickupState.Inactive;

            if(state == PickupState.Carry)
            {
                m_ButtonInteract.AddToClassList("state-carry");
                return;
            }

            m_ButtonInteract.RemoveFromClassList("state-carry");
        }

        void OnJoystickLeftMoved(Vector2 position) => m_RuntimeState.LeftJoystick = position;
        void OnJoystickRightMoved(Vector2 position) => m_RuntimeState.RightJoystick = position;

        void OnDisable()
        {
            m_JoystickLeft?.Dispose();
            m_JoystickLeft = null;
            m_JoystickRight?.Dispose();
            m_JoystickRight = null;

            GameplayEventHandler.OnPickupStateChanged -= OnPickupStateChanged;
        }

        /// <summary>
        /// This class handles a pointer capture and movement for a Joystick
        /// in the <see cref="TouchScreenBehaviour"/> UI panel.
        /// </summary>
        /// <remarks>
        /// The <see cref="IDisposable"/> interface is used to unregister the UI events callbacks
        /// and should be called in the UI <see cref="TouchScreenBehaviour"/> method.
        /// </remarks>
        /// <remarks>
        /// The Bindings on the visual elements are only reading from <see cref="MobileGamepadState"/>
        /// because in this particular case, the Pointer events handlers are writing the position to the data and the visual gets updated from it.
        /// </remarks>
        class VirtualJoystick : IDisposable
        {
            readonly VisualElement m_Root;
            readonly Action<Vector2> m_OnJoystickMoved;

            public VirtualJoystick(VisualElement root, Action<Vector2> onJoystickMoved, string topProperty, string leftProperty)
            {
                m_Root = root;
                m_OnJoystickMoved = onJoystickMoved;

                root.RegisterCallback<PointerDownEvent>(HandlePress);
                root.RegisterCallback<PointerMoveEvent>(HandleDrag);
                root.RegisterCallback<PointerUpEvent>(HandleRelease);

                var stick = root.Q<VisualElement>("Stick");
                stick.SetBinding("style.top", new DataBinding
                {
                    dataSourcePath = new PropertyPath(topProperty),
                    bindingMode = BindingMode.ToTarget,
                });
                stick.SetBinding("style.left", new DataBinding
                {
                    dataSourcePath = new PropertyPath(leftProperty),
                    bindingMode = BindingMode.ToTarget,
                });
            }

            public void Dispose()
            {
                m_Root.UnregisterCallback<PointerDownEvent>(HandlePress);
                m_Root.UnregisterCallback<PointerMoveEvent>(HandleDrag);
                m_Root.UnregisterCallback<PointerUpEvent>(HandleRelease);
            }

            void HandlePress(PointerDownEvent evt) => m_Root.CapturePointer(evt.pointerId);

            void HandleRelease(PointerUpEvent evt)
            {
                if (!m_Root.HasPointerCapture(evt.pointerId))
                    return;

                m_Root.ReleasePointer(evt.pointerId);
                m_OnJoystickMoved(Vector2.zero);
            }

            void HandleDrag(PointerMoveEvent evt)
            {
                if (!m_Root.HasPointerCapture(evt.pointerId))
                    return;

                var center = m_Root.contentRect.center;
                var width = m_Root.contentRect.width;
                var centerToPosition = ((Vector2)evt.localPosition - center) / width;

                if (centerToPosition.sqrMagnitude > 1)
                {
                    centerToPosition = centerToPosition.normalized;
                }

                m_OnJoystickMoved(centerToPosition);
            }
        }
    }
}
