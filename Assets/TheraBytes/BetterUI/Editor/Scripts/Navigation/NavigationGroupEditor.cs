using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
    [CustomEditor(typeof(NavigationGroup))]
    public class NavigationGroupEditor : UnityEditor.Editor
    {
        static GUIContent excludeSubGroupsContent = new GUIContent("Exclude Sub-Navigation-Groups",
            "If checked, selectables which are childs of child-navigation-groups are not collected. If you know that there are no Navigation Groups beneath, you can turn this off to gain a little bit of performance.");

        SerializedProperty focusTime;
        SerializedProperty focusPriority;
        SerializedProperty cancelAction;
        SerializedProperty cancelButton;
        SerializedProperty cancelEvent;
        SerializedProperty cancelWhenNoSelectablePresent;
        SerializedProperty focusTransitions;
        SerializedProperty excludeSubGroups;

        SelectableCollectionDrawer selectableGroupDrawer;
        TransitionCollectionDrawer transitionCollectionDrawer;

        protected virtual void OnEnable()
        {
            focusTime = serializedObject.FindProperty("focusTime");
            focusPriority = serializedObject.FindProperty("focusPriority");
            cancelAction = serializedObject.FindProperty("cancelAction");
            cancelButton = serializedObject.FindProperty("cancelButton");
            cancelEvent = serializedObject.FindProperty("cancelEvent");
            cancelWhenNoSelectablePresent = serializedObject.FindProperty("cancelWhenNoSelectablePresent");

            focusTransitions = serializedObject.FindProperty("focusTransitions");
            excludeSubGroups = serializedObject.FindProperty("excludeSubGroups");

            var groupProp = serializedObject.FindProperty("selectableGroup");
            selectableGroupDrawer = new SelectableCollectionDrawer(groupProp);

            transitionCollectionDrawer = new TransitionCollectionDrawer(typeof(NavigationGroup), "focusTransitions");
        }

        public override void OnInspectorGUI()
        {
            if (NavigationGroup.Current == serializedObject.targetObject)
            {
                EditorGUILayout.HelpBox("Currently Focused", MessageType.Info);
            }

            EditorGUILayout.PropertyField(focusTime);
            EditorGUILayout.PropertyField(focusPriority);
            EditorGUILayout.PropertyField(cancelAction);

            EditorGUI.indentLevel++;
            switch ((CancelAction)cancelAction.intValue)
            {
                case CancelAction.None:
                    break;

                case CancelAction.TriggerButtonClick:
                    EditorGUILayout.PropertyField(cancelButton);
                    goto default;

                case CancelAction.TriggerCustomEvent:
                    EditorGUILayout.PropertyField(cancelEvent);
                    goto default;

                default:
                    EditorGUILayout.PropertyField(cancelWhenNoSelectablePresent);
                    break;
            }
            
            EditorGUI.indentLevel--;


            EditorGUILayout.PropertyField(excludeSubGroups, excludeSubGroupsContent);
            selectableGroupDrawer.Draw();
            transitionCollectionDrawer.Draw(() => focusTransitions);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
