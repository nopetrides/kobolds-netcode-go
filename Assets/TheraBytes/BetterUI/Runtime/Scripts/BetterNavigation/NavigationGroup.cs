using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#pragma warning disable 0649 // never assigned warning

namespace TheraBytes.BetterUi
{
    [DefaultExecutionOrder(100)] // invoke "Awake" a bit later than other scripts in case selectables are added on the fly.

    [HelpURL("https://documentation.therabytes.de/better-ui/NavigationGroup.html")]
    public class NavigationGroup : UIBehaviour, IElementCollectionContainer<Selectable>, IElementChooserProvider<Selectable>
    {
        [Serializable] public class CancelEvent : UnityEvent { }

        const string TRANSITION_FOCUS = "Focus";
        const string TRANSITION_UNFOCUS = "Unfocus";

        public enum FocusTime
        {
            OnEnable,
            OneFrameAfterOnEnable,
            DoNotFocusAutomatically,
        }

        static Stack<NavigationGroup> navigationGroups = new Stack<NavigationGroup>();
        static ISelectableChooser selectableChooser;

        public static event NavigationGroupFocusChanged OnFocusChanged;

        public static NavigationGroup Current
        {
            get
            {
                return (navigationGroups != null && navigationGroups.Count > 0)
                    ? navigationGroups.Peek()
                    : null;
            }
        }

        private static void ReleaseTopNavigationGroup()
        {
            var prev = Current;
            while (!IsValidGroupAtTop())
            {
                navigationGroups.Pop();
            }

            Current?.SelectableGroup?.Focus();
            Current?.TransitionToFocusState();

            OnFocusChanged?.Invoke(prev, Current);
            NavigationController.NotifyAllAboutFocus(prev, Current);

            // local function
            bool IsValidGroupAtTop()
            {
                if (navigationGroups == null || navigationGroups.Count == 0)
                    return true;

                var top = navigationGroups.Peek();
                return top != null && top != prev && top.isActiveAndEnabled;
            }
        }

        private static void SetFocusedGroup(NavigationGroup group, NavigationGroupStacking stacking)
        {
            if (navigationGroups.Count > 0)
            {
                var cur = navigationGroups.Peek();
                if (cur == group)
                    return;

                if (cur != null)
                {
                    cur.selectableGroup.Unfocus();
                    cur.TransitionToUnfocusState();
                }

                switch (stacking)
                {
                    case NavigationGroupStacking.ForgetPrevious:
                        navigationGroups.Pop();
                        break;

                    case NavigationGroupStacking.ForgetAll:
                        navigationGroups.Clear();
                        break;

                    case NavigationGroupStacking.RememberPrevious:
                    default:
                        break;
                }
            }


            var prev = Current;
            navigationGroups.Push(group);
            var chooser = TakeSelectableChooser();
            group.selectableGroup.Focus(chooser);

            OnFocusChanged?.Invoke(prev, Current);
            NavigationController.NotifyAllAboutFocus(prev, Current);
        }

        internal static void SupplySelectableChooser(ISelectableChooser chooser, MonoBehaviour coroutiner)
        {
            if (selectableChooser == chooser)
                return;

            selectableChooser = chooser;
            coroutiner.StartCoroutine(WithdrawSelectableChooserInThreeFrames(chooser));
        }

        private static IEnumerator WithdrawSelectableChooserInThreeFrames(ISelectableChooser chooser)
        {
            yield return null;
            yield return null;
            yield return null;

            if (selectableChooser == chooser)
            {
                selectableChooser = null;
            }
        }

        internal static ISelectableChooser TakeSelectableChooser()
        {
            var chooser = selectableChooser;
            selectableChooser = null;
            return chooser;
        }

        internal static float LastDirtyTime { get; private set; }


        [SerializeField] FocusTime focusTime = FocusTime.OneFrameAfterOnEnable;
        [SerializeField] int focusPriority = 0;
        [SerializeField] CancelAction cancelAction;
        [SerializeField] Button cancelButton;
        [SerializeField] CancelEvent cancelEvent;
        [SerializeField] bool cancelWhenNoSelectablePresent = true;

        [SerializeField] bool excludeSubGroups = true;
        [SerializeField] SelectableCollection selectableGroup;

        [SerializeField, TransitionStates(TRANSITION_FOCUS, TRANSITION_UNFOCUS)]
        List<Transitions> focusTransitions = new List<Transitions>();

        ElementCollection<Selectable> IElementCollectionContainer<Selectable>.ElementCollection { get { return selectableGroup; } }
        public SelectableCollection SelectableGroup { get { return selectableGroup; } }

        public FocusTime FocusTiming { get { return focusTime; } set { focusTime = value; } }
        public int FocusPriority { get { return focusPriority; } set { focusPriority = value; } }
        public CancelAction CancelAction { get { return cancelAction; } }

        public bool IsFocused => Current == this;


        protected override void Awake()
        {
            selectableGroup?.Initialize(this);
            base.Awake();
        }

