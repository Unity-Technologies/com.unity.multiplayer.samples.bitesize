using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

namespace Unity.Multiplayer.Samples.SocialHub.Input
{
    /// <summary>
    /// This class is binding the <see cref="MobileGamepadState"/> values
    /// to runtime <see cref="InputControl"/> used by the InputSystem.
    /// </summary>
    /// <seealso cref="InputSystemManager"/>
    /// <seealso cref="TouchScreenBehaviour"/>
    class MobileGamepadBehaviour : MonoBehaviour
    {
        [InputControl(layout = "Stick"), SerializeField]
        string m_MoveAction;
        [InputControl(layout = "Stick"), SerializeField]
        string m_LookAction;
        [InputControl(layout = "Button"), SerializeField]
        string m_JumpAction;
        [InputControl(layout = "Button"), SerializeField]
        string m_InteractAction;
        [InputControl(layout = "Button"), SerializeField]
        string m_SprintAction;
        //[InputControl(layout = "Button"), SerializeField]
        //string m_ToggleNetworkStatsAction;
        [InputControl(layout = "Button"), SerializeField]
        string m_MenuAction;

        InputDevice m_Device;
        Dictionary<string, InputControl> m_ControlMap = new();
        MobileGamepadState m_RuntimeState;

        async void OnEnable()
        {
            var isMobile = await InputSystemManager.IsMobile;
            if (!isMobile)
                return;

            if (!TryGetDevice())
                return;

            SetupControlBindings();
            m_RuntimeState = MobileGamepadState.GetOrCreate;
            m_RuntimeState.ButtonStateChanged += SendControlEvent;
            m_RuntimeState.JoystickStateChanged += SendControlUpdate;
        }

        void OnDisable()
        {
            if (m_Device != null)
            {
                if (m_Device.usages.Count == 1 && m_Device.usages[0] == "OnScreen")
                    InputSystem.RemoveDevice(m_Device);

                if (m_RuntimeState != null)
                {
                    m_RuntimeState.ButtonStateChanged -= SendControlEvent;
                    m_RuntimeState.JoystickStateChanged -= SendControlUpdate;
                }
            }
        }

        bool TryGetDevice()
        {
            try
            {
                m_Device = InputSystem.GetDevice<Gamepad>();
                if (m_Device == null)
                {
                    m_Device = InputSystem.AddDevice<Gamepad>();
                    InputSystem.AddDeviceUsage(m_Device, "OnScreen");
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Could not create device with layout 'Gamepad' used in '{GetType().Name}' component");
                Debug.LogException(exception);
                return false;
            }

            return true;
        }

        void SetupControlBindings()
        {
            m_ControlMap = new Dictionary<string, InputControl>
            {
                { nameof(MobileGamepadState.LeftJoystick), MapRuntimeControl(m_MoveAction) },
                { nameof(MobileGamepadState.RightJoystick), MapRuntimeControl(m_LookAction) },
                { nameof(MobileGamepadState.ButtonJump), MapRuntimeControl(m_JumpAction) },
                { nameof(MobileGamepadState.ButtonInteract), MapRuntimeControl(m_InteractAction) },
                { nameof(MobileGamepadState.ButtonSprint), MapRuntimeControl(m_SprintAction) },
                // will re-visit to evaluate if this button is necessary
                //({ nameof(MobileGamepadState.ButtonToggleNetworkStats), MapRuntimeControl(m_ToggleNetworkStatsAction) },
                { nameof(MobileGamepadState.ButtonMenu), MapRuntimeControl(m_MenuAction) },
            };
        }

        InputControl MapRuntimeControl(string inputControl)
        {
            var deviceControl = InputSystem.FindControl(inputControl);
            if (deviceControl != null)
            {
                if (deviceControl.device == m_Device)
                    return deviceControl;
            }
            Debug.LogError($"Cannot find matching control '{inputControl}' on device of type '{m_Device}'");
            return null;
        }

        /// <summary>
        /// This method updates the current device input states in the current frame.
        /// </summary>
        /// <remarks>
        /// Use this method to update input values that are used with <see cref="InputAction.ReadValue{TValue}"/>.
        /// It will not trigger a <see cref="InputAction.WasPerformedThisFrame"/>.
        /// </remarks>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <typeparam name="TValue"></typeparam>
        void SendControlUpdate<TValue>(string propertyName, TValue value)
            where TValue : struct
        {
            if (!m_ControlMap.TryGetValue(propertyName, out var inputControl))
            {
                Debug.LogError($"No InputControl for the property {propertyName} has been registered");
                return;
            }
            if (inputControl is not InputControl<TValue> control)
            {
                Debug.LogError(
                    $"The control path {inputControl.path} yields a control of type {inputControl.GetType().Name} which is not an InputControl with value type {typeof(TValue).Name}");
                return;
            }
            using (StateEvent.From(control.device, out var eventPtr))
            {
                control.WriteValueIntoEvent(value, eventPtr);
                InputState.Change(control.device, eventPtr);
            }
        }

        /// <summary>
        /// This method queues an event in the InputSystem for the given input to change its state.
        /// </summary>
        /// <remarks>
        /// Use this method to queue an input change event that will be processed at the end of the frame.
        /// It will trigger a <see cref="InputAction.WasPerformedThisFrame"/> event on the next frame.
        /// </remarks>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <typeparam name="TValue"></typeparam>
        void SendControlEvent<TValue>(string propertyName, TValue value)
            where TValue : struct
        {
            if (!m_ControlMap.TryGetValue(propertyName, out var inputControl))
            {
                Debug.LogError($"No InputControl for the property {propertyName} has been registered");
                return;
            }
            if (inputControl is not InputControl<TValue> control)
            {
                Debug.LogError(
                    $"The control path {inputControl.path} yields a control of type {inputControl.GetType().Name} which is not an InputControl with value type {typeof(TValue).Name}");
                return;
            }
            using (StateEvent.From(control.device, out var eventPtr))
            {
                control.WriteValueIntoEvent(value, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
        }
    }
}
