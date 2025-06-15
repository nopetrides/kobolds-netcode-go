#if ENABLE_LEGACY_INPUT_MANAGER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace TheraBytes.BetterUi
{
    public class InputDetectorForLegacyInputSystem : BaseInputDetector<string, StandaloneInputModule>
    {
        public override float InitialRepeatDelay { get { return inputModule.repeatDelay; } }
        public override float ConsecutiveRepeatDelay { get { return 1f / inputModule.inputActionsPerSecond; } }
        public override bool LastInputWasGamepad
        {
            get
            {
                if(!canDetermineGamepadInput)
                {
                    Debug.LogError($"Trying to read 'LastInputWasGamepad', but this cannot be determined as long as the AdditionForStandaloneInputModule component doesn't configure the Joystick Axis Names.");
                }

                return lastInputWasGamepad;
            }
        }

        protected override string Submit => InputModule.submitButton;
        protected override string Cancel => InputModule.cancelButton;


        bool canDetermineGamepadInput;
        AdditionForStandaloneInputModule legacyInputModuleAddition;

        bool lastInputWasGamepad;
        Vector2 lastMousePosition;

        public InputDetectorForLegacyInputSystem(StandaloneInputModule inputModule)
            : base(inputModule)
        {
            this.lastMousePosition = inputModule.input.mousePosition;

            legacyInputModuleAddition = base.inputModuleAddition as AdditionForStandaloneInputModule;

            try
            {
                Input.GetAxisRaw(legacyInputModuleAddition.JoystickOnlyHorizontalAxis);
                Input.GetAxisRaw(legacyInputModuleAddition.JoystickOnlyVerticalAxis);

                canDetermineGamepadInput = true;
            }
            catch (Exception)
            {
                // Don't log anything here as the "LastInputWasGamepad" feature is purely optional
                // and not needed by the system.

                canDetermineGamepadInput = false;
            }
        }

        public override void UpdateCurrentNavigationData()
        {
            base.UpdateCurrentNavigationData();

            switch (CurrentNavigationInfo.Device)
            {
                case InputDeviceType.PointerDevice:
                    lastInputWasGamepad = false;
                    break;

                case InputDeviceType.DirectionDevice:
                    lastInputWasGamepad = CheckIfInputWasGamepad();
                    break;
            }
        }

        private bool CheckIfInputWasGamepad()
        {
            if (!canDetermineGamepadInput)
                return false;


            for (int i = 1; i <= 4; i++) // Checking multiple joysticks
            {
                if (Mathf.Abs(Input.GetAxisRaw(legacyInputModuleAddition.JoystickOnlyHorizontalAxis)) > 0.1f ||
                    Mathf.Abs(Input.GetAxisRaw(legacyInputModuleAddition.JoystickOnlyVerticalAxis)) > 0.1f)
                {
                    return true;
                }
            }

            // Check joystick buttons
            for (int i = 0; i < 20; i++) // Joystick buttons (20 as an arbitrary max)
            {
                KeyCode joystickButton = (KeyCode)((int)KeyCode.JoystickButton0 + i);
                if (Input.GetKey(joystickButton) || Input.GetKeyUp(joystickButton))
                {
                    return true;
                }
            }

            return false;
        }

        protected override InputActionType HandleKeyboardAndGamepad(out Vector2 direction)
        {
            direction = new Vector2();
            var input = inputModule.input;

            if (input.GetButtonDown(inputModule.submitButton))
                return InputActionType.Submit;

            if (input.GetButtonDown(inputModule.cancelButton))
                return InputActionType.Cancel;

            float x = input.GetAxisRaw(inputModule.horizontalAxis);
            float y = input.GetAxisRaw(inputModule.verticalAxis);
            bool isAxisNavigation = !Mathf.Approximately(x, 0.0f) || !Mathf.Approximately(y, 0.0f);

            if (isAxisNavigation)
            {
                // direction = NavigationHelper.ToFourWayDirection(x, y);
                direction = new Vector2(x, y);
                return InputActionType.NavigateInAnyDirection;
            }

            return InputActionType.None;
        }

        protected override InputActionType HandleMouseAndTouch()
        {
            var input = inputModule.input;
            var mode = GetMouseActionType(input, ref lastMousePosition);
            if (mode != InputActionType.None)
                return mode;


            // touch
            if (input.touchCount > 0)
            {
                InputActionType touchResult = InputActionType.None;

                for (int i = 0; i < input.touchCount; i++)
                {
                    Touch touch = input.GetTouch(i);

#if UNITY_5_3_OR_NEWER
                    if (touch.type == TouchType.Indirect)
                        continue;
#endif
                    switch (touch.phase)
                    {
                        case TouchPhase.Began:
                            touchResult |= InputActionType.Began;
                            break;
                        case TouchPhase.Moved:
                            touchResult |= InputActionType.PointerPositionChanged;
                            break;
                        case TouchPhase.Stationary:
                            touchResult |= InputActionType.Submit;
                            break;
                        case TouchPhase.Ended:
                            touchResult |= InputActionType.Ended;
                            break;
                        case TouchPhase.Canceled:
                            touchResult |= InputActionType.Cancel;
                            break;
                    }
                }

                return touchResult;
            }

            return InputActionType.None;
        }
        
        internal static InputActionType GetMouseActionType(BaseInput input, ref Vector2 lastMousePosition)
        {
            // movement
            Vector2 prev = lastMousePosition;
            lastMousePosition = input.mousePosition;

            if (prev != Vector2.zero)
            {
                var mouseDelta = input.mousePosition - prev;
                if (mouseDelta.sqrMagnitude > 0.0f)
                {
                    return InputActionType.PointerPositionChanged;
                }
            }

            // mouse clicks
            int mouseButton = (int)MouseButton.LeftMouse;
            if (input.GetMouseButton(mouseButton))
                return InputActionType.Submit;

            return InputActionType.None;
        }
    }
}

#endif