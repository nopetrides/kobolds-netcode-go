using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
    public static class EditorGuiUtils
    {

        static readonly GUIContent visualizationContent = new GUIContent("Visualization", "Assign a custom component here that derives from `BaseInputActionVisualization`. Note that there is no default implementation for this component as it highly depends on how you handle input in your game.");


        public const string ObsoleteMessage = "This method is obsolete.You most probably need to update the Better TextMesh Pro package. Please check the upgrade guide for more information: https://documentation.therabytes.de/better-ui/UpgradeGuide.html ";

        public static void DrawOldMethodCallWarning()
        {
            EditorGUILayout.HelpBox(@"Calling an old method. You probably need to update the Better TextMesh Pro package.

Please install the right 'BetterUI_TextMeshPro' package found at 'Assets/TheraBytes/BetterUI/Packages' (probably you need the package that ends with '_EditorPanelUI').

For more information, please read the upgrade guide.", MessageType.Warning);


            if (GUILayout.Button("Open Upgrade Guide", "minibutton"))
            {
                Application.OpenURL("https://documentation.therabytes.de/better-ui/UpgradeGuide.html");
            }

            EditorGUILayout.Space();
        }

        public static void DrawInputActionField(SerializedProperty inputActionField)
        {
            EditorGUILayout.BeginHorizontal();
            DrawEnumField(inputActionField, string.Empty, true, false,
                new[] { InputActionType.NavigateInAnyDirection, InputActionType.Submit, InputActionType.ButtonX, InputActionType.Began },
                new[] { InputActionType.PointerPositionChanged },
                (val) =>
                {
                    bool actionChanged = val < (int)InputActionType.Began;
                    if (!actionChanged)
                        return true;

                    int cur = inputActionField.intValue;
                    bool added = (cur & val) == 0;
                    if (!added)
                        return false;

                    int timings = (int)(InputActionType.Began | InputActionType.Repeated | InputActionType.Ended);
                    var t = timings & cur;
                    inputActionField.intValue = val | t;

                    inputActionField.serializedObject.ApplyModifiedProperties();
                    return false;
                });

            if (inputActionField.intValue != 0 && GUILayout.Button("x", GUILayout.Width(20)))
            {
                inputActionField.intValue = 0;
                inputActionField.serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.EndHorizontal();
        }

        public static void DrawFlagsEnumField<T>(SerializedProperty enumProperty, params T[] prependedSeparators)
            where T : Enum
        {
            DrawFlagsEnumField<T>(enumProperty, string.Empty, prependedSeparators);
        }

        public static void DrawFlagsEnumField<T>(SerializedProperty enumProperty, string tooltip, params T[] prependedSeparators)
            where T : Enum
        {
            DrawEnumField<T>(enumProperty, tooltip, true, true, prependedSeparators, null, null);
        }

        public static void DrawEnumField<T>(SerializedProperty enumProperty, params T[] prependedSeparators)
            where T : Enum
        {
            DrawEnumField<T>(enumProperty, string.Empty, prependedSeparators);
        }
        public static void DrawEnumField<T>(SerializedProperty enumProperty, string tooltip, params T[] prependedSeparators)
            where T : Enum
        {
            DrawEnumField<T>(enumProperty, tooltip, false, false, prependedSeparators, null, null);
        }

        public static void DrawEnumField<T>(SerializedProperty enumProperty, string tooltip, bool isFlagEnum, bool showNothingAndEverything, T[] prependedSeparators, T[] hiddenValues, Predicate<int> allowSelectingValue)
            where T : Enum
        {
            GetControlSubRects(out Rect rect, out Rect prefixRect, out Rect buttonRect);

            GUIContent label = new GUIContent(enumProperty.displayName, tooltip);
            EditorGUI.BeginProperty(rect, label, enumProperty);
            EditorGUI.PrefixLabel(prefixRect, label);

            T val = (T)(object)enumProperty.intValue;
            string valName = val.ToString();
            int everythingValue = 0;

            if (showNothingAndEverything)
            {
                foreach (var v in Enum.GetValues(typeof(T)))
                {
                    everythingValue |= (int)v;
                }

                valName = enumProperty.intValue == 0
                    ? "Nothing"
                    : enumProperty.intValue == everythingValue
                        ? "Everything"
                        : valName;
            }

            valName = ObjectNames.NicifyVariableName(valName.Replace(",", " |"));

            if (GUI.Button(buttonRect, valName, EditorStyles.popup))
            {
                GenericMenu menu = new GenericMenu();

                if (showNothingAndEverything)
                {
                    menu.AddItem(new GUIContent("Nothing"), enumProperty.intValue == 0, () =>
                    {
                        enumProperty.intValue = 0;
                        enumProperty.serializedObject.ApplyModifiedProperties();
                    });

                    menu.AddItem(new GUIContent("Everything"), enumProperty.intValue == everythingValue, () =>
                    {
                        enumProperty.intValue = everythingValue;
                        enumProperty.serializedObject.ApplyModifiedProperties();
                    });

                    menu.AddSeparator("");
                }

                foreach (var v in Enum.GetValues(typeof(T)).OfType<T>())
                {
                    int valueAsInt = (int)(object)v;
                    if (isFlagEnum && showNothingAndEverything && valueAsInt == 0)
                        continue;

                    if (hiddenValues != null && hiddenValues.Contains(v))
                        continue;

                    if (prependedSeparators != null && prependedSeparators.Contains(v))
                    {
                        menu.AddSeparator("");
                    }

                    bool isChecked = (isFlagEnum)
                        ? val.HasFlag(v) && (valueAsInt != 0 || enumProperty.intValue == 0)
                        : val.Equals(v);

                    menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(v.ToString())), isChecked, 
                        () =>
                        {
                            if (allowSelectingValue != null && !allowSelectingValue(valueAsInt))
                                return;

                            enumProperty.intValue = (isFlagEnum)
                                ? enumProperty.intValue ^ valueAsInt
                                : valueAsInt;

                            enumProperty.serializedObject.ApplyModifiedProperties();
                        });
                }

                menu.DropDown(buttonRect);
            }

            EditorGUI.EndProperty();
        }

        public static void DrawPopup(GUIContent label, int index, GUIContent[] options, Action<int> indexChanged, Func<string, string> selectionNameConvertMethod, params string[] prependedSeparators)
        {
            GetControlSubRects(out Rect rect, out Rect prefixRect, out Rect buttonRect);

            EditorGUI.PrefixLabel(prefixRect, label);

            string selectionName = selectionNameConvertMethod(options[index].text);

            if (GUI.Button(buttonRect, selectionName, EditorStyles.popup))
            {
                GenericMenu menu = new GenericMenu();
                for (int i = 0; i < options.Length; i++)
                {
                    var o = options[i];
                    if (prependedSeparators != null && prependedSeparators.Contains(o.text))
                    {
                        menu.AddSeparator(System.IO.Path.GetDirectoryName(o.text).Replace('\\', '/').TrimEnd('/'));
                    }

                    int currentIndex = i;
                    menu.AddItem(o, i == index, () =>
                    {
                        indexChanged(currentIndex);
                    });
                }

                menu.DropDown(buttonRect);
            }
        }

        public static void GetControlSubRects(out Rect completeRect, out Rect labelRect, out Rect interactionRect)
        {
            completeRect = EditorGUILayout.GetControlRect();

            labelRect = completeRect;
            labelRect.width = EditorGUIUtility.labelWidth;

            interactionRect = completeRect;
            interactionRect.x += labelRect.width;
            interactionRect.width -= labelRect.width;
        }


        public static void DrawInputActionWithVisualization(SerializedProperty navigation, SerializedProperty visualization)
        {
            DrawInputActionField(navigation);
            if (navigation.intValue != 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(visualization, visualizationContent);
                if(GUILayout.Button("?", GUILayout.Width(20)))
                {
                    Application.OpenURL("https://documentation.therabytes.de/better-ui/InputActionVisualization.html");
                }

                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
        }


        #region Backwards Compatibility Methods
        [Obsolete(ObsoleteMessage)]
        public static void DrawLayoutList<T>(string listTitle,
            List<T> list, SerializedProperty listProp, ref int count, ref bool foldout,
            Action<SerializedProperty> createCallback, Action<T, SerializedProperty> drawItemCallback)
        {
            DrawOldMethodCallWarning();
        }

        [Obsolete(ObsoleteMessage)]
        public static void DrawLayoutList<T>(string listTitle,
            List<T> list, SerializedProperty listProp, ref int count,
            Action<SerializedProperty> createCallback, Action<T, SerializedProperty> drawItemCallback)
        {
            DrawOldMethodCallWarning();
        }

        [Obsolete(ObsoleteMessage)]
        public static void DrawLayoutList<T>(string listTitle, bool usingFoldout,
            List<T> list, SerializedProperty listProp, ref int count, ref bool foldout,
            Action<SerializedProperty> createCallback, Action<T, SerializedProperty> drawItemCallback)
        {
            DrawOldMethodCallWarning();
        }

        [Obsolete(ObsoleteMessage)]
        public static void DrawTransitions(string title,
            List<Transitions> transitions, SerializedProperty transitionsProp, ref int count,
            params string[] stateNames)
        {
            DrawOldMethodCallWarning();
        }
        #endregion
    }
}
