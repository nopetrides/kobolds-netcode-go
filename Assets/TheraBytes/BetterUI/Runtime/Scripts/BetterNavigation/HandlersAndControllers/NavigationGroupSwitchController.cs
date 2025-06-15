using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using IAT = TheraBytes.BetterUi.InputActionType;
using System.Collections;

#pragma warning disable 0649 // never assigned warning

namespace TheraBytes.BetterUi
{
    [DefaultExecutionOrder(300)]
    [HelpURL("https://documentation.therabytes.de/better-ui/NavigationGroupSwitchController.html")]
    public class NavigationGroupSwitchController : DefaultMoveNavigationControllerBase, IElementCollectionContainer<NavigationGroup>
    {
        public static bool TryFocusAnotherGroup(bool requiresSelectableInGroup = true)
        {
            if (NavigationGroup.Current == null)
            {
                foreach (var ctrl in ActiveControllers.OfType<NavigationGroupSwitchController>())
                {
                    if (TryFocus(ctrl.ControlledNavigationGroups.InitialElement))
                        return true;

                    foreach (var group in ctrl.ControlledNavigationGroups.Elements)
                    {
                        if (group == ctrl.ControlledNavigationGroups.InitialElement)
                            continue;

                        if(TryFocus(group))
                            return true;
                    }
                }
            }
            else
            {
                var controllers = ActiveControllers
                    .OfType<NavigationGroupSwitchController>()
                    .Where(o => o.ControlledNavigationGroups.Contains(NavigationGroup.Current));

                foreach (var ctrl in controllers)
                {
                    // TODO: order elements: go down / right, jump to upper right
                    foreach (var group in ctrl.ControlledNavigationGroups.Elements)
                    {
                        if(group == NavigationGroup.Current)
                            continue;

                        TryFocus(group);
                        return true;
                    }
                }
            }

            return false;

            // local function
            bool TryFocus(NavigationGroup group)
            {
                if (group == null || !group.isActiveAndEnabled)
                    return false;

                if (requiresSelectableInGroup && !group.SelectableGroup.Elements.Any())
                    return false;

                group.Focus();
                return true;
            }
        }

        [SerializeField] IAT navigateUp = IAT.ContextUp | IAT.Began | IAT.Repeated;
        [SerializeField] IAT navigateDown = IAT.ContextDown | IAT.Began | IAT.Repeated;
        [SerializeField] IAT navigateLeft = IAT.ContextLeft | IAT.Began | IAT.Repeated;
        [SerializeField] IAT navigateRight = IAT.ContextRight | IAT.Began | IAT.Repeated;

        [SerializeField] NavigationGroupCollection navigationGroups;
        [SerializeField] bool triggerCurrentToggleOnSwitch;

        NavigationGroup currentGroup;
        public override IAT NavigateUpInput { get { return navigateUp; } }
        public override IAT NavigateDownInput { get { return navigateDown; } }
        public override IAT NavigateLeftInput { get { return navigateLeft; } }
        public override IAT NavigateRightInput { get { return navigateRight; } }

        public override NavigationGroupCollection ControlledNavigationGroups { get { return navigationGroups; } }
        ElementCollection<NavigationGroup> IElementCollectionContainer<NavigationGroup>.ElementCollection => navigationGroups;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            NotifyFocusChanged(currentGroup, NavigationGroup.Current);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        protected override IEnumerator InitializationRoutine()
        {
            var baseEnumerator = base.InitializationRoutine();
            while (baseEnumerator.MoveNext())
            {
                yield return baseEnumerator.Current;
            }

            navigationGroups.Initialize(this);
            foreach (var group in navigationGroups.LastElements)
            {
                Debug.Assert(group != null, $"NavigationGroupController '{name}' contains null-Elements");
                group.FocusTiming = NavigationGroup.FocusTime.DoNotFocusAutomatically;
            }

            yield break;
        }

        protected override void OnInitialNavigationGroupSelected(NavigationGroup group)
        {
            this.currentGroup = group;
        }

        protected override void NotifyFocusChanged(NavigationGroup previousGroup, NavigationGroup newGroup)
        {
            base.NotifyFocusChanged(previousGroup, newGroup);

            if (currentGroup == newGroup)
                return;

            if(navigationGroups.Contains(newGroup))
            {
                currentGroup = newGroup;
            }
        }

        protected override void NotifyButtonInteraction(NavigationInfo navigationInfo, bool shouldSmartMove, bool forceProvidedDirection = false)
        {
            if (currentGroup == null)
                return;

            if (!currentGroup.IsFocused)
            {
                if (!navigationGroups.Contains(NavigationGroup.Current))
                    return;

                SetCurrentGroup(NavigationGroup.Current);
            }

            var direction = (forceProvidedDirection)
                ? navigationInfo.Direction
                : base.GetMatchingMoveDirection(navigationInfo);

            if (direction == MoveDirection.None)
                return;

            var dirVector = NavigationHelper.ToDirectionVector(direction);

            RectTransform origin = currentGroup.transform as RectTransform;
            var newElement = NavigationHelper.FindElementInDirection(origin, dirVector, navigationGroups.Elements);

            if (newElement != null)
            {
                if(triggerCurrentToggleOnSwitch)
                {
                    TriggerCurrentToggle();
                }

                PrepareSwitchingContext(direction, BetterNavigation.LastSelection, shouldSmartMove);
                SupplySelectableChooser(shouldSmartMove);

                SetCurrentGroup(newElement);
            }
        }

        private void TriggerCurrentToggle()
        {
            var sel = BetterNavigation.LastSelection;
            if (sel == null || !currentGroup.IsFocused || !currentGroup.SelectableGroup.Contains(sel))
                return;

            if(sel is Toggle tgl)
            {
                tgl.isOn = true;
            }
        }

        private void SetCurrentGroup(NavigationGroup newElement)
        {
            currentGroup = newElement;
            bool isControllingCurrent = ControlledNavigationGroups.Contains(NavigationGroup.Current);
            NavigationGroupStacking previousGroupStacking = isControllingCurrent
                ? NavigationGroupStacking.ForgetPrevious
                : NavigationGroupStacking.RememberPrevious;

            currentGroup.Focus(previousGroupStacking, force: isControllingCurrent);
        }

        void IElementCollectionContainer<NavigationGroup>.CollectElements(List<NavigationGroup> resultList)
        {
            GetComponentsInChildren(includeInactive: false, result: resultList);
        }

        Rect IElementCollectionContainer<NavigationGroup>.GetRectOnScreen()
        {
            return (transform as RectTransform).ToScreenRect();
        }
    }
}

#pragma warning restore 0649 // never assigned warning
