#if ENABLE_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;

namespace TheraBytes.BetterUi
{
    public class InputDetectorForNewInputSystem : BaseInputDetector<InputActionReference, InputSystemUIInputModule>, IInputDetector
    {
        public override float InitialRepeatDelay { get { return inputModule.moveRepeatDelay; } }
        public override float ConsecutiveRepeatDelay { get { return inputModule.moveRepeatRate; } }

        protected override InputActionReference Submit => InputModule.submit;
        protected override InputActionReference Cancel => InputModule.cancel;

        public override bool LastInputWasGamepad => lastInputWasGamepad;

        static bool lastInputWasGamepad;
        Vector2 previousPointerPosition = Vector2.zero;

        public InputDetectorForNewInputSystem(InputSystemUIInputModule inputModule)
            : base(inputModule)
        {
        }

        protected override InputActionType HandleKeyboardAndGamepad(out Vector2 direction)
        {
            const float deadzone = 0.125f;

            direction = new Vector2();

            if (IsActionPressed(inputModule.submit))
                return InputActionType.Submit;

            if (IsActionPressed(inputModule.cancel))
                return InputActionType.Cancel;

            foreach (var control in inputModule.move.action.controls)
            {
                if (control is InputControl<Vector2> v2Ctrl)
                {
                    direction = v2Ctrl.ReadValue();
                    if (Mathf.Abs(direction.x) > deadzone || Mathf.Abs(direction.y) > deadzone)
                    {
                        lastInputWasGamepad = control.device is Gamepad;
                        return InputActionType.NavigateInAnyDirection;
                    }
                }
            }
            return InputActionType.None;
        }

        protected override InputActionType HandleMouseAndTouch()
        {
            if (IsActionPressed(inputModule.leftClick)) 
            {
                lastInputWasGamepad = false;
                return InputActionType.Submit;
            }

            var prev = previousPointerPosition;
            var pos = inputModule.point.action.ReadValue<Vector2>();
            previousPointerPosition = pos;

            if (pos != prev && prev != Vector2.zero)
            {
                lastInputWasGamepad = false;
                return InputActionType.PointerPositionChanged;
            }

            return InputActionType.None;
        }


        internal static bool IsActionPressed(InputActionReference actionReference)
        {
            const float epsilon = 0.1f;

            foreach (var control in actionReference.action.controls)
            {
                if (control is InputControl<float> fCtrl)
                {
                    if (fCtrl.ReadValue() >= epsilon)
                    {
                        lastInputWasGamepad = control.device is Gamepad;
                        return true;
                    }
                }
                else if (control is InputControl<Vector2> v2Ctrl)
                {
                    if (v2Ctrl.ReadValue().sqrMagnitude >= epsilon * epsilon)
                    {
                        lastInputWasGamepad = control.device is Gamepad;
                        return true;
                    }
                }
                else
                {
                    throw new NotSupportedException($"Input Control of type {control.GetType().Name} is not supported.");
                }
            }

            return false;
        }
    }
}

#endif