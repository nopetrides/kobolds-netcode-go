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
    public class SelectableCollection : ElementCollection<Selectable>
    {
        public static bool SelectablePredicate(Selectable selectable)
        {
            return selectable != null
                && selectable.interactable
                && selectable.navigation.mode != Navigation.Mode.None
                && selectable.isActiveAndEnabled;
        }

        float lastDirtyResolveTime = -1;
        public override bool IsDirty => lastDirtyResolveTime < BetterNavigation.LastDirtyTime;

        public SelectableCollection() : base() 
        { }

        public SelectableCollection(CollectingElementsStrategy collectingStrategy) 
            : base(collectingStrategy)
        { }

        protected override void SelectElement(Selectable selectable)
        {
            selectable.Select();

            // call OnSelect() manually as the method might not be triggered
            // if the selectable wasn't registered to the ExecuteEvents yet.
            selectable.OnSelect(NavigationHelper.GetBaseEventData());
        }

        protected override Selectable FindClosestElementTo(Vector2 screenCoord, IEnumerable<Selectable> elements)
        {
            return NavigationHelper.FindClosestSelectable(screenCoord, elements);
        }

        protected override Selectable GetCurrentElement()
        {
            return BetterNavigation.LastSelection;
        }

        protected override void ResolveDirtyState()
        {
            lastDirtyResolveTime = Time.realtimeSinceStartup;
        }

        protected override bool Predicate(Selectable element)
        {
            return SelectablePredicate(element);
        }

        protected override void OnUnfocus()
        {
            if(this.Contains(BetterNavigation.LastSelection, false) && BetterNavigation.Current != null)
            {
                BetterNavigation.Current.DeselectCurrentSelection();
            }
        }
    }
}
