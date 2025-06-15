using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{
    public abstract class NavigationController : ButtonInteractionHandler
    {
        static ObjectCollectionTracker<NavigationController> activeControllers 
            = new ObjectCollectionTracker<NavigationController>();

        public static ObjectCollectionTracker<NavigationController> ActiveControllers { get { return activeControllers; } }

        static List<NavigationController> allControllers
            = new List<NavigationController>();
        public static List<NavigationController> AllControllers { get { return allControllers; } }

        internal static void NotifyActiveAboutMove(MoveDirection moveDirection, Selectable current, bool movingOutOfGroup)
        {
            foreach (var controller in activeControllers.CleanUpIterator())
            {
                controller.NotifyMove(moveDirection, current, movingOutOfGroup);
            }
        }

        internal static void NotifyAllAboutFocus(NavigationGroup previousGroup, NavigationGroup focusedGroup)
        {
            foreach(var controller in allControllers)
            {
                Debug.Assert(controller != null, $"A {nameof(NavigationController)} is null. This should never happen.");
                controller.NotifyFocusChanged(previousGroup, focusedGroup);
            }
        }


        [SerializeField] bool isControllingNavigationGroups = true;
        [SerializeField] bool unfocusNavigationGroupOnDisable = true;
        [SerializeField] bool disableWhenNoManagedGroupActive = true;
        [SerializeField] bool enableWhenManagedGroupActive = true;

        [SerializeField] bool initializeOneFrameDelayed = false;

        Coroutine initRoutine;
        Coroutine ensureNavigationGroupCoroutine;
        bool isInitialized;
        public abstract NavigationGroupCollection ControlledNavigationGroups { get; }
        public bool UnfocusNavigationGroupOnDisable 
        { 
            get { return unfocusNavigationGroupOnDisable; } 
            set { unfocusNavigationGroupOnDisable = value; } 
        }

        public virtual bool IsInitialized 
        { 
            get { return (!IsControllingNavigationGroups || ControlledNavigationGroups.IsInitialized) && isInitialized; } 
        }

        public bool IsControllingNavigationGroups { get { return isControllingNavigationGroups; } }

        protected override void Awake()
        {
            allControllers.Add(this);
            base.Awake();
        }

        protected override void OnDestroy()
        {
            allControllers.Remove(this);
            base.OnDestroy();
        }

        protected override void OnEnable()
        {
            activeControllers.Add(this);

            StartCoroutine(InternalInitializationCoroutine());
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            StopAllCoroutines();
            activeControllers.Remove(this);

            if(unfocusNavigationGroupOnDisable && IsControllingNavigationGroups)
            {
                if(this.ControlledNavigationGroups.Contains(NavigationGroup.Current, collectElements: false))
                {
                    if (NavigationGroup.Current.SelectableGroup.Contains(BetterNavigation.LastSelection))
                    {
                        BetterNavigation.Current?.DeselectCurrentSelection();
                    }

                    NavigationGroup.Current?.Unfocus();
                }
            }

            base.OnDisable();
        }

        protected virtual void NotifyFocusChanged(NavigationGroup previousGroup, NavigationGroup focusedGroup)
        {
            if (!IsControllingNavigationGroups)
                return;

            if(enableWhenManagedGroupActive && ControlledNavigationGroups.Contains(focusedGroup))
            {
                this.enabled = true;
            }
            else if(disableWhenNoManagedGroupActive && ControlledNavigationGroups.Contains(previousGroup))
            {
                this.enabled = false;
            }
        }

        IEnumerator InternalInitializationCoroutine()
        {
            initRoutine = StartCoroutine(InitializationRoutine());
            yield return initRoutine;
            yield return EnsureNavigationGroupFocus();
            isInitialized = true;
        }

        protected Coroutine EnsureNavigationGroupFocus()
        {
            if(!IsControllingNavigationGroups)
                return null;

            if (ensureNavigationGroupCoroutine != null)
            {
                StopCoroutine(ensureNavigationGroupCoroutine);
            }

            if (ControlledNavigationGroups.Contains(NavigationGroup.Current, collectElements: false))
            {
                OnInitialNavigationGroupSelected(NavigationGroup.Current);
                return null;
            }

            ensureNavigationGroupCoroutine = StartCoroutine(EnsureNavigationGroupFocusCoroutine());
            return ensureNavigationGroupCoroutine;
        }

        private IEnumerator EnsureNavigationGroupFocusCoroutine()
        {
            int maxIterationsLeft = 3; // We don't want to search forever

            while (maxIterationsLeft > 0)
            {
                var group = ControlledNavigationGroups.SelectInitialElement();

                bool isValidGroup = group == NavigationGroup.Current
                    && group != null
                    && group.isActiveAndEnabled
                    && ControlledNavigationGroups.Contains(group, collectElements: false);

                if (isValidGroup)
                {
                    OnInitialNavigationGroupSelected(group);
                    break;
                }

                yield return null;
                maxIterationsLeft--;
            }
        }

        protected virtual IEnumerator InitializationRoutine()
        {
            if (initializeOneFrameDelayed)
                yield return null;
        }

        protected virtual void OnInitialNavigationGroupSelected(NavigationGroup group) { }

        protected abstract void NotifyMove(MoveDirection moveDirection, Selectable current, bool movingOutOfGroup);
    }
}
