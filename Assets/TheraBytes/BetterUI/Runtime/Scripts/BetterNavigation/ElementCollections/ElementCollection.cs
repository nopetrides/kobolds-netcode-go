using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{
    [Serializable]
    public abstract class ElementCollection<T> 
        where T : MonoBehaviour
    {
        [SerializeField] protected bool rememberPrevious = true;
        [SerializeField] protected SelectionOnFocus initialFocused;
        [SerializeField] protected T initialElement;
        [SerializeField] protected Vector2 relativeCoordinate;
        [SerializeField] protected CollectingElementsStrategy collectingStrategy = CollectingElementsStrategy.CollectWhenDirty;

        // we need the elements for this group two times:
        // - the list for collection logic and initial selection (in case of "First" or "Last in Hierarchy")
        // - the hashset to check `Contains(element)` more efficiently
        [SerializeField] protected List<T> elements;
        HashSet<T> elementsLookup;


        protected IElementCollectionContainer<T> container;
        protected T currentElement; // current element is not required to be part of this collection
        protected T previousElement; // previous element is an element which is part of this collection
        protected Vector2 previousElementPosition;

        public event Action CouldNotSelectElement;
        public IEnumerable<T> Elements
        {
            get
            {
                CollectElements(checkForLostElement: IsInitialized);
                return elements.Where(Predicate);
            }
        }

        /// <summary>
        /// Returns the elements without collecting them before.
        /// </summary>
        public IEnumerable<T> LastElements { get { return elements.Where(Predicate); } }

        public SelectionOnFocus InitialFocus { get { return initialFocused; } set { initialFocused = value; } }
        public T InitialElement { get { return initialElement; } set { initialElement = value; } }

        public IElementCollectionContainer<T> Container { get { return container; } }
        public CollectingElementsStrategy CollectingStrategy { get { return collectingStrategy; } }

        public abstract bool IsDirty { get; }

        public bool IsInitialized { get { return container != null; } }

        protected ElementCollection() { }

        protected ElementCollection(CollectingElementsStrategy collectingStrategy)
        {
            this.collectingStrategy = collectingStrategy;
        }

        internal void Initialize(IElementCollectionContainer<T> container)
        {
            if (IsInitialized)
                return;

            this.container = container;
            CollectElements(force: true, checkForLostElement: false);
        }
        internal void UnInitialize()
        {
            this.container = null;
            elements = null;
            elementsLookup = null;
        }

        internal void Focus(IElementChooser<T> chooser = null)
        {
            if (chooser == null)
            {
                SelectInitialElement();
            }
            else
            {
                CollectElements(checkForLostElement: false);
                T fallback = GetInitialElement();
                T element = chooser.ChooseFrom(LastElements, fallback);
                TrySetSelectedElement(element);
            }

            OnFocus();
        }

        internal void Unfocus()
        {
            OnUnfocus();
        }

        protected virtual void OnFocus()
        {
            // nothing to do in default implementation
        }

        protected virtual void OnUnfocus()
        {
            // nothing to do in default implementation
        }

        public bool TrySetSelectedElement(T element)
        {
            if (element == null)
            {
                CouldNotSelectElement?.Invoke();
                return false;
            }

            if (!UpdateRememberedElement(element))
                return false;
            
            SelectElement(element);
            return true;
        }

        internal bool UpdateRememberedElement(T element)
        {
            currentElement = element;

            if (!Contains(element))
                return false;
            
            previousElement = element;
            previousElementPosition = (previousElement.transform as RectTransform).ToScreenRect().center;
            return true;
        }

        protected abstract void SelectElement(T element);
        protected abstract T FindClosestElementTo(Vector2 screenCoord, IEnumerable<T> elements);
        protected abstract T GetCurrentElement();
        protected abstract void ResolveDirtyState();
        protected abstract bool Predicate(T element);

        public bool IsValid(T element)
        {
            return Predicate(element);
        }

        public T SelectInitialElement()
        {
            T element = GetInitialElement();
            TrySetSelectedElement(element);

            return element;
        }

        public T GetInitialElement()
        {
            if (rememberPrevious && Predicate(previousElement))
                return previousElement;

            return DetermineInitialElement();
        }

        private T DetermineInitialElement()
        {
            switch (initialFocused)
            {
                case SelectionOnFocus.FirstInHierarchy:
                    CollectElements(checkForLostElement: false);
                    return elements.FirstOrDefault(Predicate);

                case SelectionOnFocus.LastInHierarchy:
                    CollectElements(checkForLostElement: false);
                    return elements.LastOrDefault(Predicate);

                case SelectionOnFocus.Specific:
                    return initialElement;

                case SelectionOnFocus.ClosestToCoordinate:
                    var rectOnScreen = container.GetRectOnScreen();
                    var screenCoord = new Vector2(
                        x: rectOnScreen.x + relativeCoordinate.x * rectOnScreen.width,
                        y: rectOnScreen.y + relativeCoordinate.y * rectOnScreen.height);

                    CollectElements(checkForLostElement: false);
                    return FindClosestElementTo(screenCoord, LastElements);

                case SelectionOnFocus.ClosestToPreviousSelection:
                    var prevSel = EventSystem.current?.currentSelectedGameObject;
                    Vector2 center = (prevSel != null)
                        ? (prevSel.transform as RectTransform).ToScreenRect().center
                        : 0.5f * ResolutionMonitor.CurrentResolution;

                    CollectElements(checkForLostElement: false);
                    return FindClosestElementTo(center, LastElements);

                case SelectionOnFocus.HighestPriority:
                    CollectElements(checkForLostElement: false);
                    return GetElementWithHighestPriority();
                    
                case SelectionOnFocus.KeepPreviousSelection:
                default:
                    return null;
            }
        }

        protected virtual T GetElementWithHighestPriority()
        {
             return elements.FirstOrDefault(Predicate);
        }

        public bool Contains(T element, bool collectElements = true)
        {
            if (collectElements || elementsLookup == null)
            {
                CollectElements(checkForLostElement: false);
            }

            return element != null 
                && elementsLookup.Contains(element);
        }

        internal void CollectElements(bool force = false, bool checkForLostElement = true)
        {
            bool shouldCollect = IsInitialized
                && (force || elements == null || elementsLookup == null
                || (collectingStrategy == CollectingElementsStrategy.CollectWhenDirty && IsDirty));

            T cur = GetCurrentElement();
            bool shouldReEvaluateCurrentElement = checkForLostElement 
                && (currentElement == null || cur != currentElement || !IsValid(cur));

            if (elements == null)
            {
                elements = new List<T>();
            }

            // for some reason, elementsLookup is sometimes null in the editor, although elements are not.
            if (elementsLookup == null)
            {
                elementsLookup = new HashSet<T>();
            }

            if (shouldCollect)
            {
                if (collectingStrategy != CollectingElementsStrategy.FixedSet)
                {
                    container.CollectElements(elements);
                }

                // by not using `new HashSet<T>(elements)`
                // we have a good chance to not allocate memory resulting in better performance.
                elementsLookup.Clear();
                elementsLookup.UnionWith(elements);

                ResolveDirtyState();
            }

            if(shouldReEvaluateCurrentElement)
            {
                if(cur != null && UpdateRememberedElement(cur))
                {
                    return;
                }
                else
                {
                    T element = FindElementAfterLosingIt();
                    TrySetSelectedElement(element);
                }
            }
        }

        T FindElementAfterLosingIt()
        {
            if (container is IElementChooserProvider<T> chooserProvider)
            {
                var chooser = chooserProvider.TakeElementChooser();
                if (chooser != null)
                {
                    T fallback = GetInitialElement();
                    return chooser.ChooseFrom(LastElements, fallback);
                }
            }
            
            if(previousElementPosition != default)
                return FindClosestElementTo(previousElementPosition, LastElements);
            
            return GetInitialElement();
        }

        internal bool ShouldEvaluateInitialElement()
        {
            if (rememberPrevious && previousElement != null)
            {
                return false;
            }

            switch (InitialFocus)
            {
                case SelectionOnFocus.FirstInHierarchy:
                case SelectionOnFocus.LastInHierarchy:
                case SelectionOnFocus.ClosestToCoordinate:
                case SelectionOnFocus.ClosestToPreviousSelection:
                case SelectionOnFocus.HighestPriority:
                    return true;

                case SelectionOnFocus.Specific:
                    return initialElement != null;

                case SelectionOnFocus.KeepPreviousSelection:
                    return false;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
