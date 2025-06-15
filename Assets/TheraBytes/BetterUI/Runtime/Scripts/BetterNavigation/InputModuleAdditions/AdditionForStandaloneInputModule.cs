
#if ENABLE_LEGACY_INPUT_MANAGER

using UnityEngine;
using UnityEngine.EventSystems;

namespace TheraBytes.BetterUi
{
    [HelpURL("https://documentation.therabytes.de/better-ui/InputModuleAddition.html")]
    public class AdditionForStandaloneInputModule : InputModuleAdditionWithDualAxisNavigation<string, StandaloneInputModule>
    {
        [SerializeField] string joystickOnlyHorizontal = "Joystick X Axis";
        [SerializeField] string joystickOnlyVertical = "Joystick Y Axis";

        internal string JoystickOnlyHorizontalAxis => joystickOnlyHorizontal;
        internal string JoystickOnlyVerticalAxis => joystickOnlyVertical;

        protected override string HorizontalAxis => InputModule.horizontalAxis;
        protected override string VerticalAxis => InputModule.verticalAxis;

        protected override bool IsDown(string buttonName)
        {
            if (string.IsNullOrEmpty(buttonName))
                return false;

            return InputModule.input.GetButtonDown(buttonName);
        }
    }
}

#endif