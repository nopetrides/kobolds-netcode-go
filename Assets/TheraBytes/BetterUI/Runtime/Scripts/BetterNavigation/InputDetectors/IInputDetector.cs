using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{
    public interface IInputDetector
    {
        NavigationInfo CurrentNavigationInfo { get; }
        MoveDirection NavigationDirection { get; }
        float InitialRepeatDelay { get; }
        float ConsecutiveRepeatDelay { get; }
        BaseInputModule InputModule { get; }
        bool LastInputWasGamepad { get; }

        void UpdateCurrentNavigationData();
    }
}
