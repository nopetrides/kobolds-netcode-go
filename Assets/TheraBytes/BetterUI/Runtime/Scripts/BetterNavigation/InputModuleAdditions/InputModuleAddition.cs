using System;
using UnityEngine;
using UnityEngine.EventSystems;

#pragma warning disable 0649 // never assigned warning

namespace TheraBytes.BetterUi
{
    public abstract class InputModuleAdditionWithDualAxisNavigation<TType, TModule>
        : InputModuleAddition<TType, TModule> where TModule : BaseInputModule
    {
        enum Axis { Horizontal, Vertical }

        [SerializeField] Axis navigationVisualizationAxisBinding;
        [SerializeField] bool mapAllNavigationActionsToThisAxis;

        protected override TType NavigateInAnyDirection
            => (navigationVisualizationAxisBinding == Axis.Horizontal) ? HorizontalAxis : VerticalAxis;

        protected override TType NavigateUp
            => (mapAllNavigationActionsToThisAxis) ? NavigateInAnyDirection : VerticalAxis;
        protected override TType NavigateDown
            => (mapAllNavigationActionsToThisAxis) ? NavigateInAnyDirection : VerticalAxis;
        protected override TType NavigateLeft
            => (mapAllNavigationActionsToThisAxis) ? NavigateInAnyDirection : HorizontalAxis;
        protected override TType NavigateRight
            => (mapAllNavigationActionsToThisAxis) ? NavigateInAnyDirection : HorizontalAxis;

        protected abstract TType HorizontalAxis { get; }
        protected abstract TType VerticalAxis { get; }
    }

    [RequireComponent(typeof(BaseInputModule))]
    public abstract class InputModuleAddition<TType, TModule> : InputModuleAddition<TType>
        where TModule : BaseInputModule
    {

        TModule inputModule;
        public TModule InputModule
        {
            get
            {
                if (inputModule == null)
                {
                    inputModule = GetComponent<TModule>();
                }

                Debug.Assert(inputModule != null,
                    $"An Input Module of type `{typeof(TModule).Name} is expected but wasn't found on {gameObject.name}.");

                return inputModule;
            }
        }
    }

    public abstract class InputModuleAddition<TType> : InputModuleAddition
    {
        [SerializeField] protected TType buttonX;
        [SerializeField] protected TType buttonY;
        [SerializeField] protected TType buttonL;
        [SerializeField] protected TType buttonR;
        [SerializeField] protected TType menuButton;
        [Space]
        [SerializeField] protected TType switchLeft;
        [SerializeField] protected TType switchRight;
        [SerializeField] protected TType alternativeSwitchLeft;
        [SerializeField] protected TType alternativeSwitchRight;
        [Space]
        [SerializeField] protected TType contextLeft;
        [SerializeField] protected TType contextRight;
        [SerializeField] protected TType contextUp;
        [SerializeField] protected TType contextDown;

        protected abstract TType NavigateInAnyDirection { get; }
        protected abstract TType NavigateUp { get; }
        protected abstract TType NavigateDown { get; }
        protected abstract TType NavigateLeft { get; }
        protected abstract TType NavigateRight { get; }

        public TType GetInputActionMappingFor(InputActionType actionType)
        {
            actionType &= ~(InputActionType.Began | InputActionType.Repeated | InputActionType.Ended);
            switch (actionType)
            {

                case InputActionType.NavigateInAnyDirection: return NavigateInAnyDirection;
                case InputActionType.NavigateUp: return NavigateUp;
                case InputActionType.NavigateDown: return NavigateDown;
                case InputActionType.NavigateLeft: return NavigateLeft;
                case InputActionType.NavigateRight: return NavigateRight;

                case InputActionType.ButtonX: return buttonX;
                case InputActionType.ButtonY: return buttonY;
                case InputActionType.ButtonL: return buttonL;
                case InputActionType.ButtonR: return buttonR;
                case InputActionType.MenuButton: return menuButton;

                case InputActionType.SwitchLeft: return switchLeft;
                case InputActionType.SwitchRight: return switchRight;
                case InputActionType.AltSwitchLeft: return alternativeSwitchLeft;
                case InputActionType.AltSwitchRight: return alternativeSwitchRight;

                case InputActionType.ContextUp: return contextUp;
                case InputActionType.ContextDown: return contextDown;
                case InputActionType.ContextLeft: return contextLeft;
                case InputActionType.ContextRight: return contextRight;
                default:
                    throw new NotSupportedException($"Input Mapping Lookup for action type {actionType} is not supported.");
            }
        }

        void Update()
        {
            ActivatedButtonX = IsDown(buttonX);
            ActivatedButtonY = IsDown(buttonY);
            ActivatedButtonL = IsDown(buttonL);
            ActivatedButtonR = IsDown(buttonR);
            ActivatedMenuButton = IsDown(menuButton);

            ActivatedSwitchLeft = IsDown(switchLeft);
            ActivatedSwitchRight = IsDown(switchRight);
            ActivatedAlternativeSwitchLeft = IsDown(alternativeSwitchLeft);
            ActivatedAlternativeSwitchRight = IsDown(alternativeSwitchRight);

            ActivatedContextLeft = IsDown(contextLeft);
            ActivatedContextRight = IsDown(contextRight);
            ActivatedContextUp = IsDown(contextUp);
            ActivatedContextDown = IsDown(contextDown);
        }

        protected abstract bool IsDown(TType button);
    }

    public abstract class InputModuleAddition : MonoBehaviour
    {
        public bool ActivatedButtonX { get; protected set; }
        public bool ActivatedButtonY { get; protected set; }
        public bool ActivatedButtonL { get; protected set; }
        public bool ActivatedButtonR { get; protected set; }
        public bool ActivatedMenuButton { get; protected set; }

        public bool ActivatedSwitchLeft { get; protected set; }
        public bool ActivatedSwitchRight { get; protected set; }

        public bool ActivatedAlternativeSwitchLeft { get; protected set; }
        public bool ActivatedAlternativeSwitchRight { get; protected set; }

        public bool ActivatedContextLeft { get; protected set; }
        public bool ActivatedContextRight { get; protected set; }
        public bool ActivatedContextUp { get; protected set; }
        public bool ActivatedContextDown { get; protected set; }


    }
}

#pragma warning restore 0649 // never assigned warning
