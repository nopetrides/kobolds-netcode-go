using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheraBytes.BetterUi
{
    [Serializable]
    public class NavigationGroupCollection : ElementCollection<NavigationGroup>
    {
        float lastDirtyResolveTime = -1;
        public override bool IsDirty => lastDirtyResolveTime < NavigationGroup.LastDirtyTime;
        protected override void SelectElement(NavigationGroup group)
        {
            group.Focus();
        }

        protected override NavigationGroup FindClosestElementTo(Vector2 screenCoord, IEnumerable<NavigationGroup> elements)
        {
            return NavigationHelper.FindClosestElementTo(screenCoord, elements, null);
        }

        protected override NavigationGroup GetCurrentElement()
        {
            return NavigationGroup.Current;
        }

        protected override void ResolveDirtyState()
        {
            lastDirtyResolveTime = Time.realtimeSinceStartup;
        }

        protected override bool Predicate(NavigationGroup element)
        {
            return element != null && element.isActiveAndEnabled;
        }

        protected override NavigationGroup GetElementWithHighestPriority()
        {
            int prio = int.MinValue;
            NavigationGroup highest = null;

            foreach(var element in elements)
            {
                if (!Predicate(element))
                    continue;

                if (element.FocusPriority <= prio)
                    continue;

                prio = element.FocusPriority;
                highest = element;
            }

            return highest;
        }
    }
}
