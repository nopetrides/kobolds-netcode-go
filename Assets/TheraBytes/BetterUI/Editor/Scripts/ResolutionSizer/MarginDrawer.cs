using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi
{
    [CustomPropertyDrawer(typeof(Margin))]
    public class MarginDrawer : PropertyDrawer
    {
        bool foldout;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty left = property.FindPropertyRelative("left");
            EditorGUILayout.PropertyField(left);
            SerializedProperty right = property.FindPropertyRelative("right");
            EditorGUILayout.PropertyField(right);
            SerializedProperty top = property.FindPropertyRelative("top");
            EditorGUILayout.PropertyField(top);
            SerializedProperty bottom = property.FindPropertyRelative("bottom");
            EditorGUILayout.PropertyField(bottom);
        }

    }
}
