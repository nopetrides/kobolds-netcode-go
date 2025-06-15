using System;
using UnityEngine;
using static TheraBytes.BetterUi.BaseInputActionVisualization;

namespace TheraBytes.BetterUi
{
    public abstract class BaseInputActionVisualization : MonoBehaviour
    {
        protected internal enum ActiveState
        {
            Uninitialized,
            Active,
            Inactive,
        }


        internal protected struct State
        {
            internal ActiveState stateOfHandler;
            internal ActiveState stateOfSelf;

            internal ActiveState CombinedState
                => (stateOfHandler == ActiveState.Active && stateOfSelf == ActiveState.Active)
                            ? ActiveState.Active
                            : (stateOfHandler == ActiveState.Uninitialized || stateOfHandler == ActiveState.Uninitialized)
                            ? ActiveState.Uninitialized
                            : ActiveState.Inactive;
        }

        protected State currentState;

        InputActionType currentInputActionType;
        ButtonInteractionHandler handler;

        public bool IsCurrentlyActive { get { return currentState.CombinedState.AsBool(); } }
        public InputActionType CurrentInputActionType { get {  return currentInputActionType; } }

        public void AssignHandler(ButtonInteractionHandler handler)
        {
            this.handler = handler;
        }

        protected void MapCurrentInputActionVisualizationAgain()
        {
            MapInputActionVisualization(currentInputActionType);
        }

        public void MapInputActionVisualization(InputActionType inputActionType)
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
                return;
#endif

            var actionTypeWithoutTimings = inputActionType & ~(InputActionType.Began | InputActionType.Repeated | InputActionType.Ended);

            currentInputActionType = actionTypeWithoutTimings;
            if(actionTypeWithoutTimings == InputActionType.None)
            {
                SetActivenessLogic(false);
            }
            else
            {
                MapInputActionVisualizationLogic(actionTypeWithoutTimings);
                SetActivenessLogic(IsCurrentlyActive);
            }
        }

        protected virtual bool IsAllowedToBeActive()
        {
            return true;
        }

        public void UpdateActiveness()
        {
            if (currentInputActionType == InputActionType.None)
                return;

            bool allowedSelf = IsAllowedToBeActive();
            bool allowedHandler = handler.ShouldShowVisualization();

            if ((allowedSelf && allowedHandler) == IsCurrentlyActive)
                return;

            currentState.stateOfSelf = allowedSelf.ToActiveState();
            currentState.stateOfHandler = allowedHandler.ToActiveState();

            if(IsCurrentlyActive)
            {
                MapCurrentInputActionVisualizationAgain();
            }
            else
            {
                SetActivenessLogic(false);
            }
        }

        public void SetHandlerActiveness(bool isActive)
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
                return;
#endif
            var targetState = isActive.ToActiveState();
            if (targetState == currentState.stateOfHandler)
                return;

            currentState.stateOfHandler = targetState;
            currentState.stateOfSelf = IsAllowedToBeActive().ToActiveState();
            SetActivenessLogic(IsCurrentlyActive);
        }

        protected abstract void MapInputActionVisualizationLogic(InputActionType inputActionType);
        protected abstract void SetActivenessLogic(bool isCurrentlyActive);

#if ENABLE_LEGACY_INPUT_MANAGER
        public static string GetLegacyInputActionName(InputActionType inputActionType)
        {
            Debug.Assert(BetterNavigation.Current != null
                && BetterNavigation.Current.InputDetector is InputDetectorForLegacyInputSystem,
                "Trying to retrieve legacy input action name while the current input is for a different input system.");

            return GetInputSystemActionType<string>(inputActionType);
        }
#endif

#if ENABLE_INPUT_SYSTEM
        public static UnityEngine.InputSystem.InputActionReference GetInputSystemActionReference(InputActionType inputActionType)
        {
            Debug.Assert(BetterNavigation.Current != null
                && BetterNavigation.Current.InputDetector is InputDetectorForNewInputSystem,
                "Trying to retrieve an input action reference while the current input is for a different input system.");

            return GetInputSystemActionType<UnityEngine.InputSystem.InputActionReference>(inputActionType);
        }
#endif

#if REWIRED
        public static int GetRewiredActionId(InputActionType inputActionType)
        {
            Debug.Assert(BetterNavigation.Current != null
                && BetterNavigation.Current.InputDetector is InputDetectorForRewired,
                "Trying to retrieve rewired action while the current input is for a different input system.");

            return GetInputSystemActionType<int>(inputActionType);
        }

        public static Rewired.InputAction GetRewiredAction(InputActionType inputActionType)
        {
            if (!Rewired.ReInput.isReady)
            {
                Debug.LogError("Cannot get Rewired Action: Rewired is not ready yet.");
                return null;
            }

            int id = GetRewiredActionId(inputActionType);
            return Rewired.ReInput.mapping.GetAction(id);
        }
#endif

        public static T GetInputSystemActionType<T>(InputActionType inputActionType)
        {
            if (BetterNavigation.Current == null)
            {
                Debug.LogError($"{nameof(InputButtonTrigger)} Cannot look up input mapping: there is no {nameof(BetterNavigation)} currently active.");

                return default;
            }

            if (!(BetterNavigation.Current.InputDetector is BaseInputDetector<T> detector))
            {
                throw new Exception($"The current input detector does not provide mappings for action mapping type {typeof(T).Name}.");
            }

            return detector.GetInputActionMappingFor(inputActionType);
        }
    }


    internal static class ActiveStateExtensions
    {
        public static bool AsBool(this ActiveState self) 
            => self == ActiveState.Active;

        public static ActiveState ToActiveState(this bool self)
            => (self) ? ActiveState.Active : ActiveState.Inactive;
    }
}
