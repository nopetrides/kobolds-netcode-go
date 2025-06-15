using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheraBytes.BetterUi.Editor;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi
{
    public abstract class DefaultMoveNavigationControllerBaseEditor : NavigationControllerBaseEditor
    {
        const string SmartSelectTooltip = "Smart Select means that the selectable that the user would expect is selected when switching groups. If you move to the right, a selectable closest to the vertical position of the previous selectable is searched which is at the left of the newly focused group. Smart Select omits the logic for selecting an element of the collection in the navigation group";

        static readonly GUIContent viaMovementContent = new GUIContent("Smart Select for Move out of group", SmartSelectTooltip);
        static readonly GUIContent viaButtonContent = new GUIContent("Smart Select for switch by Button", SmartSelectTooltip);

        SerializedProperty switchGroupByMoving;
        SerializedProperty smartSelectWhenSwitchingViaMovement;
        SerializedProperty smartSelectWhenSwitchingViaButton;

        protected SerializedProperty navigateLeftVisualization;
        protected SerializedProperty navigateRightVisualization;
        protected SerializedProperty navigateUpVisualization;
        protected SerializedProperty navigateDownVisualization;

        protected virtual bool HasButtonMovement { get { return true; } }

        protected override void OnEnable()
        {
            base.OnEnable();

            switchGroupByMoving = serializedObject.FindProperty("switchGroupByMoving");
            smartSelectWhenSwitchingViaMovement = serializedObject.FindProperty("smartSelectWhenSwitchingViaMovement");
            smartSelectWhenSwitchingViaButton = serializedObject.FindProperty("smartSelectWhenSwitchingViaButton");

            navigateLeftVisualization = serializedObject.FindProperty("navigateLeftVisualization");
            navigateRightVisualization = serializedObject.FindProperty("navigateRightVisualization");
            navigateUpVisualization = serializedObject.FindProperty("navigateUpVisualization");
            navigateDownVisualization = serializedObject.FindProperty("navigateDownVisualization");
        }

        protected override void DrawControlledNavigationGroupStuff()
        {

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Navigation Group Switch Behavior", EditorStyles.boldLabel);
            DrawLeftToggle(switchGroupByMoving);
            if (switchGroupByMoving.boolValue)
            {
                EditorGUI.indentLevel++;
                DrawLeftToggle(smartSelectWhenSwitchingViaMovement, viaMovementContent);
                EditorGUI.indentLevel--;
            }

            if (HasButtonMovement)
            {
                DrawLeftToggle(smartSelectWhenSwitchingViaButton, viaButtonContent);
            }

            EditorGUILayout.EndVertical();

            base.DrawControlledNavigationGroupStuff();
        }
    }

    public abstract class NavigationControllerBaseEditor : UnityEditor.Editor
    {
        protected SerializedProperty isControllingNavigationGroups;
        SerializedProperty unfocusNavigationGroupOnDisable;
        SerializedProperty disableWhenNoManagedGroupActive;
        SerializedProperty enableWhenManagedGroupActive;

        SerializedProperty initializeOneFrameDelayed;

        protected abstract bool IsNavigationControlOptional { get; }

        protected virtual void OnEnable()
        {
            isControllingNavigationGroups = serializedObject.FindProperty("isControllingNavigationGroups");
            if(!IsNavigationControlOptional)
            {
                isControllingNavigationGroups.boolValue = true;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            unfocusNavigationGroupOnDisable = serializedObject.FindProperty("unfocusNavigationGroupOnDisable");
            disableWhenNoManagedGroupActive = serializedObject.FindProperty("disableWhenNoManagedGroupActive");
            enableWhenManagedGroupActive = serializedObject.FindProperty("enableWhenManagedGroupActive");
            initializeOneFrameDelayed = serializedObject.FindProperty("initializeOneFrameDelayed");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(initializeOneFrameDelayed);
            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(!IsNavigationControlOptional);
            EditorGUILayout.PropertyField(isControllingNavigationGroups);
            EditorGUI.EndDisabledGroup();

            if (isControllingNavigationGroups.boolValue)
            {
                EditorGUI.indentLevel++;
                DrawControlledNavigationGroupStuff();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }

        protected virtual void DrawControlledNavigationGroupStuff()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Enable / Disable behavior", EditorStyles.boldLabel);
            DrawLeftToggle(unfocusNavigationGroupOnDisable);
            DrawLeftToggle(disableWhenNoManagedGroupActive);
            DrawLeftToggle(enableWhenManagedGroupActive);
            EditorGUILayout.EndVertical();
        }

        protected static void DrawLeftToggle(SerializedProperty property, GUIContent content = null)
        {
            var rect = EditorGUILayout.GetControlRect();
            
            if (content == null)
            {
                content = new GUIContent(property.displayName, property.tooltip);
            }
            
            EditorGUI.BeginProperty(rect, content, property);
            property.boolValue = EditorGUI.ToggleLeft(rect, content, property.boolValue);
            EditorGUI.EndProperty();
        }
    }
}
