using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheraBytes.BetterUi
{
    public enum SelectionOnFocus
    {
        FirstInHierarchy,
        LastInHierarchy,
        Specific,
        ClosestToCoordinate,
        ClosestToPreviousSelection,
        KeepPreviousSelection,

        HighestPriority,
    }

    public enum CancelAction
    {
        None,
        Unfocus,
        DeactivateGameObject,
        DestroyGameObject,
        TriggerButtonClick,
        TriggerCustomEvent,
    }

    public enum CollectingElementsStrategy
    {
        CollectOnInitialization,
        CollectWhenDirty,
        FixedSet,
    }

    public enum DirtyStateDetection
    {
        None,
        CheckCountChange,
        CheckEachSelectable,
    }

    public enum NavigationGroupStacking
    {
        RememberPrevious,
        ForgetPrevious,
        ForgetAll,
    }

    [Flags]
    public enum InputDeviceType
    {
        Unknown = 0,

        /// <summary>
        /// Mouse, Touch, Gaze or Point in XR: Point at the Selectable.
        /// </summary>
        PointerDevice = 1 << 0,

        /// <summary>
        /// Keyboard or Controller: Jump from one Selectable to another by using direction keys / sticks.
        /// </summary>
        DirectionDevice = 1 << 1,
    }

    public enum InteractionCondition
    {
        PartOfCurrentNavigationGroup = 0,
        PartOfActiveNavigationController = 1,
        NoNavigationGroupActive = 2,

        PartOfSpecificNavigationGroup = 3,
        SpecificNavigationControllerActive = 4,
        SpecificNavigationGroupFocused = 5,

        // PartOfNamedNavigationGroup = 6, <-- a bit complicated and probably rarely used
        NamedNavigationControllerActive = 7,
        NamedNavigationGroupFocused = 8,

        CurrentlySelected = 10,
        ToggleCurrentlyOn = 11,
        ToggleCurrentlyOff = 12,

        AnyChildSelected = 15,
        // AnyChildToggleOn = 16,  <-- probably not really useful
        // AnyChildToggleOff = 17, <-- probably not really useful

        AlwaysTrigger = int.MaxValue,
        NeverTrigger = int.MinValue,
    }

    [Flags]
    public enum InputActionType 
    {
        None = 0,

        // navigation actions
        NavigateInAnyDirection = 1 << 0,
        NavigateUp = 1 << 1,
        NavigateLeft = 1 << 2,
        NavigateDown = 1 << 3,
        NavigateRight = 1 << 4,

        // default actions:
        Submit = 1 << 5,
        Cancel = 1 << 6,

        // custom additions:
        ButtonX = 1 << 7,
        ButtonY = 1 << 8,

        ButtonL = 1 << 9,
        ButtonR = 1 << 10,

        MenuButton= 1 << 11,

        SwitchLeft = 1 << 12,
        SwitchRight = 1 << 13,

        AltSwitchLeft = 1 << 14,
        AltSwitchRight = 1 << 15,

        ContextUp = 1 << 16,
        ContextLeft = 1 << 18,
        ContextDown = 1 << 17,
        ContextRight = 1 << 19,

        // probably more work to integrate: right stick
        // AimInAnyDirection = 1 << 20
        // AimUp = 1 << 21,
        // AimLeft = 1 << 22,
        // AimDown = 1 << 23,
        // AimRight = 1 << 24,

        // timing states:
        Began = 1 << 26,
        Repeated = 1 << 27,
        Ended = 1 << 28,

        // General Navigation actions (the negative byte)
        PointerPositionChanged = 1 << 31,
    }
}
