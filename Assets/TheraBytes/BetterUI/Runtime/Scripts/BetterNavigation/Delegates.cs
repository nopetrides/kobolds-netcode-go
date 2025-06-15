using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{
    public delegate void CurrentSelectableChanged(Selectable previousSelectable, Selectable currentSelectable);
    public delegate void NavigationGroupFocusChanged(NavigationGroup previousGroup, NavigationGroup currentGroup);
}
