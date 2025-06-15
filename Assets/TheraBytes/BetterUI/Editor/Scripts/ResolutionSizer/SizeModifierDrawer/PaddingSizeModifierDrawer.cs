using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;

namespace TheraBytes.BetterUi.Editor
{
    [CustomPropertyDrawer(typeof(PaddingSizeModifier))]
    public class PaddingSizeModifierDrawer : MarginOrPaddingSizeModifierDrawer<Padding, float>
    {
    }
}
