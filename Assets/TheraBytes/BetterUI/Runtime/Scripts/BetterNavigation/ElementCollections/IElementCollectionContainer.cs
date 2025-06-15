using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{
    public interface IElementChooserProvider<T>
    {
        IElementChooser<T> TakeElementChooser();
    }

    public interface IElementCollectionContainer<T>
        where T : MonoBehaviour
    {
        ElementCollection<T> ElementCollection { get; }
        void CollectElements(List<T> resultList);
        Rect GetRectOnScreen();

    }
}
