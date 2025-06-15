using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
    [CustomEditor(typeof(BetterNavigation))]
    public class BetterNavigationEditor : UnityEditor.Editor
    {
        SerializedProperty handleNavigationInput;
        SerializedProperty omitSelectionStatesForPointerInput;
        SerializedProperty dirtyStateDetection;
        SelectableCollectionDrawer rootSelectablesDrawer;

        protected virtual void OnEnable()
        {
            handleNavigationInput = serializedObject.FindProperty("handleNavigationInput");
            omitSelectionStatesForPointerInput = serializedObject.FindProperty("omitSelectionStatesForPointerInput");
            dirtyStateDetection = serializedObject.FindProperty("dirtyStateDetection");
            rootSelectablesDrawer = new SelectableCollectionDrawer(serializedObject.FindProperty("rootSelectables"));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(omitSelectionStatesForPointerInput);
            EditorGUILayout.PropertyField(handleNavigationInput);
            EditorGUILayout.PropertyField(dirtyStateDetection);
            rootSelectablesDrawer.Draw();

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("CONTEXT/EventSystem/♠ Add Better Navigation", false)]
        public static void AddBetterNavigator(MenuCommand command)
        {
            var ctx = command.context as MonoBehaviour;
            var locator = ctx.gameObject.AddComponent<BetterNavigation>();

            while (UnityEditorInternal.ComponentUtility.MoveComponentUp(locator))
            { }

            UnityEditorInternal.ComponentUtility.MoveComponentDown(locator);
        }

        [MenuItem("CONTEXT/EventSystem/♠ Add Better Navigation", true)]
        public static bool CheckBetterNavigator(MenuCommand command)
        {
            var ctx = command.context as MonoBehaviour;
            return ctx.gameObject.GetComponent<BetterNavigation>() == null;
        }
    }
}
