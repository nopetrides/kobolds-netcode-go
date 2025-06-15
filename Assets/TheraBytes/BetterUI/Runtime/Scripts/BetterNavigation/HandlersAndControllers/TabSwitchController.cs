using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using IAT = TheraBytes.BetterUi.InputActionType;

#pragma warning disable 0649 // never assigned warning

namespace TheraBytes.BetterUi
{
    [HelpURL("https://documentation.therabytes.de/better-ui/TabSwitchController.html")]
    public class TabSwitchController : DefaultMoveNavigationControllerBase, 
        IElementCollectionContainer<Selectable>, IElementCollectionContainer<NavigationGroup>, IResolutionDependency
    {
        #region Nested types 

        [SerializeField]
        [Serializable]
        public class Settings : IScreenConfigConnection
        {
            [SerializeField] IAT navigateLeft = IAT.SwitchLeft | IAT.Began | IAT.Repeated;
            [SerializeField] IAT navigateRight = IAT.SwitchRight | IAT.Began | IAT.Repeated;
            [SerializeField] IAT navigateDown = IAT.None;
            [SerializeField] IAT navigateUp = IAT.None;

            [SerializeField] string screenConfigName;
            public string ScreenConfigName { get { return screenConfigName; } set { screenConfigName = value; } }

            public IAT NavigateLeft { get { return navigateLeft; } }
            public IAT NavigateRight { get { return navigateRight; } }
            public IAT NavigateDown { get { return navigateDown; } }
            public IAT NavigateUp { get { return navigateUp; } }
        }

        [Serializable]
        public class SettingsConfigCollection : SizeConfigCollection<Settings> { }

        [Serializable]
        public class SelectableChangedArgs
        {
            public Selectable PrevousSelectable { get; }
            public Selectable CurrentSelectable { get; }

            public SelectableChangedArgs(Selectable previous, Selectable current)
            {
                this.PrevousSelectable = previous;
                this.CurrentSelectable = current;
            }
        }

        [Serializable]
        public class SelectableChangedEvent : UnityEvent<SelectableChangedArgs> { }

        [Flags]
        public enum SelectableFocusMode
        {
            Trigger = 1 << 0,
            Select = 1 << 1,
            TransitionToSelectedState = 1 << 2,
        }

        #endregion

        [SerializeField] Settings settingsFallback = new Settings();
        [SerializeField] SettingsConfigCollection customSettings = new SettingsConfigCollection();

        [SerializeField] SelectableFocusMode focusMode = SelectableFocusMode.Trigger;

        [SerializeField] SelectableCollection tabs;
        [SerializeField] SelectableChangedEvent currentTabChanged;

        [SerializeField] RectTransform controlledNavigationGroupsParent;
        [SerializeField] NavigationGroupCollection controlledNavigationGroups;

        Selectable currentTab;
        Settings currentSettings;

        public Settings CurrentSettings { get { return currentSettings; } }

        ElementCollection<Selectable> IElementCollectionContainer<Selectable>.ElementCollection { get { return tabs; } }
        ElementCollection<NavigationGroup> IElementCollectionContainer<NavigationGroup>.ElementCollection 
            { get { return controlledNavigationGroups; } }

        public SelectableCollection Tabs { get { return tabs; } }
        public Selectable CurrentTab { get { return currentTab; } }
        public SelectableChangedEvent CurrentTabChanged { get { return currentTabChanged; } }
        public override NavigationGroupCollection ControlledNavigationGroups { get { return controlledNavigationGroups; } }

        public RectTransform ControlledNavigationGroupParent
        {
            get { return controlledNavigationGroupsParent; }
            set
            {
                if (controlledNavigationGroupsParent == value)
                    return;

                controlledNavigationGroupsParent = value;

                if (controlledNavigationGroups.IsInitialized)
                {
                    controlledNavigationGroups.CollectElements(force: true);
                }
                else
                {
                    controlledNavigationGroups.Initialize(this);
                }
            }
        }

        public override IAT NavigateLeftInput { get { return CurrentSettings.NavigateLeft; } }
        public override IAT NavigateRightInput { get { return CurrentSettings.NavigateRight; } }
        public override IAT NavigateUpInput { get { return CurrentSettings.NavigateUp; } }
        public override IAT NavigateDownInput { get { return CurrentSettings.NavigateDown; } }

        public override bool IsInitialized { get { return base.IsInitialized && tabs.IsInitialized; } }

        protected override void OnEnable()
        {
            UpdateCurrentSettings();
            base.OnEnable();
        }

        protected override IEnumerator InitializationRoutine()
        {
            var baseEnumerator = base.InitializationRoutine();
            while (baseEnumerator.MoveNext())
            {
                yield return baseEnumerator.Current;
            }

            tabs.Initialize(this);
            controlledNavigationGroups.Initialize(this);

            var tab = currentTab;
            if (tab != null) // current tab may be set programmatically already
            {
                // HACK: When a tab is clicked with the mouse, all navigation groups might be disabled
                // causing the Tab Switch Controller to disable only to enable again when the tab shows the content.
                // But at that time, the newly clicked tab couldn't become the current Tab,
                // so we check what's the actual current tab here.
                tab = tabs.Elements.OfType<Toggle>().FirstOrDefault(o => o.isOn) ?? currentTab;
            }
            else
            {
                tabs.GetInitialElement();
            }

            bool hasNoTabOnPurpose = tabs.InitialFocus == SelectionOnFocus.KeepPreviousSelection
                || (tabs.InitialFocus == SelectionOnFocus.Specific && tabs.InitialElement == null);

            while(tab == null && !hasNoTabOnPurpose)
            {
                yield return null;
                tabs.CollectElements(force: true);
                tab = tabs.GetInitialElement();
            }

            controlledNavigationGroups.Initialize(this);
            SetCurrentTab(tab, true, true, false);
        }

        public void SetCurrentTab(Selectable selectable, 
            bool executeFocusLogic = true, bool triggerEvent = true, bool shouldSmartMove = false)
        {
            Debug.Assert(tabs.Contains(selectable),
                "BetterUi.TabSwitchController.SetCurrentTab(): The provided selectable is not one of the tabs. This may cause unexpected behavior.");

            if (selectable == currentTab)
            {
                var tgl = selectable as Toggle;
                if (tgl == null || tgl.isOn) // if toggle is not on, do the logic below this section
                {
                    EnsureNavigationGroupFocus();
                    return;
                }
            }

            var prev = currentTab;
            currentTab = selectable;

            if (executeFocusLogic)
            {
                SelectOrTrigger(prev, selectable, shouldSmartMove);
                EnsureNavigationGroupFocus();
            }

            if(triggerEvent)
            {
                CurrentTabChanged?.Invoke(new SelectableChangedArgs(prev, currentTab));
            }
        }

        private void SelectOrTrigger(Selectable prev, Selectable cur, bool shouldSmartMove)
        {
            if(prev != null 
                && focusMode.HasFlag(SelectableFocusMode.TransitionToSelectedState)
                && !focusMode.HasFlag(SelectableFocusMode.Select))
            {
                prev.OnDeselect(NavigationHelper.GetBaseEventData());
            }

            if (focusMode.HasFlag(SelectableFocusMode.Select))
            {
                cur.Select();
            }
            else if(focusMode.HasFlag(SelectableFocusMode.TransitionToSelectedState))
            {
                cur.OnSelect(NavigationHelper.GetBaseEventData());
            }

            if (focusMode.HasFlag(SelectableFocusMode.Trigger))
            {
                SupplySelectableChooser(shouldSmartMove);

                if (cur is Toggle tgl)
                {
                    tgl.SetIsOn(true);
                }
                else if (cur is Button btn)
                {
                    btn.onClick.Invoke();
                }
                else
                {
                    Debug.LogWarning($"Selectable '{cur?.name}' of type '{cur?.GetType().Name}' cannot be triggered.");
                }
            }
        }

        protected override void NotifyButtonInteraction(NavigationInfo navigationInfo, bool shouldSmartMove, bool forceProvidedDirection = false)
        {
            if (currentTab == null || !IsInitialized)
                return;

            if (IsControllingNavigationGroups && !controlledNavigationGroups.Contains(NavigationGroup.Current))
                return;

            MoveDirection direction = (forceProvidedDirection)
                ? navigationInfo.Direction
                : base.GetMatchingMoveDirection(navigationInfo);

            if (direction == MoveDirection.None)
                return;

            // Find selectable in direction
            var sel = NavigationHelper.FindSelectableInDirection(currentTab, direction, tabs);
            if (sel == null)
                return;

            PrepareSwitchingContext(direction, BetterNavigation.LastSelection, shouldSmartMove);
            SetCurrentTab(sel, true, true, shouldSmartMove);
        }

        void IResolutionDependency.OnResolutionChanged()
        {
            UpdateCurrentSettings();
        }

        void UpdateCurrentSettings()
        {
            if (settingsFallback == null)
                return;

            var prev = currentSettings;
            currentSettings = customSettings.GetCurrentItem(settingsFallback);
            if(prev == currentSettings) 
                return;

            base.MapVisualizations();
        }

        void IElementCollectionContainer<Selectable>.CollectElements(List<Selectable> resultList)
        {
            GetComponentsInChildren(includeInactive: false, result: resultList);
        }

        Rect IElementCollectionContainer<Selectable>.GetRectOnScreen()
        {
            return (transform as RectTransform).ToScreenRect();
        }

        void IElementCollectionContainer<NavigationGroup>.CollectElements(List<NavigationGroup> resultList)
        {
            Transform tr = (controlledNavigationGroupsParent != null) ? controlledNavigationGroupsParent : transform;
            tr.GetComponentsInChildren(includeInactive: true, result: resultList);
        }

        Rect IElementCollectionContainer<NavigationGroup>.GetRectOnScreen()
        {
            RectTransform rt = (controlledNavigationGroupsParent != null) ? controlledNavigationGroupsParent : transform as RectTransform;
            return rt.ToScreenRect();
        }
    }
}

#pragma warning restore 0649 // never assigned warning
