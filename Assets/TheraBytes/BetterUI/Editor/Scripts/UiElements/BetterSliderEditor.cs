using UnityEditor;
using UnityEditor.UI;
using UnityEngine.UI;

namespace TheraBytes.BetterUi.Editor
{
    [CustomEditor(typeof(BetterSlider)), CanEditMultipleObjects]
    public class BetterSliderEditor : SliderEditor
    {
        SerializedProperty stepSizeScaleFactor;
        SerializedProperty stepSizeOverTime;
        SerializedProperty moveHandlePivotWithValue;
        BetterElementHelper<Slider, BetterSlider> helper =
            new BetterElementHelper<Slider, BetterSlider>();

        protected override void OnEnable()
        {
            base.OnEnable();
            stepSizeOverTime = serializedObject.FindProperty("stepSizeOverTime");
            stepSizeScaleFactor = serializedObject.FindProperty("stepSizeScaleFactor");
            moveHandlePivotWithValue = serializedObject.FindProperty("moveHandlePivotWithValue");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Better Navigation (Gamepad / Keyboard)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(stepSizeOverTime);
            EditorGUILayout.PropertyField(stepSizeScaleFactor);
            EditorGUILayout.EndVertical();

            EditorGUILayout.PropertyField(moveHandlePivotWithValue);
            helper.DrawGui(serializedObject);

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("CONTEXT/Slider/♠ Make Better")]
        public static void MakeBetter(MenuCommand command)
        {
            Slider obj = command.context as Slider;
            Betterizer.MakeBetter<Slider, BetterSlider>(obj);
        }
    }
}
