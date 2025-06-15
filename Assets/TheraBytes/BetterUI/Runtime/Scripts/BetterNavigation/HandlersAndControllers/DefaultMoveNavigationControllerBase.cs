using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#pragma warning disable 0649 // never assigned warning

namespace TheraBytes.BetterUi
{
    [DefaultExecutionOrder(-100)] // be earlier than navigation groups to prevent wrong condition states
    public abstract class DefaultMoveNavigationControllerBase : NavigationController
    {
        [SerializeField] bool switchGroupByMoving = true;
        [SerializeField] protected bool smartSelectWhenSwitchingViaMovement = true;
        [SerializeField] protected bool smartSelectWhenSwitchingViaButton = true;

        [SerializeField] BaseInputActionVisualization navigateLeftVisualization;
        [SerializeField] BaseInputActionVisualization navigateRightVisualization;
        [SerializeField] BaseInputActionVisualization navigateUpVisualization;
        [SerializeField] BaseInputActionVisualization navigateDownVisualization;

        protected NavigationControllerSelectableChooser selectableChooser;

        public bool SwitchGroupByMoving
        {
            get { return switchGroupByMoving; }
            set { switchGroupByMoving = value; }
        }

        public abstract InputActionType NavigateLeftInput { get; }
        public abstract InputActionType NavigateRightInput { get; }
        public abstract InputActionType NavigateUpInput { get; }
        public abstract InputActionType NavigateDownInput { get; }

        public BaseInputActionVisualization NavigateLeftVisualization { get { return navigateLeftVisualization; } }
        public BaseInputActionVisualization NavigateRightVisualization { get { return navigateRightVisualization; } }
        public BaseInputActionVisualization NavigateUpVisualization { get { return navigateUpVisualization; } }
        public BaseInputActionVisualization NavigateDownVisualization { get { return navigateDownVisualization; } } 


        protected override void Awake()
        {
            base.Awake();
            selectableChooser = new NavigationControllerSelectableChooser(this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            MapVisualizations();
            SetVisualizationActiveness(true);
        }

        protected override void OnDisable()
        {
            SetVisualizationActiveness(false);
            base.OnDisable();
        }

        protected override void NotifyMove(MoveDirection moveDirection, Selectable current, bool movingOutOfGroup)
        {
            if(!movingOutOfGroup)
            {
                selectableChooser.NotifyNavigation(moveDirection, current);
            }

            if (!movingOutOfGroup || !switchGroupByMoving)
                return;

            if (moveDirection == MoveDirection.None)
                return;

            NavigationInfo interaction = new NavigationInfo(InputDeviceType.DirectionDevice, InputActionType.None, moveDirection);

            PrepareSwitchingContext(moveDirection, current, smartSelectWhenSwitchingViaMovement);
            NotifyButtonInteraction(interaction, smartSelectWhenSwitchingViaMovement, forceProvidedDirection: true);
        }

        protected override void NotifyButtonInteraction(NavigationInfo navigationInfo)
        {
            NotifyButtonInteraction(navigationInfo, smartSelectWhenSwitchingViaButton);
        }

        protected abstract void NotifyButtonInteraction(NavigationInfo navigationInfo, bool shouldSmartMove, bool forceProvidedDirection = false);

        protected MoveDirection GetMatchingMoveDirection(NavigationInfo navigationInfo)
        {
            if (IsActionMatched(navigationInfo, NavigateLeftInput))
                return MoveDirection.Left;

            else if (IsActionMatched(navigationInfo, NavigateRightInput))
                return MoveDirection.Right;

            else if (IsActionMatched(navigationInfo, NavigateUpInput))
                return MoveDirection.Up;

            else if (IsActionMatched(navigationInfo, NavigateDownInput))
                return MoveDirection.Down;

            return MoveDirection.None;
        }

        protected void SupplySelectableChooser(bool shouldSmartMove)
        {
            if (!shouldSmartMove)
                return;

            NavigationGroup.SupplySelectableChooser(selectableChooser, this);
        }

        protected void PrepareSwitchingContext(MoveDirection moveDirection, Selectable current, bool shouldSmartMove)
        {
            if (!shouldSmartMove)
                return;

            selectableChooser.PrepareSwitchingContext(moveDirection, current);
        }

        protected void MapVisualizations()
        {
            MapVisualization(NavigateLeftVisualization, NavigateLeftInput);
            MapVisualization(NavigateRightVisualization, NavigateRightInput);
            MapVisualization(NavigateUpVisualization, NavigateUpInput);
            MapVisualization(NavigateDownVisualization, NavigateDownInput);
        }

        void MapVisualization(BaseInputActionVisualization visualization, InputActionType inputActionType)
        {
            if (visualization == null)
                return;

            visualization.AssignHandler(this);
            visualization.MapInputActionVisualization(inputActionType);
        }

        private void SetVisualizationActiveness(bool isActive)
        {
            SetVisualizationActiveness(NavigateLeftVisualization, NavigateLeftInput, isActive);
            SetVisualizationActiveness(NavigateRightVisualization, NavigateRightInput, isActive);
            SetVisualizationActiveness(NavigateUpVisualization, NavigateUpInput, isActive);
            SetVisualizationActiveness(NavigateDownVisualization, NavigateDownInput, isActive);
        }

        static void SetVisualizationActiveness(BaseInputActionVisualization visualization, InputActionType inputActionType, bool isActive)
        {
            if (visualization == null)
                return;

            isActive = isActive && inputActionType != InputActionType.None;
            visualization.SetHandlerActiveness(isActive);
        }
    }
}

#pragma warning restore 0649 // never assigned warning
