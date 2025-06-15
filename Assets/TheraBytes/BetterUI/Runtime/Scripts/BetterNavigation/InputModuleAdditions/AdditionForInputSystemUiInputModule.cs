
#if ENABLE_INPUT_SYSTEM

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace TheraBytes.BetterUi
{

    [HelpURL("https://documentation.therabytes.de/better-ui/InputModuleAddition.html")]
    public class AdditionForInputSystemUiInputModule : InputModuleAddition<InputActionReference, InputSystemUIInputModule>
    {
        protected override InputActionReference NavigateInAnyDirection => InputModule.move;
        protected override InputActionReference NavigateUp => InputModule.move;
        protected override InputActionReference NavigateDown => InputModule.move;
        protected override InputActionReference NavigateLeft => InputModule.move;
        protected override InputActionReference NavigateRight => InputModule.move;

        protected override bool IsDown(InputActionReference button)
        {
            if(button == null)
                return false;

            return InputDetectorForNewInputSystem.IsActionPressed(button);
        }
    }
}

#endif