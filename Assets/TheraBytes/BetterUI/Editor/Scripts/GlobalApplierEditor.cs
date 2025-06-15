using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
    [CustomEditor(typeof(GlobalApplier))]
    public class GlobalApplierEditor : UnityEditor.Editor
    {
        SerializedProperty onButtonClicked;
        SerializedProperty onToggleChanged;

        List<ReorderableList> listOverrides;

        bool foldoutEvents = true;
        bool foldoutOverrides = false;
        
        void OnEnable()
        {
            onButtonClicked = serializedObject.FindProperty("onButtonClicked");
            onToggleChanged = serializedObject.FindProperty("onToggleChanged");

            listOverrides = new List<ReorderableList>()
            {
                CreateOverrideListEditor<float>(serializedObject.FindProperty("floatOverrides")),
                CreateOverrideListEditor<Vector2Int>(serializedObject.FindProperty("vector2IntOverrides")),
                CreateOverrideListEditor<Vector2>(serializedObject.FindProperty("vector2Overrides")),
                CreateOverrideListEditor<Vector3>(serializedObject.FindProperty("vector3Overrides")),
                CreateOverrideListEditor<Vector4>(serializedObject.FindProperty("vector4Overrides"), DrawVector4),
                CreateOverrideListEditor<Margin>(serializedObject.FindProperty("marginOverrides"), DrawMarginOrPadding),
                CreateOverrideListEditor<Padding>(serializedObject.FindProperty("paddingOverrides"), DrawMarginOrPadding)
            };
        }


        public override void OnInspectorGUI()
        {
            foldoutEvents = EditorGUILayout.BeginFoldoutHeaderGroup(foldoutEvents, "Global Events");
            if (foldoutEvents)
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(onButtonClicked);
                EditorGUILayout.PropertyField(onToggleChanged);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            foldoutOverrides = EditorGUILayout.BeginFoldoutHeaderGroup(foldoutOverrides, "Global Sizer Overrides");
            if (foldoutOverrides)
            {
                EditorGUILayout.Space();
                
                foreach (ReorderableList list in listOverrides)
                {
                    list.DoLayoutList();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }

        ReorderableList CreateOverrideListEditor<T>(SerializedProperty property, Action<Rect, SerializedProperty> drawLastCalculatedSize = null)
        {
            ReorderableList list = new ReorderableList(property.serializedObject, property, true, true, true, true);
            list.elementHeight = 2 + 5 * (EditorGUIUtility.singleLineHeight + 2);
            list.drawHeaderCallback = (Rect rect) =>
            {
                int tmp = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                EditorGUI.LabelField(rect, property.displayName, EditorStyles.miniLabel);
                EditorGUI.indentLevel = tmp;
            };

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                int tmp = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                var element = list.serializedProperty.GetArrayElementAtIndex(index);

                float height = EditorGUIUtility.singleLineHeight;
                Rect r = new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight);

                var prefabField = element.FindPropertyRelative("Prefab");
                EditorGUI.PropertyField(r, prefabField);

                r.y += EditorGUIUtility.singleLineHeight + 2;
                r.width -= 20;
                Rect rd = new Rect(rect.x + r.width + 2, r.y, 18, EditorGUIUtility.singleLineHeight);
                var compTypeField = element.FindPropertyRelative("ComponentTypeName");
                EditorGUI.PropertyField(r, compTypeField);
                DrawComponentSelection(rd, prefabField, compTypeField);

                r.y += EditorGUIUtility.singleLineHeight + 2;
                rd.y = r.y;
                var sizerField = element.FindPropertyRelative("SizerPropertyName");
                EditorGUI.PropertyField(r, sizerField);
                DrawSizerPropertySelection<T>(rd, prefabField, compTypeField, sizerField);

                r.y += EditorGUIUtility.singleLineHeight + 2;
                rd.y = r.y;
                var screenConfigField = element.FindPropertyRelative("ScreenConfigName");
                EditorGUI.PropertyField(r, screenConfigField);
                DrawScreenConfigSelection(rd, screenConfigField);

                r.y += EditorGUIUtility.singleLineHeight + 2;
                r.width += 20;
                EditorGUI.BeginDisabledGroup(true);
                if (drawLastCalculatedSize != null)
                {
                    drawLastCalculatedSize(r, element.FindPropertyRelative("LastCalculatedSize"));
                }
                else
                {
                    EditorGUI.PropertyField(r, element.FindPropertyRelative("LastCalculatedSize"));
                }

                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel = tmp;
            };

            return list;
        }


        private void DrawComponentSelection(Rect rect, SerializedProperty prefabField, SerializedProperty compTypeField)
        {
            var go = prefabField.objectReferenceValue as GameObject;
            if(go == null) 
                return;

            if (!EditorGUI.DropdownButton(rect, GUIContent.none, FocusType.Keyboard))
                return;

            var comps = go.GetComponents<MonoBehaviour>();

            GenericMenu menu = new GenericMenu();
            foreach(var comp in comps)
            {
                var cachedComponent = comp;
                menu.AddItem(new GUIContent(comp.GetType().Name), false, () =>
                {
                    compTypeField.stringValue = cachedComponent.GetType().Name;
                    compTypeField.serializedObject.ApplyModifiedProperties();
                });
            }

            if (comps.Length <= 0)
            {
                menu.AddDisabledItem(new GUIContent("< No Component on top level >"));
            }

            menu.DropDown(rect);
        }

        private void DrawSizerPropertySelection<T>(Rect rect, SerializedProperty prefabField, SerializedProperty compTypeField, SerializedProperty sizerField)
        {
            var go = prefabField.objectReferenceValue as GameObject;
            if (go == null)
                return;

            var comp = go.GetComponent(compTypeField.stringValue);
            if (comp == null)
                return;


            if (!EditorGUI.DropdownButton(rect, GUIContent.none, FocusType.Keyboard))
                return;

            var props = comp.GetType().GetProperties()
                .Where(o => typeof(ScreenDependentSize<T>).IsAssignableFrom(o.PropertyType));


            GenericMenu menu = new GenericMenu();
            foreach (var prop in props)
            {
                var cache = prop;
                menu.AddItem(new GUIContent(prop.Name), false, () =>
                {
                    sizerField.stringValue = cache.Name;
                    sizerField.serializedObject.ApplyModifiedProperties();
                });
            }

            if(!props.Any())
            {
                menu.AddDisabledItem(new GUIContent($"< No Size modifier properties for {typeof(T).Name}s >"));
            }

            menu.DropDown(rect);
        }

        private void DrawScreenConfigSelection(Rect rect, SerializedProperty screenConfigField)
        {
            if (!EditorGUI.DropdownButton(rect, GUIContent.none, FocusType.Keyboard))
                return;

            var screens = ResolutionMonitor.Instance.OptimizedScreens;

            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent($"{ResolutionMonitor.Instance.FallbackName} (Fallback)"), false, () =>
            {
                screenConfigField.stringValue = "";
                screenConfigField.serializedObject.ApplyModifiedProperties();
            });

            menu.AddSeparator("");

            foreach(var screen in screens)
            {
                var cache = screen;

                menu.AddItem(new GUIContent(screen.Name), false, () =>
                {
                    screenConfigField.stringValue = screen.Name;
                    screenConfigField.serializedObject.ApplyModifiedProperties();
                });
            }

            if (screens.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent($"< No screen configurations defined in Resolution Monitor >"));
            }

            menu.DropDown(rect);
        }

        private void DrawMarginOrPadding(Rect rect, SerializedProperty property)
        {
            DrawFourFields(rect, property, "left", "←", "right", "→", "top", "↑", "bottom", "↓");
        }

        private void DrawVector4(Rect rect, SerializedProperty property)
        {
            DrawFourFields(rect, property, "x", "X", "y", "Y", "z", "Z", "w", "W");
        }

        private void DrawFourFields(Rect rect, SerializedProperty property, string aProp, string aLbl, string bProp, string bLbl, string cProp, string cLbl, string dProp, string dLbl)
        {
            SerializedProperty left = property.FindPropertyRelative(aProp);
            SerializedProperty right = property.FindPropertyRelative(bProp);
            SerializedProperty top = property.FindPropertyRelative(cProp);
            SerializedProperty bottom = property.FindPropertyRelative(dProp);

            float labelWidth = EditorGUIUtility.labelWidth;
            Rect labelR = new Rect(rect.x, rect.y, labelWidth, rect.height);
            EditorGUI.LabelField(labelR, property.displayName);

            EditorGUIUtility.labelWidth = 12;

            float w = (rect.width - labelWidth) / 4f;
            Rect r = new Rect(rect.x + labelWidth, rect.y, w, rect.height);
            EditorGUI.PropertyField(r, left, new GUIContent(aLbl));
            r.x += w;
            EditorGUI.PropertyField(r, right, new GUIContent(bLbl));
            r.x += w;
            EditorGUI.PropertyField(r, top, new GUIContent(cLbl));
            r.x += w;
            EditorGUI.PropertyField(r, bottom, new GUIContent(dLbl));

            EditorGUIUtility.labelWidth = labelWidth;
        }

    }
}
