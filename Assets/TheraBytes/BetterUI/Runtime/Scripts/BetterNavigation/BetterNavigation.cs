using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#pragma warning disable 0649 // never assigned warning

namespace TheraBytes.BetterUi
{
    [RequireComponent(typeof(EventSystem))]
    [RequireComponent(typeof(BaseInputModule))]
    [HelpURL("https://documentation.therabytes.de/better-ui/BetterNavigation1.html")]
    // By deriving from selectable, we can access s_Selectables without copying it.
    public class BetterNavigation : Selectable, IElementCollectionContainer<Selectable>
    {
        #region Factory
        public static Dictionary<System.Type, Func<BaseInputModule, IInputDetector>> InputDetectorsForInputModulesFactories
        {
            get;
            private set;
        }

        static BetterNavigation()
        {
            InputDetectorsForInputModulesFactories = new Dictionary<System.Type, Func<BaseInputModule, IInputDetector>>()
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                { typeof(StandaloneInputModule),
                    (module) => new InputDetectorForLegacyInputSystem(module as StandaloneInputModule) },
#endif

#if ENABLE_INPUT_SYSTEM
                { typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule),
                    (module) => new InputDetectorForNewInputSystem(module as UnityEngine.InputSystem.UI.InputSystemUIInputModule) },
#endif

#if REWIRED // <- needs to be added to the player settings manually.
                { typeof(Rewired.Integration.UnityUI.RewiredStandaloneInputModule),
                    (module) => new InputDetectorForRewired(module as Rewired.Integration.UnityUI.RewiredStandaloneInputModule) }
#endif
            };
        }

        static IInputDetector InputDetectorFactory(BaseInputModule inputModule)
        {
            var type = (inputModule != null) ? inputModule.GetType() : null;

            if (!InputDetectorsForInputModulesFactories.TryGetValue(type, out var factory))
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                Debug.LogWarning(
                    $"There is no special Input Detector available for {type?.Name ?? "null"}. Using the Fallback.");

                return new FallbackInputDetector();
#else
                Debug.LogError(
                    $"There is no Input Detector available for {type?.Name ?? "null"}. You may want to activate the legacy input system for a fallback input detector or write an own IInputDetector implementation and add it to `BetterNavigation.InputDetectorsForInputModulesFactories` on startup.");

                return null;
#endif
            }

            return factory(inputModule);
        }
        #endregion

        static int previousSelectableCount;
        static Selectable[] previousSelectables;
        static float lastDirtyTime = 0;
        internal static float LastDirtyTime { get { return lastDirtyTime; } }

        static BetterNavigation current;

        public static BetterNavigation Current
        {
            get
            {
                if (current == null || current.eventSystem != EventSystem.current)
                {
                    current = EventSystem.current?.GetComponent<BetterNavigation>();
                }

                return current;
            }
        }

        static GameObject lastSelectedGameObject;
        static GameObject lastDeselectedGameObject;
        static Selectable lastSelection;

        public static event CurrentSelectableChanged SelectableChanged;
        public static Selectable LastSelection { get { return lastSelection; } }

        [SerializeField] bool handleNavigationInput = true;
        [SerializeField] bool omitSelectionStatesForPointerInput = true;
        [SerializeField] DirtyStateDetection dirtyStateDetection = DirtyStateDetection.CheckCountChange;
        [SerializeField] SelectableCollection rootSelectables;
        EventSystem eventSystem;
        IInputDetector inputDetector;

        InputDeviceType previousNavigationMode = InputDeviceType.DirectionDevice;
        NavigationGroup previousNavigationGroup;
        bool usedCancelActionLastFrame;

        public EventSystem EventSystem
        {
            get
            {
                if (eventSystem == null)
                {
                    eventSystem = GetComponent<EventSystem>();
                }

                return eventSystem;
            }
        }

        public ElementCollection<Selectable> ElementCollection { get { return rootSelectables; } }
        public bool HandleNavigationInput { get { return handleNavigationInput; } set { handleNavigationInput = value; } }

        public IInputDetector InputDetector { get { return inputDetector; } }

        public bool OmitSelectionStatesForPointerInput
        {
            get { return omitSelectionStatesForPointerInput; }
            set
            {
                if (value == omitSelectionStatesForPointerInput)
                    return;

                omitSelectionStatesForPointerInput = value;
                if (omitSelectionStatesForPointerInput)
                {
                    UpdateSelection();
                }
                else
                {
                    ResumeButtonNavigation();
                }
            }
        }

        /// <summary>
        /// returns true if a cancel action has been triggered at the last update 
        /// or if the currently active <see cref="NavigationGroup"/> has a Cancel Action assigned.
        /// </summary>
        public bool HasCancelAction
        {
            get
            {
                return usedCancelActionLastFrame
                    || (NavigationGroup.Current != null
                    && NavigationGroup.Current.CancelAction != CancelAction.None);
            }
        }

        public SelectableCollection CurrentSelectables
        {
            get
            {
                return NavigationGroup.Current != null
                    ? NavigationGroup.Current.SelectableGroup
                    : rootSelectables;
            }
        }

        protected override void Awake()
        {
            DisableSelectableThings();
            StartCoroutine(DelayedStart());
        }

        IEnumerator DelayedStart()
        {
            var inputModule = GetComponent<BaseInputModule>();
            inputDetector = InputDetectorFactory(inputModule);

            // Wait a Frame before initializing to make sure the scene has fully been loaded.
            yield return null;

            rootSelectables?.Initialize(this);
            if (NavigationGroup.Current == null && EventSystem.current != null)
            {
                rootSelectables?.Focus();
            }
        }

        protected override void OnEnable()
        {
            if (handleNavigationInput)
            {
                EventSystem.sendNavigationEvents = false;
            }

            if (NavigationGroup.Current == null && rootSelectables != null && rootSelectables.IsInitialized)
            {
                rootSelectables.Focus();
            }
        }

        protected override void OnDisable()
        {
            // omit base logic
        }

        public override void OnSelect(BaseEventData eventData)
        {
            Debug.LogWarning("Better Navigation got selected. This should never happen.");
            Selectable sel = (lastSelection != null && lastSelection != this)
                ? lastSelection
                : (lastDeselectedGameObject != null && lastDeselectedGameObject != this.gameObject)
                    ? lastDeselectedGameObject.GetComponent<Selectable>()
                    : null;

            if (sel != null)
            {
                sel.Select();
            }
            else if (NavigationGroup.Current != null)
            {
                NavigationGroup.Current.SelectableGroup.SelectInitialElement();
            }
            else
            {
#if UNITY_2021_3_OR_NEWER
                sel = FindObjectsByType<Selectable>(FindObjectsSortMode.None).FirstOrDefault(x => x != this);
#else
                sel = FindObjectsOfType<Selectable>().FirstOrDefault(x => x != this);
#endif
                if (sel != null)
                {
                    sel.Select();
                }
                else
                {
                    throw new Exception("BetterNavigation got selected itself and there is no other selectable that can be switched to.");
                }
            }
        }

        void Update()
        {
            if (inputDetector == null || !rootSelectables.IsInitialized)
                return;

            usedCancelActionLastFrame = false;

            UpdateSelectionIfChanged();
            CheckDirtyState();
            HandleRootGroupFocus();
            inputDetector.UpdateCurrentNavigationData();

            if (inputDetector.CurrentNavigationInfo.Device == InputDeviceType.Unknown)
                return;

            //Debug.Log($"Navigation: {inputDetector.CurrentNavigationInfo.Device} -> {inputDetector.CurrentNavigationInfo.Action} @ {inputDetector.NavigationDirection}");

            HandleNavigation();

            if (inputDetector.CurrentNavigationInfo.Device == previousNavigationMode)
                return;


            previousNavigationMode = inputDetector.CurrentNavigationInfo.Device;
            switch (previousNavigationMode)
            {
                case InputDeviceType.DirectionDevice:
                    ResumeButtonNavigation();
                    break;

                case InputDeviceType.PointerDevice:
                    UpdateSelection();
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        private void CheckDirtyState()
        {
            switch (dirtyStateDetection)
            {
                case DirtyStateDetection.None:
                    break;
                case DirtyStateDetection.CheckCountChange:
                    if (previousSelectableCount != s_SelectableCount)
                    {
                        previousSelectableCount = s_SelectableCount;
                        SetDirty();
                    }
                    break;
                case DirtyStateDetection.CheckEachSelectable:
                    if (HasAnySelectableChanged())
                    {
                        if (previousSelectables == null || previousSelectables.Length < s_SelectableCount)
                        {
                            previousSelectables = new Selectable[s_Selectables.Length];
                        }

                        AllSelectablesNoAlloc(previousSelectables);
                        previousSelectableCount = s_SelectableCount;

                        SetDirty();
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private bool HasAnySelectableChanged()
        {
            if (previousSelectableCount != s_SelectableCount)
                return true;

            if (previousSelectables == null)
                return true;

            for (int i = 0; i < s_SelectableCount; i++)
            {
                if (previousSelectables[i] != s_Selectables[i])
                    return true;
            }

            return false;
        }

        public static void SetDirty()
        {
            lastDirtyTime = Time.realtimeSinceStartup;
            if (NavigationGroup.Current != null)
            {
                NavigationGroup.Current.ReCollectSelectables();
            }
        }

        private void HandleRootGroupFocus()
        {
            if (!ReferenceEquals(previousNavigationGroup, NavigationGroup.Current))
            {
                if (NavigationGroup.Current == null)
                {
                    rootSelectables.Focus();
                }
                else if (previousNavigationGroup == null)
                {
                    rootSelectables.Unfocus();
                }

                previousNavigationGroup = NavigationGroup.Current;
            }
        }

        private void HandleNavigation()
        {
            if (!handleNavigationInput)
                return;

            if (inputDetector.CurrentNavigationInfo.Device != InputDeviceType.DirectionDevice)
                return;

            bool isNavigatingOutOfGroup = false;
            Selectable sel = null;

            var actionType = inputDetector.CurrentNavigationInfo.Action;
            switch (actionType)
            {
                case InputActionType.Began | InputActionType.Submit:
                    if (lastSelection != null)
                    {
                        if (!CanLastSelectionBeSelected())
                            break;

                        var evtData = NavigationHelper.GetBaseEventData(EventSystem);
                        ExecuteEvents.Execute(lastSelectedGameObject, evtData, ExecuteEvents.submitHandler);
                    }
                    break;

                case InputActionType.Began | InputActionType.Cancel:
                    if (lastSelection != null)
                    {
                        if (!CanLastSelectionBeSelected())
                            break;

                        var evtData = NavigationHelper.GetBaseEventData(EventSystem);
                        ExecuteEvents.Execute(lastSelectedGameObject, evtData, ExecuteEvents.cancelHandler);
                        NavigationGroup.Current?.TriggerCancelAction();
                    }
                    break;

                case InputActionType.Began | InputActionType.NavigateInAnyDirection:
                case InputActionType.Repeated | InputActionType.NavigateInAnyDirection:
                    if (LastSelection == null)
                        return;

                    // First we try to get the correct neighbor-selectably from the current selectable
                    // to handle special navigation implementations like scollbars or sliders.
                    sel = NavigationHelper.GetSelectableInDirection(inputDetector.NavigationDirection, LastSelection);
                    if (sel != null)
                    {
                        // If we found a selectable, but it is not part of the current group,
                        // let's try to find another one in the given direction using a custom implementation.
                        // It should be safe to do that here as special selectable implementation
                        // for selecting neighbors usually return null as special case (slider, scrollbar, ...).
                        if (!CurrentSelectables.Contains(sel))
                        {
                            sel = NavigationHelper.FindSelectableInDirection(LastSelection, inputDetector.NavigationDirection, CurrentSelectables);

                            if (sel == null)
                            {
                                isNavigatingOutOfGroup = actionType.HasFlag(InputActionType.Began);
                                break;
                            }
                        }

                        sel.Select();
                    }
                    else
                    {
                        // If the selection is null, the current selectable might have a special implementation
                        // for the movement in the respective direction (like sliders change value).
                        // Therefore, we should trigger OnMove in case there is no selectable.
                        AxisEventData axisData = new AxisEventData(EventSystem)
                        {
                            moveDir = inputDetector.NavigationDirection,
                            selectedObject = lastSelectedGameObject,
                        };

                        LastSelection.OnMove(axisData);
                        Debug.Assert(lastSelectedGameObject == EventSystem.currentSelectedGameObject,
                            $"{lastSelectedGameObject?.name}.OnMove(axisData) actually changed the current selction ({EventSystem.currentSelectedGameObject?.name}). This should not happen here.");

                        // Scrollbar and sliders are special cases.
                        // All other custom "OnMove-Overriders" should Use() the axisData
                        // to inform that navigation is prohibited.
                        isNavigatingOutOfGroup = actionType.HasFlag(InputActionType.Began)
                            && !axisData.used
                            && !(lastSelection is Scrollbar)
                            && !(lastSelection is Slider);
                    }

                    break;
            }

            ButtonInteractionHandler.NotifyAllAboutButtonInteraction(inputDetector.CurrentNavigationInfo);

            if (sel != null || isNavigatingOutOfGroup)
            {
                NavigationController.NotifyActiveAboutMove(inputDetector.NavigationDirection, sel, isNavigatingOutOfGroup);
            }
        }

        bool CanLastSelectionBeSelected()
        {
            return lastSelection.isActiveAndEnabled
                && lastSelection.interactable
                && CurrentSelectables.Contains(lastSelection, collectElements: false);
        }

        private void UpdateSelectionIfChanged()
        {
            var sel = EventSystem.currentSelectedGameObject;
            if (sel == null)
            {
                lastSelectedGameObject = null;

                if (lastSelection == null || !lastSelection.isActiveAndEnabled)
                {
                    lastSelection = null;

                    // if we get here, the selected game object was determined too early:
                    // previous selectables were destroyed and new might be created after focus.
                    // Let's re-Focus.
                    if (CurrentSelectables.ShouldEvaluateInitialElement())
                    {
                        var selection = CurrentSelectables.SelectInitialElement();
                        if (selection == null) // no selectable in navigation group -> see if there is another focusable group
                        {
                            NavigationGroupSwitchController.TryFocusAnotherGroup();
                        }
                    }
                }

                return;
            }

            // is selectable still valid (visible / interactable)?
            if (sel == lastSelectedGameObject
                && (!CurrentSelectables.IsValid(lastSelection) || !CurrentSelectables.Contains(lastSelection)))
            {
                // CONSIDER: first look at sibling components
                var newSelection = sel.transform.GetComponentsInParent<Selectable>(includeInactive: false)
                    .FirstOrDefault(o => o.interactable && CurrentSelectables.Contains(o, false));

                // in case there is no interactable parent: select initial element of group
                if (!CurrentSelectables.IsValid(newSelection))
                {
                    newSelection = CurrentSelectables.GetInitialElement();
                }

                // in case initial element of group is not valid: select the first element that is valid in the group
                if (!CurrentSelectables.IsValid(newSelection))
                {
                    newSelection = CurrentSelectables.Elements.FirstOrDefault();
                }

                if (newSelection != null)
                {
                    SetNewSelection(newSelection.gameObject);
                    return;
                }
                // else:
                // Consider calling `NavigationGroupSwitchController.TryFocusAnotherGroup();`,
                // but make sure the previous input direction is used to find the other group.

            }

            if (sel == lastSelectedGameObject && sel != lastDeselectedGameObject)
                return;

            UpdateSelection();
        }

        private void UpdateSelection()
        {
            var sel = EventSystem.currentSelectedGameObject;
            SetNewSelection(sel);

            if (!omitSelectionStatesForPointerInput)
                return;

            bool isPointerDevice =
                inputDetector.CurrentNavigationInfo.Device == InputDeviceType.PointerDevice
                || (inputDetector.CurrentNavigationInfo.Device == InputDeviceType.Unknown
                && previousNavigationMode == InputDeviceType.PointerDevice);

            if (isPointerDevice)
            {
                DeselectCurrentSelection();
            }
        }

        private void SetNewSelection(GameObject selectableGameObject)
        {
            if (selectableGameObject == null || selectableGameObject == lastSelectedGameObject)
                return;

            var selectable = selectableGameObject.GetComponent<Selectable>();
            bool couldSelect = CurrentSelectables.TrySetSelectedElement(selectable);
            if (!couldSelect)
            {
                DeselectCurrentSelection(force: true);
            }

            var prev = lastSelection;
            lastSelection = selectable;
            lastSelectedGameObject = selectableGameObject;

            if (!couldSelect)
                return;

            lastDeselectedGameObject = null;

            if (NavigationGroup.Current != null)
            {
                NavigationGroup.Current.SelectableGroup.UpdateRememberedElement(lastSelection);
            }

            SelectableChanged?.Invoke(prev, lastSelection);
        }

        internal void DeselectCurrentSelection(bool force = false)
        {
            if (lastSelection == null)
                return;

            if (!force && lastDeselectedGameObject == lastSelectedGameObject)
                return;

            var eventData = NavigationHelper.GetBaseEventData(EventSystem);
            lastSelection.OnDeselect(eventData);
            lastDeselectedGameObject = lastSelection.gameObject;
            SelectableChanged?.Invoke(lastSelection, null);
        }

        private void ResumeButtonNavigation()
        {
            if (lastSelection == null)
                return;

            // Special case: 
            // When trying to select an element where the eventsystem thinks it is already selected,
            // we have to pretend, something else was selected before.
            // As the event system cannot handle null for `SetSelectedGameObject()`,
            // this hacky workaround is needed (select any neighbor before selecting the actual element).
            if (EventSystem.currentSelectedGameObject == lastSelectedGameObject)
            {
                var sel = GetAnyNeighbor(lastSelection);
                if (sel != null)
                {
                    EventSystem.SetSelectedGameObject(sel.gameObject);
                }
            }

            lastSelection.Select();

        }

        Selectable GetAnyNeighbor(Selectable focused)
        {
            return focused.FindSelectableOnUp()
                ?? focused.FindSelectableOnDown()
                ?? focused.FindSelectableOnLeft()
                ?? focused.FindSelectableOnRight();
        }

        Rect IElementCollectionContainer<Selectable>.GetRectOnScreen()
        {
            return new Rect(0, 0, ResolutionMonitor.CurrentResolution.x, ResolutionMonitor.CurrentResolution.y);
        }

        void IElementCollectionContainer<Selectable>.CollectElements(List<Selectable> resultList)
        {
            resultList.Clear();

            List<GameObject> rootObjects = new List<GameObject>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    Debug.LogWarning($"Root Selectables of '{name}' may miss something because scene '{scene.name}' is not fully loaded.");
                    continue;
                }

                scene.GetRootGameObjects(rootObjects);

                foreach (var obj in rootObjects)
                {
                    NavigationHelper.CollectSelectablesBeneath(obj.transform, resultList, false, false);
                }
            }
        }

        internal void NotifyUsedCancelAction()
        {
            usedCancelActionLastFrame = true;
        }

        void DisableSelectableThings()
        {
            base.navigation = new Navigation() { mode = Navigation.Mode.None };
            base.interactable = false;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (interactable)
            {
                DisableSelectableThings();
            }

            if (handleNavigationInput && enabled)
            {
                EventSystem.sendNavigationEvents = false;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (EventSystem.firstSelectedGameObject != null && ElementCollection.InitialElement != EventSystem.firstSelectedGameObject)
            {
                ElementCollection.Initialize(this);

                var selectable = EventSystem.firstSelectedGameObject.GetComponent<Selectable>();
                if (ElementCollection.Contains(selectable))
                {
                    ElementCollection.InitialFocus = SelectionOnFocus.Specific;
                    ElementCollection.InitialElement = EventSystem.firstSelectedGameObject.GetComponent<Selectable>();
                }
                else
                {
                    EventSystem.firstSelectedGameObject = null;
                }

                ElementCollection.UnInitialize();
            }
        }

#endif
    }
}

#pragma warning restore 0649 // never assigned warning
