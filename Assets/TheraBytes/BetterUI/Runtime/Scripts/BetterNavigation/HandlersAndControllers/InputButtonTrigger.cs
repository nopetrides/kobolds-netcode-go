using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#pragma warning disable 0649 // never assigned warning

namespace TheraBytes.BetterUi
{
    [HelpURL("https://documentation.therabytes.de/better-ui/InputButtonTrigger.html")]
    public class InputButtonTrigger : ButtonInteractionHandler
    {
        [Serializable] public class TriggerEventClass : UnityEvent { }

        [SerializeField] InputActionType buttonActionType = InputActionType.ButtonX | InputActionType.Began;
        [SerializeField] bool useCustomEvent;
        [SerializeField] TriggerEventClass triggerEvent;
        [SerializeField] InteractionCondition condition;
        [SerializeField] RectTransform objectForCondition;
        [SerializeField] string nameForCondition;

        [SerializeField] BaseInputActionVisualization visualization;
        public InteractionCondition Condition { get { return condition; } }

        Selectable selectable;

        public InputActionType ButtonActionType 
        { 
            get { return buttonActionType; }
            set
            { 
                buttonActionType = value; 
                MapVisualization(); 
            } 
        }

        public InputActionType ButtonActionTypeWithoutTiming
        {
            get
            {
                return buttonActionType & ~(InputActionType.Began | InputActionType.Repeated | InputActionType.Ended);
            }
        }

        public bool UseCustomAction { get { return useCustomEvent; } set { useCustomEvent = value; } }
        public TriggerEventClass TriggerEvent { get { return triggerEvent; } }

        protected override void OnEnable()
        {
            base.OnEnable();
            selectable = GetComponent<Selectable>();
            if(selectable == null)
            {
                selectable = GetComponentInChildren<Selectable>();
            }

            MapVisualization();
            UpdateVisualizationActiveState(IsConditionFulfilled());
        }

        protected override void OnDisable()
        {
            UpdateVisualizationActiveState(false);
            base.OnDisable();
        }

        protected internal override bool ShouldShowVisualization()
        {
            return base.ShouldShowVisualization() && IsConditionFulfilled();
        }

        protected override void NotifyButtonInteraction(NavigationInfo navigationInfo)
        {
            bool isConditionFulfilled = IsConditionFulfilled();
            UpdateVisualizationActiveState(isConditionFulfilled);

            if (!isConditionFulfilled)
                return;

            if (!IsActionMatched(navigationInfo, buttonActionType))
                return;

            if (useCustomEvent)
            {
                triggerEvent.Invoke();
            }
            else
            {
                if (selectable is Button btn)
                {
                    btn.onClick.Invoke();
                }
                else if (selectable is Toggle tgl)
                {
                    tgl.SetIsOn(!tgl.isOn);
                }
                else
                {
                    Debug.LogWarning($"{nameof(InputButtonTrigger)}: No suitable selectable for interaction found in object {name}. You may want to use a custom event.");
                }
            }
        }

        private bool IsConditionFulfilled()
        {
            if (!useCustomEvent && selectable != null && !selectable.interactable)
            {
                return false;
            }

            switch (condition)
            {
                case InteractionCondition.PartOfCurrentNavigationGroup:
                    return IsPartOfNavigationGroup(NavigationGroup.Current);

                case InteractionCondition.PartOfActiveNavigationController:

                    foreach (var ctrl in NavigationController.ActiveControllers.CleanUpIterator())
                    {
                        foreach (var navGroup in ctrl.ControlledNavigationGroups.Elements)
                        {
                            if (IsPartOfNavigationGroup(navGroup))
                                return true;
                        }
                    }

                    return false;

                case InteractionCondition.NoNavigationGroupActive:
                    return NavigationGroup.Current == null;

                case InteractionCondition.PartOfSpecificNavigationGroup:
                {
                    return objectForCondition != null
                        && objectForCondition.TryGetComponent<NavigationGroup>(out var navigationGroup)
                        && IsPartOfNavigationGroup(navigationGroup);
                }

                case InteractionCondition.SpecificNavigationGroupFocused:
                {
                    return objectForCondition != null
                        && objectForCondition.TryGetComponent<NavigationGroup>(out var navigationGroup)
                        && navigationGroup.IsFocused;
                }

                case InteractionCondition.SpecificNavigationControllerActive:
                    return objectForCondition != null
                        && objectForCondition.TryGetComponent<NavigationController>(out var navigationController)
                        && NavigationController.ActiveControllers.Contains(navigationController);

                case InteractionCondition.NamedNavigationControllerActive:
                    return NavigationController.ActiveControllers.Any(o => o.name == nameForCondition);

                case InteractionCondition.NamedNavigationGroupFocused:
                    return NavigationGroup.Current != null && NavigationGroup.Current.name == nameForCondition;

                case InteractionCondition.AnyChildSelected:
                    if (objectForCondition == null || BetterNavigation.LastSelection == null)
                        return false;

                    var parent = BetterNavigation.LastSelection.transform;
                    while(parent != null)
                    {
                        parent = parent.parent;

                        if (parent == objectForCondition)
                            return true;
                    }

                    return false;

                case InteractionCondition.CurrentlySelected:
                    return objectForCondition != null 
                        && BetterNavigation.LastSelection != null
                        && BetterNavigation.LastSelection.transform == objectForCondition;

                case InteractionCondition.ToggleCurrentlyOn:
                    var onTgl = objectForCondition.GetComponent<Toggle>();
                    return onTgl != null && onTgl.isOn;

                case InteractionCondition.ToggleCurrentlyOff:
                    var offTgl = objectForCondition.GetComponent<Toggle>();
                    return offTgl != null && !offTgl.isOn;

                case InteractionCondition.AlwaysTrigger:
                    return true;

                case InteractionCondition.NeverTrigger:
                    return false;

                default:
                    throw new NotImplementedException();
            }
        }

        private void UpdateVisualizationActiveState(bool isActive)
        {
            if(visualization == null) 
                return;

            isActive = isActive && ButtonActionTypeWithoutTiming != InputActionType.None;
            visualization.SetHandlerActiveness(isActive);
        }

        void MapVisualization()
        {
            if (visualization == null)
                return;

            visualization.AssignHandler(this);
            visualization.MapInputActionVisualization(buttonActionType);
        }

        private bool IsPartOfNavigationGroup(NavigationGroup navigationGroup)
        {
            if (selectable == null || navigationGroup == null)
            {
                return navigationGroup != null
                    && navigationGroup.gameObject == this.gameObject;
            }

            return navigationGroup.SelectableGroup.Contains(selectable);
        }

    }
}

#pragma warning restore 0649 // never assigned warning