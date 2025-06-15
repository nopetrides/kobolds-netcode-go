using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
    public abstract class InputModuleAdditionWithAxisEditor : InputModuleAdditionEditor
    {
        static readonly GUIContent navigationAxisContent = new GUIContent("Mapped Axis for 'Any Direction'", "Specify here which axis should be used for looking up visualizations for the navigation input.");

        SerializedProperty navigationVisualizationAxisBinding;
        SerializedProperty mapAllAxis;

        protected override void OnEnable()
        {
            base.OnEnable();

            navigationVisualizationAxisBinding = serializedObject.FindProperty("navigationVisualizationAxisBinding");
            mapAllAxis = serializedObject.FindProperty("mapAllNavigationActionsToThisAxis");
        }

        public override void OnInspectorGUI()
        {
            DrawBindings();
            EditorGUILayout.Space();
            DrawAxisBindingGui();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAxisBindingGui()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Bindings for Navigation Visualization", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(navigationVisualizationAxisBinding, navigationAxisContent);

            EditorGUI.indentLevel++;

            var rect = EditorGUILayout.GetControlRect();
            var label = new GUIContent(navigationVisualizationAxisBinding.intValue == 0
                ? "Also use Horizontal for NavigateUp and NavigateDown"
                : "Also use Vertical for NavigateLeft and Right");

            EditorGUI.BeginProperty(rect, label, mapAllAxis);
            mapAllAxis.boolValue = EditorGUI.ToggleLeft(rect, label, mapAllAxis.boolValue);
            EditorGUI.EndProperty();

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();

        }
    }

    [CustomEditor(typeof(InputModuleAddition))]
    public abstract class InputModuleAdditionEditor : UnityEditor.Editor
    {
        SerializedProperty buttonX;
        SerializedProperty buttonY;
        SerializedProperty buttonL;
        SerializedProperty buttonR;
        SerializedProperty switchLeft;
        SerializedProperty switchRight;
        SerializedProperty alternativeSwitchLeft;
        SerializedProperty alternativeSwitchRight;

        SerializedProperty contextLeft;
        SerializedProperty contextRight;
        SerializedProperty contextUp;
        SerializedProperty contextDown;

        protected virtual void OnEnable()
        {
            buttonX = serializedObject.FindProperty("buttonX");
            buttonY = serializedObject.FindProperty("buttonY");
            buttonL = serializedObject.FindProperty("buttonL");
            buttonR = serializedObject.FindProperty("buttonR");
            switchLeft = serializedObject.FindProperty("switchLeft");
            switchRight = serializedObject.FindProperty("switchRight");
            alternativeSwitchLeft = serializedObject.FindProperty("alternativeSwitchLeft");
            alternativeSwitchRight = serializedObject.FindProperty("alternativeSwitchRight");
            contextLeft = serializedObject.FindProperty("contextLeft");
            contextRight = serializedObject.FindProperty("contextRight");
            contextUp = serializedObject.FindProperty("contextUp");
            contextDown = serializedObject.FindProperty("contextDown");
        }

        public override void OnInspectorGUI()
        {
            DrawBindings();

            serializedObject.ApplyModifiedProperties();
        }

        protected void DrawBindings()
        {
            DrawButtonSelection(buttonX);
            DrawButtonSelection(buttonY);
            
            EditorGUILayout.Space();
            
            DrawButtonSelection(buttonL);
            DrawButtonSelection(buttonR);
            
            EditorGUILayout.Space();

            DrawButtonSelection(switchLeft);
            DrawButtonSelection(switchRight);
            
            EditorGUILayout.Space();

            DrawButtonSelection(alternativeSwitchLeft);
            DrawButtonSelection(alternativeSwitchRight);

            EditorGUILayout.Space();

            DrawButtonSelection(contextLeft);
            DrawButtonSelection(contextRight);
            DrawButtonSelection(contextUp);
            DrawButtonSelection(contextDown);
        }

        protected abstract void DrawButtonSelection(SerializedProperty button);
    }
}
