using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
    [CustomPropertyDrawer(typeof(Vector2IntSizeModifier))]
    public class Vector2IntSizeModifierDrawer : ScreenDependentSizeDrawer<Vector2Int>
    {
        protected override void DrawModifiers(SerializedProperty property)
        {
            var modx = property.FindPropertyRelative("ModX");
            DrawModifierList(modx, "X Modification");

            var mody = property.FindPropertyRelative("ModY");
            DrawModifierList(mody, "Y Modification");
        }

        protected override string GetValueString(Vector2Int obj)
        {
            return string.Format("({0}, {1})", obj.x, obj.y);
        }
    }
}
