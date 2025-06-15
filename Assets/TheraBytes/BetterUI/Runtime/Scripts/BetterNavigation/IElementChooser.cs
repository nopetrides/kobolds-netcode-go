using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{
    public interface ISelectableChooser : IElementChooser<Selectable>
    {
        NavigationController NavigationController { get; }
        Vector2 PreviousSelectableScreenPosition { get; }
    }

    public interface IElementChooser<T>
    {
        T ChooseFrom(IEnumerable<T> options, T fallback);
    }
}
