
#if ENABLE_LEGACY_INPUT_MANAGER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
    [CustomEditor(typeof(AdditionForStandaloneInputModule))]
    public class AdditionForStandaloneInputModuleEditor : InputModuleAdditionWithAxisEditor
    {
        SerializedProperty joystickOnlyHorizontal;
        SerializedProperty joystickOnlyVertical;
        protected override void OnEnable()
        {
            joystickOnlyHorizontal = serializedObject.FindProperty("joystickOnlyHorizontal");
            joystickOnlyVertical = serializedObject.FindProperty("joystickOnlyVertical");
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Joystick Axis Names", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Specify axis for gamepads / joysticks as defined under Edit > Project Settings > Input. This is needed to determine if the last input was a gamepad (only required if you use that in your code).", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(joystickOnlyHorizontal);
            EditorGUILayout.PropertyField(joystickOnlyVertical);
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        protected override void DrawButtonSelection(SerializedProperty button)
        {
            EditorGUILayout.PropertyField(button);
        }


        [MenuItem("CONTEXT/StandaloneInputModule/♠ Add Additional Button Mapping", false)]
        public static void AddAddition(MenuCommand command)
        {
            var ctx = command.context as MonoBehaviour;
            ctx.gameObject.AddComponent<AdditionForStandaloneInputModule>();
        }

        [MenuItem("CONTEXT/StandaloneInputModule/♠ Add Additional Button Mapping", true)]
        public static bool CheckAddition(MenuCommand command)
        {
            var ctx = command.context as MonoBehaviour;
            return ctx.gameObject.GetComponent<AdditionForStandaloneInputModule>() == null;
        }
    }
}

#endif