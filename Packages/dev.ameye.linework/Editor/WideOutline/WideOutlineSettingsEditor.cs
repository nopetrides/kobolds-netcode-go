using System;
using Linework.Common.Utils;
using Linework.Editor.Common.Utils;
using Linework.WideOutline;
using UnityEditor;
using UnityEditor.Rendering;

namespace Linework.Editor.WideOutline
{
    [CustomEditor(typeof(WideOutlineSettings))]
    public class WideOutlineSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty injectionPoint;
        private SerializedProperty showInSceneView;
        
        private SerializedProperty materialType;
        private SerializedProperty customMaterial;
        private SerializedProperty widthMethod;
        private SerializedProperty blendMode;
        private SerializedProperty width;
        private SerializedProperty gap;
        private SerializedProperty customDepthBuffer;
        private SerializedProperty occludedColor;
        
        private SerializedProperty outlines;
        private EditorList<Outline> outlineList;

        private void OnEnable()
        {
            injectionPoint = serializedObject.FindProperty("injectionPoint");
            showInSceneView = serializedObject.FindProperty("showInSceneView");
            
            materialType = serializedObject.FindProperty(nameof(WideOutlineSettings.materialType));
            customMaterial = serializedObject.FindProperty(nameof(WideOutlineSettings.customMaterial));
            widthMethod = serializedObject.FindProperty(nameof(WideOutlineSettings.widthControl));
            blendMode = serializedObject.FindProperty(nameof(WideOutlineSettings.blendMode));
            width = serializedObject.FindProperty(nameof(WideOutlineSettings.sharedWidth));
            gap = serializedObject.FindProperty(nameof(WideOutlineSettings.gap));
            customDepthBuffer = serializedObject.FindProperty(nameof(WideOutlineSettings.customDepthBuffer));
            occludedColor = serializedObject.FindProperty(nameof(WideOutlineSettings.occludedColor));

            outlines = serializedObject.FindProperty("outlines");
            outlineList = new EditorList<Outline>(this, outlines, ForceSave, "Add Outline", "No outlines added.");
        }

        private void OnDisable()
        {
            outlineList.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            if (outlines == null) OnEnable();

            serializedObject.Update();

            var occlusionChanged = false;
            var widthMethodChanged = false;
            
            EditorGUILayout.LabelField("Wide Outline", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(injectionPoint, EditorUtils.CommonStyles.InjectionPoint);
            EditorGUILayout.PropertyField(showInSceneView, EditorUtils.CommonStyles.ShowInSceneView);
            EditorGUILayout.Space();
            CoreEditorUtils.DrawSplitter();
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.LabelField(EditorUtils.CommonStyles.Outlines, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(materialType, EditorUtils.CommonStyles.MaterialType);
            switch ((MaterialType) materialType.intValue)
            {
                case MaterialType.Basic:
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(widthMethod, EditorUtils.CommonStyles.WidthControl);
                    widthMethodChanged |= EditorGUI.EndChangeCheck();
                    EditorGUI.indentLevel++;
                    if ((WidthControl) widthMethod.intValue == WidthControl.Shared)
                    {
                        EditorGUILayout.PropertyField(width, EditorUtils.CommonStyles.OutlineWidth);
                    }
                    EditorGUI.indentLevel--;
                    if ((WidthControl) widthMethod.intValue == WidthControl.PerOutline)
                    {
                        EditorGUILayout.HelpBox("An information buffer will be generated. See the documentation for details.", MessageType.Warning);
                    }
                    EditorGUILayout.PropertyField(gap, EditorUtils.CommonStyles.OutlineGap);
                    EditorGUILayout.PropertyField(blendMode, EditorUtils.CommonStyles.OutlineBlendMode);
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(customDepthBuffer, EditorUtils.CommonStyles.FixBleeding);
                    occlusionChanged |= EditorGUI.EndChangeCheck();
                    if (customDepthBuffer.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(occludedColor,EditorUtils.CommonStyles.OutlineOccludedColor);
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.BeginChangeCheck();
                    break;
                case MaterialType.Custom:
                    EditorGUILayout.PropertyField(width, EditorUtils.CommonStyles.OutlineWidth);
                    EditorGUILayout.PropertyField(customMaterial, EditorUtils.CommonStyles.CustomMaterial);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            EditorGUILayout.Space();
            
            serializedObject.ApplyModifiedProperties();
            outlineList.Draw();
            
            EditorGUILayout.Space();
            
            if (occlusionChanged)
            {
                ForceSave();
            }
            
            if (widthMethodChanged)
            {
                ForceSave();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ForceSave()
        {
            ((WideOutlineSettings) target).Changed();
            EditorUtility.SetDirty(target);
        }
    }
}