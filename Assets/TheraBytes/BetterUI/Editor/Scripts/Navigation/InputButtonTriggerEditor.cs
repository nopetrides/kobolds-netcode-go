using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheraBytes.BetterUi.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{
    [CustomEditor(typeof(InputButtonTrigger))]
    public class InputButtonTriggerEditor : UnityEditor.Editor
    {
        SerializedProperty buttonActionType;
        SerializedProperty useCustomEvent;
        SerializedProperty triggerEvent;
        SerializedProperty condition;
        SerializedProperty objectForCondition;
        SerializedProperty nameForCondition;
        SerializedProperty visualization;

        InputButtonTrigger component;

        private void OnEnable()
        {
            component = target as InputButtonTrigger;
            buttonActionType = serializedObject.FindProperty("buttonActionType");
            useCustomEvent = serializedObject.FindProperty("useCustomEvent");
            triggerEvent = serializedObject.FindProperty("triggerEvent");
            condition = serializedObject.FindProperty("condition");
            objectForCondition = serializedObject.FindProperty("objectForCondition");
            nameForCondition = serializedObject.FindProperty("nameForCondition");
            visualization = serializedObject.FindProperty("visualization");
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            EditorGuiUtils.DrawEnumField(condition, 
                InteractionCondition.CurrentlySelected, 
                InteractionCondition.AlwaysTrigger);

            bool conditionChanged = EditorGUI.EndChangeCheck();

            var c = (InteractionCondition)condition.intValue;
            switch (c)
            {
                case InteractionCondition.PartOfSpecificNavigationGroup:
                    DrawConditionObject<NavigationGroup>(conditionChanged, new GUIContent("Containing Navigation Group"));
                    break;
                case InteractionCondition.SpecificNavigationControllerActive:
                    DrawConditionObject<NavigationController>(conditionChanged, new GUIContent("Related Navigation Controller"));
                    break;
                case InteractionCondition.SpecificNavigationGroupFocused:
                    DrawConditionObject<NavigationGroup>(conditionChanged, new GUIContent("Related Navigation Group"));
                    break;

                case InteractionCondition.NamedNavigationControllerActive:
                    DrawConditionName(conditionChanged, new GUIContent("GameObject Name of Navigation Controller"));
                    break;
                case InteractionCondition.NamedNavigationGroupFocused:
                    DrawConditionName(conditionChanged, new GUIContent("GameObject Name of Navigation Group"));
                    break;

                case InteractionCondition.AnyChildSelected:
                    DrawConditionObject<RectTransform>(conditionChanged, new GUIContent("Parent Object"));
                    break;
                case InteractionCondition.CurrentlySelected:
                    DrawConditionObject<Selectable>(conditionChanged, new GUIContent("Related Selectable"));
                    break;
                case InteractionCondition.ToggleCurrentlyOn:
                case InteractionCondition.ToggleCurrentlyOff:
                    DrawConditionObject<Toggle>(conditionChanged, new GUIContent("Related Toggle"));
                    break;
            }

            EditorGuiUtils.DrawInputActionWithVisualization(buttonActionType, visualization);
            EditorGUILayout.PropertyField(useCustomEvent);

            if (useCustomEvent.boolValue)
            {
                EditorGUILayout.PropertyField(triggerEvent);
            }
            else
            {
                var selectable = component.GetComponent<Selectable>();
                if (!(selectable is Button || selectable is Toggle))
                {
                    EditorGUILayout.HelpBox("There is no Button or Toggle attached to this game object. You should trigger something using Custom Events in this case.", MessageType.Error);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConditionName(bool conditionChanged, GUIContent label)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(nameForCondition, label);
            EditorGUI.indentLevel--;
        }

        private void DrawConditionObject<T>(bool conditionChanged, GUIContent label)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(objectForCondition, label);

            bool wasNull = objectForCondition.objectReferenceValue == null;
            if (conditionChanged && wasNull)
            {
                objectForCondition.objectReferenceValue = component.transform as RectTransform;
            }

            if (objectForCondition.objectReferenceValue != null
                && !(objectForCondition.objectReferenceValue as Component).TryGetComponent<T>(out _))
            {
                if (!wasNull)
                {
                    Debug.LogError($"Object '{objectForCondition.objectReferenceValue.name}' not allowed. It must have a {typeof(T).Name} component.");
                }

                objectForCondition.objectReferenceValue = null;
            }
            

            EditorGUI.indentLevel--;
        }
    }
}
