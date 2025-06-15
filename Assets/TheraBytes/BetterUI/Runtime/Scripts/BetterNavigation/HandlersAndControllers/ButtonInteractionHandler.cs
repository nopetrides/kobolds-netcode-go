using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TheraBytes.BetterUi
{
    public abstract class ButtonInteractionHandler : UIBehaviour
    {
        static ObjectCollectionTracker<ButtonInteractionHandler> activeHandlers
            = new ObjectCollectionTracker<ButtonInteractionHandler>();
        public static ObjectCollectionTracker<ButtonInteractionHandler> ActiveHandlers { get {  return activeHandlers; } }


        internal static void NotifyAllAboutButtonInteraction(NavigationInfo navigationInfo)
        {
            foreach (var handler in activeHandlers.CleanUpIterator())
            {
                handler.NotifyButtonInteraction(navigationInfo);
            }
        }

        protected override void OnEnable()
        {
            activeHandlers.Add(this);
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            activeHandlers.Remove(this);
            base.OnDisable();
        }

        protected abstract void NotifyButtonInteraction(NavigationInfo navigationInfo);

        internal static bool IsActionMatched(NavigationInfo navigationInfo, InputActionType actionTypeToCheck)
        {
            const InputActionType timings = InputActionType.Began | InputActionType.Repeated | InputActionType.Ended;

            InputActionType inputAction = navigationInfo.Action;
            var inputActionCommand = inputAction & ~(timings);
            var checkCommand = actionTypeToCheck & ~(timings);

            if (inputActionCommand == InputActionType.NavigateInAnyDirection)
            {
                // Special case:
                // Internally we do not evaluate "NavigateLeft" / "NavigateRight" / ... actions beforehand.
                // All we know is that we navigated in any direction.
                // To allow the user to bind an action to a certain direction, we have to handle it here.
                if(!IsNavigatingToDirection(checkCommand, navigationInfo.Direction))
                    return false;
            }
            else if (inputActionCommand != checkCommand)
            {
                return false;
            }


            var actionToCheckTiming = actionTypeToCheck & timings;

            // special case: fire every frame when pressed -> ended means it was released
            if (actionToCheckTiming == InputActionType.None && !inputAction.HasFlag(InputActionType.Ended))
                return true;

            if((actionToCheckTiming & inputAction) != 0)
                return true;

            return false;
        }

        internal protected virtual bool ShouldShowVisualization()
        {
            return this.isActiveAndEnabled;
        }

        static bool IsNavigatingToDirection(InputActionType checkCommand, MoveDirection dir)
        {
            switch (checkCommand)
            {
                case InputActionType.NavigateLeft:  return dir == MoveDirection.Left;
                case InputActionType.NavigateRight: return dir == MoveDirection.Right;
                case InputActionType.NavigateUp:    return dir == MoveDirection.Up;
                case InputActionType.NavigateDown:  return dir == MoveDirection.Down;
                case InputActionType.NavigateInAnyDirection: return true;
                default: return false;
            }
        }
    }
}
