
using System;
using System.Numerics;
using UnityEngine.EventSystems;

namespace TheraBytes.BetterUi
{
    public struct NavigationInfo
    {
        public InputDeviceType Device { get; }
        public InputActionType Action { get; internal set; }
        public MoveDirection Direction { get; }

        public NavigationInfo(InputDeviceType deviceType, InputActionType actionType,
            MoveDirection direction = MoveDirection.None)
        {
            Device = deviceType;
            Action = actionType;
            Direction = direction;
        }

        public override string ToString()
        {
            return $"{Device}: {Action} ({Direction})";
        }
    }

    public static class InputActionTypeExtensions
    {
        public static InputActionType GetCombinationWithPreviousFrame(this InputActionType action, InputDeviceType device, NavigationInfo previousFrameInfo, bool canHaveAdditionalActions)
        {
            if (previousFrameInfo.Device != device)
            {
                if (action != InputActionType.None)
                {
                    action |= InputActionType.Began;
                }

                return action;
            }

            action = TrackFrameChangesFor(InputActionType.Submit, action, previousFrameInfo.Action);
            action = TrackFrameChangesFor(InputActionType.Cancel, action, previousFrameInfo.Action);
            action = TrackFrameChangesFor(InputActionType.NavigateInAnyDirection, action, previousFrameInfo.Action);
            action = TrackFrameChangesFor(InputActionType.PointerPositionChanged, action, previousFrameInfo.Action);

            if (canHaveAdditionalActions)
            {
                action = TrackFrameChangesFor(InputActionType.ButtonX, action, previousFrameInfo.Action);
                action = TrackFrameChangesFor(InputActionType.ButtonY, action, previousFrameInfo.Action);
                action = TrackFrameChangesFor(InputActionType.SwitchLeft, action, previousFrameInfo.Action);
                action = TrackFrameChangesFor(InputActionType.SwitchRight, action, previousFrameInfo.Action);
                action = TrackFrameChangesFor(InputActionType.AltSwitchLeft, action, previousFrameInfo.Action);
                action = TrackFrameChangesFor(InputActionType.AltSwitchRight, action, previousFrameInfo.Action);
                action = TrackFrameChangesFor(InputActionType.ContextUp, action, previousFrameInfo.Action);
                action = TrackFrameChangesFor(InputActionType.ContextDown, action, previousFrameInfo.Action);
                action = TrackFrameChangesFor(InputActionType.ContextLeft, action, previousFrameInfo.Action);
                action = TrackFrameChangesFor(InputActionType.ContextRight, action, previousFrameInfo.Action);
            }

            return action;
        }

        static InputActionType TrackFrameChangesFor(InputActionType actionToLookFor, InputActionType state, InputActionType prevFrame)
        {
            // early out if already got a timing
            if ((state & (InputActionType.Began | InputActionType.Repeated | InputActionType.Ended)) != 0)
                return state;

            bool prevHas = prevFrame.HasFlag(actionToLookFor);
            bool curHas = state.HasFlag(actionToLookFor);

            if (prevHas == curHas)
                return state;

            if (!prevHas || curHas)
            {
                state |= InputActionType.Began;
            }
            else if(!prevFrame.HasFlag(InputActionType.Ended))
            {
                state |= actionToLookFor | InputActionType.Ended;
            }

            return state;
        }
    }

}
