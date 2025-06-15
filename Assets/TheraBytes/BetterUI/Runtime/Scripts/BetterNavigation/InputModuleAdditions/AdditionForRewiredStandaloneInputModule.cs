
#if REWIRED

using Rewired.Integration.UnityUI;
using UnityEngine;

namespace TheraBytes.BetterUi
{
    [HelpURL("https://documentation.therabytes.de/better-ui/InputModuleAddition.html")]
    public class AdditionForRewiredStandaloneInputModule : InputModuleAdditionWithDualAxisNavigation<int, RewiredStandaloneInputModule>
    {
        protected override int HorizontalAxis => InputModule.HorizontalActionId;
        protected override int VerticalAxis => InputModule.VerticalActionId;

        protected override  bool IsDown(int actionId)
        {
            if (actionId < 0)
                return false;

            return InputDetectorForRewired.CheckButton(InputModule, actionId);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            bool allzero = buttonX == 0
                && buttonY == 0
                && buttonL == 0
                && buttonR == 0
                && switchLeft == 0
                && switchRight == 0
                && alternativeSwitchLeft == 0
                && alternativeSwitchRight == 0
                && contextLeft == 0
                && contextRight == 0
                && contextUp == 0
                && contextDown == 0;

            if (allzero)
            {
                buttonX = -1;
                buttonY = -1;
                buttonL = -1;
                buttonR = -1;
                switchLeft = -1;
                switchRight = -1;
                alternativeSwitchLeft = -1;
                alternativeSwitchRight = -1;
                contextLeft = -1;
                contextRight = -1;
                contextUp = -1;
                contextDown = -1;
            }
                
        }

#endif

    }
}

#endif
