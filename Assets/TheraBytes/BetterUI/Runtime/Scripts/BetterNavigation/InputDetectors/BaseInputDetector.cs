using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine;

namespace TheraBytes.BetterUi
{

    public abstract class BaseInputDetector<TActionMappingType, TModule> : BaseInputDetector<TActionMappingType>, IInputDetector
        where TModule : BaseInputModule
    {
        const float NO_REPEAT_INTERVAL = -1000;

        protected TModule inputModule;

        MoveDirection navigationDirection;
        MoveDirection previousDirection;
        float nextNavigationRepetition = NO_REPEAT_INTERVAL;

        NavigationInfo previousNavigationInfo;
        NavigationInfo currentNavigationInfo;
        public NavigationInfo CurrentNavigationInfo { get { return currentNavigationInfo; } }
        public MoveDirection NavigationDirection { get { return navigationDirection; } }
        public TModule InputModule { get { return inputModule; } }
        BaseInputModule IInputDetector.InputModule { get { return inputModule; } }

        public abstract float InitialRepeatDelay { get; }
        public abstract float ConsecutiveRepeatDelay { get; }
        public abstract bool LastInputWasGamepad { get; }


        protected BaseInputDetector(TModule inputModule)
            : base(inputModule)
        {
            this.inputModule = inputModule;
        }

        public virtual void UpdateCurrentNavigationData()
        {
            previousNavigationInfo = currentNavigationInfo;
            currentNavigationInfo = GetCurrentNavigationInfo();

            HandleConsecutiveNavigation();
        }

        private void HandleConsecutiveNavigation()
        {
            TrackInputChanges();
            UpdateJumpIntervals();

            previousDirection = navigationDirection;
        }

        private void TrackInputChanges()
        {
            if (currentNavigationInfo.Action == InputActionType.None)
                return;

            if (previousNavigationInfo.Action == InputActionType.None)
            {
                nextNavigationRepetition = NO_REPEAT_INTERVAL;
                return;
            }

            if(currentNavigationInfo.Action == previousNavigationInfo.Action)
            { 
                if(currentNavigationInfo.Action == InputActionType.NavigateInAnyDirection)
                {
                    if (previousDirection == navigationDirection)
                        return;
                }

                SetNextInterval();
            }
        }

        private void SetNextInterval()
        {
            if (nextNavigationRepetition > ConsecutiveRepeatDelay)
                return;

            nextNavigationRepetition = ConsecutiveRepeatDelay;
        }

        private void UpdateJumpIntervals()
        {
            if(nextNavigationRepetition == NO_REPEAT_INTERVAL)
            {
                nextNavigationRepetition = InitialRepeatDelay;
                return;
            }

            if(nextNavigationRepetition > 0)
            {
                nextNavigationRepetition -= Time.unscaledDeltaTime;
            }
            else
            {
                while (nextNavigationRepetition < ConsecutiveRepeatDelay)
                {
                    nextNavigationRepetition += ConsecutiveRepeatDelay;
                }
                currentNavigationInfo.Action |= InputActionType.Repeated;
            }
        }

        NavigationInfo GetCurrentNavigationInfo()
        {
            // Keyboard / Gamepad
            bool hasAdditionalActions = inputModuleAddition != null;
            Vector2 direction;
            var action = HandleKeyboardAndGamepad(out direction);
            action = action.GetCombinationWithPreviousFrame(InputDeviceType.DirectionDevice, currentNavigationInfo, false);

            if (action != InputActionType.None)
            {
                if(action.HasFlag(InputActionType.NavigateInAnyDirection))
                {
                    if (!action.HasFlag(InputActionType.Ended))
                    {
                        navigationDirection = NavigationHelper.ToMoveDirection(direction);
                    }

                    return new NavigationInfo(InputDeviceType.DirectionDevice, action, navigationDirection);
                }

                return new NavigationInfo(InputDeviceType.DirectionDevice, action);
            }
            else if (hasAdditionalActions)
            {
                action = HandleAdditionalInputActions();
                action = action.GetCombinationWithPreviousFrame(InputDeviceType.DirectionDevice, currentNavigationInfo, true);

                if(action != InputActionType.None)
                {
                    return new NavigationInfo(InputDeviceType.DirectionDevice, action);
                }
            }

            // Mouse / Touch
            action = HandleMouseAndTouch();
            action = action.GetCombinationWithPreviousFrame(InputDeviceType.PointerDevice, currentNavigationInfo, hasAdditionalActions);

            if (action != InputActionType.None)
                return new NavigationInfo(InputDeviceType.PointerDevice, action);
            
            // No Input
            return new NavigationInfo();
        }

        private InputActionType HandleAdditionalInputActions()
        {
            if (inputModuleAddition.ActivatedButtonX) return InputActionType.ButtonX;
            if (inputModuleAddition.ActivatedButtonY) return InputActionType.ButtonY;

            if (inputModuleAddition.ActivatedSwitchLeft)  return InputActionType.SwitchLeft;
            if (inputModuleAddition.ActivatedSwitchRight) return InputActionType.SwitchRight;

            if (inputModuleAddition.ActivatedAlternativeSwitchLeft)  return InputActionType.AltSwitchLeft;
            if (inputModuleAddition.ActivatedAlternativeSwitchRight) return InputActionType.AltSwitchRight;

            if (inputModuleAddition.ActivatedContextLeft) return InputActionType.ContextLeft;
            if (inputModuleAddition.ActivatedContextRight) return InputActionType.ContextRight;
            if (inputModuleAddition.ActivatedContextDown) return InputActionType.ContextDown;
            if (inputModuleAddition.ActivatedContextUp) return InputActionType.ContextUp;

            return InputActionType.None;
        }

        protected abstract InputActionType HandleKeyboardAndGamepad(out Vector2 direction);
        protected abstract InputActionType HandleMouseAndTouch();

    }

    public abstract class BaseInputDetector<TActionMappingType>
    {
        protected abstract TActionMappingType Submit { get; }
        protected abstract TActionMappingType Cancel { get; }

        protected InputModuleAddition<TActionMappingType> inputModuleAddition;

        protected BaseInputDetector(BaseInputModule inputModule)
        {
            this.inputModuleAddition = inputModule.GetComponent<InputModuleAddition<TActionMappingType>>();
        }

        public TActionMappingType GetInputActionMappingFor(InputActionType actionType)
        {
            switch (actionType)
            {
                case InputActionType.Submit:
                    return Submit;
                case InputActionType.Cancel:
                    return Cancel;


                case InputActionType.PointerPositionChanged:
                    throw new NotSupportedException($"Converting Input action {actionType} is not supported.");

                default:
                    if (inputModuleAddition != null)
                    {
                        return inputModuleAddition.GetInputActionMappingFor(actionType);
                    }
                    else
                    {
                        var actionTypeWithoutTiming = actionType & ~(InputActionType.Began | InputActionType.Repeated | InputActionType.Ended);

                        if (actionTypeWithoutTiming != actionType)
                        {
                            return GetInputActionMappingFor(actionTypeWithoutTiming);
                        }

                        throw new Exception($@"There is no mapping for {actionType}. You probably need to add an ""Addition"" Conponent for the Input Module.");
                    }
            }
        }

    }
}
