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

        InputDevice _mDevice;
        Dictionary<string, InputControl> _mControlMap = new();
        MobileGamepadState _mRuntimeState;

        async void OnEnable()
        {
            var isMobile = await InputSystemManager.IsMobile;
            if (!isMobile)
                return;

            if (!TryGetDevice())
                return;

            SetupControlBindings();
            _mRuntimeState = MobileGamepadState.GetOrCreate;
            _mRuntimeState.ButtonStateChanged += SendControlEvent;
            _mRuntimeState.JoystickStateChanged += SendControlUpdate;
        }

        void OnDisable()
        {
            if (_mDevice != null)
            {
                if (_mDevice.usages.Count == 1 && _mDevice.usages[0] == "OnScreen")
                    InputSystem.RemoveDevice(_mDevice);

                if (_mRuntimeState != null)
                {
                    _mRuntimeState.ButtonStateChanged -= SendControlEvent;
                    _mRuntimeState.JoystickStateChanged -= SendControlUpdate;
                }
            }
        }

        bool TryGetDevice()
        {
            try
            {
                _mDevice = InputSystem.GetDevice<Gamepad>();
                if (_mDevice == null)
                {
                    _mDevice = InputSystem.AddDevice<Gamepad>();
                    InputSystem.AddDeviceUsage(_mDevice, "OnScreen");
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
            _mControlMap = new Dictionary<string, InputControl>
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
                if (deviceControl.device == _mDevice)
                    return deviceControl;
            }
            Debug.LogError($"Cannot find matching control '{inputControl}' on device of type '{_mDevice}'");
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
            if (!_mControlMap.TryGetValue(propertyName, out var inputControl))
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
            if (!_mControlMap.TryGetValue(propertyName, out var inputControl))
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
