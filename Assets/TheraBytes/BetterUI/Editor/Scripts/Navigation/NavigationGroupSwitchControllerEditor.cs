using UnityEditor;
using UnityEngine.UI;
using TheraBytes.BetterUi.Editor;
using System;

namespace TheraBytes.BetterUi
{
    [CustomEditor(typeof(NavigationGroupSwitchController))]
    public class NavigationGroupSwitchControllerEditor : DefaultMoveNavigationControllerBaseEditor
    {
        SerializedProperty navigateUp;
        SerializedProperty navigateDown;
        SerializedProperty navigateLeft;
        SerializedProperty navigateRight;
        SerializedProperty navigationGroups;
        SerializedProperty triggerCurrentToggleOnSwitch;

        NavigationGroupCollectionDrawer navigationGroupsCollectionDrawer;
        protected override bool IsNavigationControlOptional { get { return false; } }

        protected override void OnEnable()
        {
            base.OnEnable();
            navigateUp = serializedObject.FindProperty("navigateUp");
            navigateDown = serializedObject.FindProperty("navigateDown");
            navigateLeft = serializedObject.FindProperty("navigateLeft");
            navigateRight = serializedObject.FindProperty("navigateRight");
            triggerCurrentToggleOnSwitch = serializedObject.FindProperty("triggerCurrentToggleOnSwitch");

            navigationGroups = serializedObject.FindProperty("navigationGroups");
            navigationGroupsCollectionDrawer = new NavigationGroupCollectionDrawer(navigationGroups);
        }


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Navigation Group Switch Bindings", EditorStyles.boldLabel);

            EditorGuiUtils.DrawInputActionWithVisualization(navigateUp, navigateUpVisualization);
            EditorGuiUtils.DrawInputActionWithVisualization(navigateDown, navigateDownVisualization);
            EditorGuiUtils.DrawInputActionWithVisualization(navigateLeft, navigateLeftVisualization);
            EditorGuiUtils.DrawInputActionWithVisualization(navigateRight, navigateRightVisualization);
            
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        protected override void DrawControlledNavigationGroupStuff()
        {

            DrawLeftToggle(triggerCurrentToggleOnSwitch);
            navigationGroupsCollectionDrawer.Draw();

            base.DrawControlledNavigationGroupStuff();
        }

    }
}
