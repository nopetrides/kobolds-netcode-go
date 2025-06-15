#if REWIRED // <- needs to be added to the player settings manually.

using Rewired;
using Rewired.Integration.UnityUI;
using Rewired.UI;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{
    public class InputDetectorForRewired : BaseInputDetector<int, RewiredStandaloneInputModule>
    {
        public override float InitialRepeatDelay 
        {
            get
            { 
                return (inputModule.repeatDelay > 0)
                    ? inputModule.repeatDelay 
                    : ConsecutiveRepeatDelay; 
            }
        }

        public override float ConsecutiveRepeatDelay { get { return 1f / inputModule.inputActionsPerSecond; } }

        
        protected override int Submit => InputModule.SubmitActionId;
        protected override int Cancel => InputModule.CancelActionId;

        public override bool LastInputWasGamepad => lastInputWasGamepad;

        static bool lastInputWasGamepad;
        Vector2 lastMousePosition;

        public InputDetectorForRewired(RewiredStandaloneInputModule inputModule) 
            : base(inputModule)
        {
#if !ENABLE_LEGACY_INPUT_MANAGER
            Debug.LogError(
                "The Rewired Input Detector only works with the legacy input module (unless you are using Player Mice)");
#endif
        }

        public override void UpdateCurrentNavigationData()
        {
            if (!Rewired.ReInput.isReady)
                return;

            base.UpdateCurrentNavigationData();
        }


        protected override InputActionType HandleKeyboardAndGamepad(out Vector2 direction)
        {
            direction = new Vector2();

            if (CheckButton(inputModule, inputModule.SubmitActionId))
                return InputActionType.Submit;

            if (CheckButton(inputModule, inputModule.CancelActionId))
                return InputActionType.Cancel;

            if (IsNavigatingWithAxis(ref direction))
            {
                direction = NavigationHelper.ToEightWayDirection(direction.x, direction.y);
                return InputActionType.NavigateInAnyDirection;
            }

            return InputActionType.None;
        }

        protected override InputActionType HandleMouseAndTouch()
        {
            var mode = CheckMouseInteraction();
            if (mode != InputActionType.None)
                return mode;

            mode = CheckTouchInteraction();
            if (mode != InputActionType.None)
                return mode;

            return InputActionType.None;
        }

        // NOTE: The following methods could use an abstraction that allows a delegate parameter for the actual check.
        //       For performance / allocation reason, the code for iterating over the players are copied instead.

        public static bool CheckButton(RewiredStandaloneInputModule inputModule, int actionId)
        {
            for (int p = 0; p < inputModule.RewiredPlayerIds.Length; p++)
            {
                int playerId = inputModule.RewiredPlayerIds[p];
                Rewired.Player player = Rewired.ReInput.players.GetPlayer(playerId);
                
                if (player == null)
                    continue;

                if (inputModule.UsePlayingPlayersOnly && !player.isPlaying)
                    continue;

                // button check
                if (player.GetButton(actionId))
                {
                    if (player.IsCurrentInputSource(actionId, Rewired.ControllerType.Mouse))
                        continue;

                    lastInputWasGamepad = player.IsCurrentInputSource(actionId, ControllerType.Joystick);
                    return true;
                }
            }

            return false;
        }

        private bool IsNavigatingWithAxis(ref Vector2 direction)
        {
            for (int p = 0; p < inputModule.RewiredPlayerIds.Length; p++)
            {
                int playerId = inputModule.RewiredPlayerIds[p];
                Rewired.Player player = Rewired.ReInput.players.GetPlayer(playerId);

                if (player == null)
                    continue;

                if (inputModule.UsePlayingPlayersOnly && !player.isPlaying)
                    continue;

                var horizontal = player.GetAxis(inputModule.HorizontalActionId);
                var vertical = player.GetAxis(inputModule.VerticalActionId);
                if (!Mathf.Approximately(horizontal, 0f) || !Mathf.Approximately(vertical, 0f))
                {
                    if (player.IsCurrentInputSource(inputModule.HorizontalActionId, Rewired.ControllerType.Mouse))
                        continue;

                    if (player.IsCurrentInputSource(inputModule.VerticalActionId, Rewired.ControllerType.Mouse))
                        continue;

                    lastInputWasGamepad 
                        = player.IsCurrentInputSource(inputModule.HorizontalActionId, ControllerType.Joystick)
                        || player.IsCurrentInputSource(inputModule.VerticalActionId, ControllerType.Joystick);

                    direction = new Vector2(horizontal, vertical);
                    return true;
                }
            }

            return false;
        }

        private InputActionType CheckMouseInteraction()
        {
            if (!inputModule.allowMouseInput)
                return InputActionType.None;

            // use default mouse
            if(inputModule.PlayerMice.Count == 0)
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                var mouseResult = InputDetectorForLegacyInputSystem.GetMouseActionType(inputModule.input, ref lastMousePosition);

                if (mouseResult != InputActionType.None)
                {
                    lastInputWasGamepad = false;
                }
                return mouseResult;
#else
                return InputActionType.None;
#endif
            }

            // use rewired player mice
            for (int p = 0; p < inputModule.RewiredPlayerIds.Length; p++)
            {
                int playerId = inputModule.RewiredPlayerIds[p];
                Rewired.Player player = Rewired.ReInput.players.GetPlayer(playerId);

                if (player == null)
                    continue;

                if (inputModule.UsePlayingPlayersOnly && !player.isPlaying)
                    continue;

                int mouseCount = inputModule.GetMouseInputSourceCount(playerId);
                for (int i = 0; i < mouseCount; i++)
                {
                    IMouseInputSource source = inputModule.GetMouseInputSource(playerId, i);
                    if (source == null)
                        continue;

                    if (source.GetButton(0))
                    {
                        lastInputWasGamepad = false;
                        return InputActionType.Submit;
                    }

                    if (source.screenPositionDelta.sqrMagnitude > 0f)
                    {
                        lastInputWasGamepad = false;
                        return InputActionType.PointerPositionChanged;
                    }
                }
            }

            return InputActionType.None;
        }

        private InputActionType CheckTouchInteraction()
        {
            if (!inputModule.allowTouchInput)
                return InputActionType.None;

            InputActionType touchResult = InputActionType.None;

            for (int p = 0; p < inputModule.RewiredPlayerIds.Length; p++)
            {
                int playerId = inputModule.RewiredPlayerIds[p];
                var source = inputModule.GetTouchInputSource(playerId, 0);

                for (int i = 0; i < source.touchCount; i++)
                {
                    Touch touch = source.GetTouch(i);

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
            }

            if (touchResult != InputActionType.None)
            {
                lastInputWasGamepad = false;
            }

            return touchResult;
        }
    }
}

#endif