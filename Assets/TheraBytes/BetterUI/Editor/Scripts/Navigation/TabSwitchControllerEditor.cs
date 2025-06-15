using UnityEditor;
using static TheraBytes.BetterUi.TabSwitchController;
using UnityEngine.UI;
using TheraBytes.BetterUi.Editor;
using System;

namespace TheraBytes.BetterUi
{
    [CustomEditor(typeof(TabSwitchController))]
    public class TabSwitchControllerEditor : DefaultMoveNavigationControllerBaseEditor
    {
        SerializedProperty settingsFallback;
        SerializedProperty customSettings ;
        SerializedProperty focusMode;
        SerializedProperty tabs;
        SerializedProperty controlledNavigationGroups;
        SerializedProperty controlledNavigationGroupsParent;
        SerializedProperty currentTabChanged;

        SelectableCollectionDrawer tabsDrawer;
        NavigationGroupCollectionDrawer navigationGroupsDrawer;
        protected override bool IsNavigationControlOptional { get { return true; } }

        protected override void OnEnable()
        {
            base.OnEnable();

            settingsFallback = serializedObject.FindProperty("settingsFallback");
            customSettings = serializedObject.FindProperty("customSettings");
            focusMode = serializedObject.FindProperty("focusMode");
            tabs = serializedObject.FindProperty("tabs");
            currentTabChanged = serializedObject.FindProperty("currentTabChanged");
            controlledNavigationGroupsParent = serializedObject.FindProperty("controlledNavigationGroupsParent");
            controlledNavigationGroups = serializedObject.FindProperty("controlledNavigationGroups");

            tabsDrawer = new SelectableCollectionDrawer(tabs);
            navigationGroupsDrawer = new NavigationGroupCollectionDrawer(controlledNavigationGroups);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();

            tabsDrawer.Draw();
            EditorGuiUtils.DrawFlagsEnumField<SelectableFocusMode>(focusMode);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(currentTabChanged);

            base.OnInspectorGUI();

            ScreenConfigConnectionHelper.DrawGui("Settings", customSettings, ref settingsFallback, DrawSettings);
            serializedObject.ApplyModifiedProperties();
        }

        protected override void DrawControlledNavigationGroupStuff()
        {
            var obj = target as TabSwitchController;
            if (obj.ControlledNavigationGroups.CollectingStrategy != CollectingElementsStrategy.FixedSet)
            {
                EditorGUILayout.PropertyField(controlledNavigationGroupsParent);
            }

            navigationGroupsDrawer.Draw();
            base.DrawControlledNavigationGroupStuff();
        }

        private void DrawSettings(string configName, SerializedProperty property)
        {
            var navigateLeft = property.FindPropertyRelative("navigateLeft");
            var navigateRight = property.FindPropertyRelative("navigateRight");
            var navigateDown = property.FindPropertyRelative("navigateDown");
            var navigateUp = property.FindPropertyRelative("navigateUp");

            EditorGuiUtils.DrawInputActionWithVisualization(navigateLeft, navigateLeftVisualization);
            EditorGuiUtils.DrawInputActionWithVisualization(navigateRight, navigateRightVisualization);
            EditorGuiUtils.DrawInputActionWithVisualization(navigateDown, navigateDownVisualization);
            EditorGuiUtils.DrawInputActionWithVisualization(navigateUp, navigateUpVisualization);
        }
    }
}