        protected override void OnEnable()
        {
            LastDirtyTime = Time.realtimeSinceStartup;
            TransitionToUnfocusState(instant: true);

            selectableGroup.CouldNotSelectElement += SelectableGroupCouldNotSelectElement;

            switch (focusTime)
            {
                case FocusTime.OnEnable:
                    Focus();
                    break;

                case FocusTime.OneFrameAfterOnEnable:
                    FocusDelayed();
                    break;

                case FocusTime.DoNotFocusAutomatically:
                    break;

                default:
                    throw new NotImplementedException();
            }
        }


        protected override void OnDisable()
        {
            LastDirtyTime = Time.realtimeSinceStartup;
            selectableGroup.CouldNotSelectElement -= SelectableGroupCouldNotSelectElement;
            Unfocus();
        }

        private void SelectableGroupCouldNotSelectElement()
        {
            if (!cancelWhenNoSelectablePresent || CancelAction == CancelAction.None)
                return;

            TriggerCancelAction();
        }

        public void TriggerCancelAction()
        {
            switch (cancelAction)
            {
                // None -> return early
                case CancelAction.None:
                    return;

                case CancelAction.Unfocus:
                    Unfocus();
                    break;

                case CancelAction.DeactivateGameObject:
                    this.gameObject.SetActive(false);
                    break;

                case CancelAction.DestroyGameObject:
                    Destroy(this.gameObject);
                    break;

                case CancelAction.TriggerButtonClick:
                    if (cancelButton == null)
                    {
                        Debug.LogError($"Navigation Group '{name}': Cannot trigger cancel button because it is not assigned or it is destroyed.");
                        break;
                    }

                    cancelButton.onClick.Invoke();
                    break;

                case CancelAction.TriggerCustomEvent:
                    cancelEvent.Invoke();
                    break;

                default:
                    throw new NotImplementedException();
            }

            BetterNavigation.Current.NotifyUsedCancelAction();
        }

        public void Focus()
        {
            Focus(NavigationGroupStacking.RememberPrevious);
        }

        public void Focus(NavigationGroupStacking stacking, bool force = false)
        {
            if (IsFocused)
                return;

            if (force || Current == null || Current.focusPriority <= this.focusPriority)
            {
                SetFocusedGroup(this, stacking);
                TransitionToFocusState();
            }
        }

        public void FocusDelayed(YieldInstruction delay = null,
            NavigationGroupStacking stacking = NavigationGroupStacking.RememberPrevious)
        {
            StopAllCoroutines();
            StartCoroutine(FocusDelayedCoroutine(delay, stacking));
        }

        IEnumerator FocusDelayedCoroutine(YieldInstruction delay, NavigationGroupStacking stacking)
        {
            yield return delay;
            Focus(stacking);
        }

        public void Unfocus()
        {
            if (Current == this)
            {
                selectableGroup.Unfocus();
                TransitionToUnfocusState();
                ReleaseTopNavigationGroup();
            }
        }

        internal void ReCollectSelectables(bool force = true, bool checkForLostElement = true)
        {
            selectableGroup.CollectElements(force, checkForLostElement);
        }

        public void TransitionToFocusState(bool instant = false)
            => DoStateTransition(TRANSITION_FOCUS, instant);

        public void TransitionToUnfocusState(bool instant = false)
            => DoStateTransition(TRANSITION_UNFOCUS, instant);

        private void DoStateTransition(string state, bool instant)
        {
            if (!this.gameObject.activeInHierarchy || !this.enabled)
                return;

            foreach (var info in focusTransitions)
            {
                info.SetState(state, instant);
            }
        }

        Rect IElementCollectionContainer<Selectable>.GetRectOnScreen()
        {
            return (transform as RectTransform).ToScreenRect();
        }

        void IElementCollectionContainer<Selectable>.CollectElements(List<Selectable> resultList)
        {
            // in case of one-time-collection-strategy, include inactive, so they are considered when they become active.
            // For "when dirty" we don't need to do this, as the selectables become dirty when a collectable becomes active.
            bool includeInactive = selectableGroup.CollectingStrategy == CollectingElementsStrategy.CollectOnInitialization;

            // Performance notice:
            // The custom implementation for collecting all selectables that are not part of a sub-group
            // is about 10 times slower than "GetComponentsInChildren" (which doesn't care about sub-groups).
            // (except for the very fist time where GetComponentsInChildren is significantly slower)
            // However, it takes still just a fraction of a millisecond, so it shouldn't cause a performance problem.
            if (excludeSubGroups)
            {
                resultList.Clear();
                NavigationHelper.CollectSelectablesBeneath(transform, resultList, false, includeInactive);
            }
            else
            {
                GetComponentsInChildren(includeInactive, result: resultList);
            }
        }

        IElementChooser<Selectable> IElementChooserProvider<Selectable>.TakeElementChooser()
        {
            return TakeSelectableChooser();
        }

    }
}

#pragma warning restore 0649 // never assigned warning
