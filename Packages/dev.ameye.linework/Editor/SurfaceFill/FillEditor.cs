using System;
using Linework.Common.Utils;
using Linework.Editor.Common.Utils;
using Linework.SurfaceFill;
using UnityEditor;

namespace Linework.Editor.SurfaceFill
{
    [CustomEditor(typeof(Fill))]
    public class FillEditor : UnityEditor.Editor
    {
        private SerializedProperty renderingLayer;
        private SerializedProperty layerMask;
        private SerializedProperty renderQueue;
        private SerializedProperty materialType;
        private SerializedProperty customMaterial;
        private SerializedProperty occlusion;
        private SerializedProperty blendMode;
        private SerializedProperty pattern;
        private SerializedProperty primaryColor;
        private SerializedProperty secondaryColor;
        private SerializedProperty texture;
        private SerializedProperty channel;
        private SerializedProperty frequency;
        private SerializedProperty density;
        private SerializedProperty rotation;
        private SerializedProperty direction;
        private SerializedProperty offset;
        private SerializedProperty speed;
        private SerializedProperty scale;
        private SerializedProperty softness;
        private SerializedProperty width;
        private SerializedProperty power;

        private void OnEnable()
        {
            renderingLayer = serializedObject.FindProperty(nameof(Fill.RenderingLayer));
            layerMask = serializedObject.FindProperty(nameof(Fill.layerMask));
            renderQueue = serializedObject.FindProperty(nameof(Fill.renderQueue));
            materialType = serializedObject.FindProperty(nameof(Fill.materialType));
            customMaterial = serializedObject.FindProperty(nameof(Fill.customMaterial));
            occlusion = serializedObject.FindProperty(nameof(Fill.occlusion));
            blendMode = serializedObject.FindProperty(nameof(Fill.blendMode));
            pattern = serializedObject.FindProperty(nameof(Fill.pattern));
            primaryColor = serializedObject.FindProperty(nameof(Fill.primaryColor));
            secondaryColor = serializedObject.FindProperty(nameof(Fill.secondaryColor));
            texture = serializedObject.FindProperty(nameof(Fill.texture));
            channel = serializedObject.FindProperty(nameof(Fill.channel));
            frequency = serializedObject.FindProperty(nameof(Fill.frequencyX));
            density = serializedObject.FindProperty(nameof(Fill.density));
            rotation = serializedObject.FindProperty(nameof(Fill.rotation));
            direction = serializedObject.FindProperty(nameof(Fill.direction));
            offset = serializedObject.FindProperty(nameof(Fill.offset));
            speed = serializedObject.FindProperty(nameof(Fill.speed));
            scale = serializedObject.FindProperty(nameof(Fill.scale));
            width = serializedObject.FindProperty(nameof(Fill.width));
            power = serializedObject.FindProperty(nameof(Fill.power));
            softness = serializedObject.FindProperty(nameof(Fill.softness));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(renderingLayer, EditorUtils.CommonStyles.FillLayer);
            EditorGUILayout.PropertyField(layerMask, EditorUtils.CommonStyles.LayerMask);
            EditorGUILayout.PropertyField(renderQueue, EditorUtils.CommonStyles.RenderQueue);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Render", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(occlusion, EditorUtils.CommonStyles.FillOcclusion);
            EditorGUILayout.PropertyField(blendMode, EditorUtils.CommonStyles.FillBlendMode);
            // TODO: enable in future update
            // EditorGUILayout.PropertyField(alphaCutout, EditorUtils.CommonStyles.AlphaCutout);
            // if (alphaCutout.boolValue)
            // {
            //     EditorGUI.indentLevel++;
            //     EditorGUILayout.PropertyField(alphaCutoutTexture, EditorUtils.CommonStyles.AlphaCutoutTexture); 
            //     EditorGUILayout.PropertyField(alphaCutoutThreshold, EditorUtils.CommonStyles.AlphaCutoutThreshold);
            //     EditorGUI.indentLevel--;
            // }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fill", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(materialType, EditorUtils.CommonStyles.MaterialType);
            switch ((MaterialType) materialType.intValue)
            {
                case MaterialType.Basic:
                    EditorGUILayout.PropertyField(pattern, EditorUtils.CommonStyles.Pattern);
                    RenderPatternSettings();
                    break;
                case MaterialType.Custom:
                    EditorGUILayout.PropertyField(customMaterial, EditorUtils.CommonStyles.CustomMaterial);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if ((MaterialType) materialType.intValue == MaterialType.Custom)
            {
                EditorGUILayout.HelpBox("A custom fill material should use a Fullscreen Shader Graph shader with 'Allow Material Override' and 'Enable Stencil' enabled. Also set 'Blend Mode' to 'Custom'.", MessageType.Warning);
            }
            EditorGUILayout.Space();
            
            serializedObject.ApplyModifiedProperties();
        }

        private void RenderPatternSettings()
        {
            switch ((Pattern) pattern.intValue)
            {
                case Pattern.Solid:
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(primaryColor, EditorUtils.CommonStyles.FillColor);
                    EditorGUI.indentLevel--;
                    break;
                case Pattern.Checkerboard:
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(primaryColor, EditorUtils.CommonStyles.PrimaryFillColor);
                    EditorGUILayout.PropertyField(secondaryColor, EditorUtils.CommonStyles.SecondaryFillColor);
                    EditorGUILayout.PropertyField(frequency, EditorUtils.CommonStyles.Frequency);
                    EditorGUILayout.PropertyField(rotation, EditorUtils.CommonStyles.Rotation);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.LabelField("Movement");
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(direction, EditorUtils.CommonStyles.Direction);
                    EditorGUILayout.PropertyField(speed, EditorUtils.CommonStyles.Speed);
                    EditorGUI.indentLevel--;
                    break;
                case Pattern.Dots:
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(primaryColor, EditorUtils.CommonStyles.PrimaryFillColor);
                    EditorGUILayout.PropertyField(secondaryColor, EditorUtils.CommonStyles.SecondaryFillColor);
                    EditorGUILayout.PropertyField(frequency, EditorUtils.CommonStyles.Frequency);
                    EditorGUILayout.PropertyField(density, EditorUtils.CommonStyles.Density);
                    EditorGUILayout.PropertyField(rotation, EditorUtils.CommonStyles.Rotation);
                    EditorGUILayout.PropertyField(offset, EditorUtils.CommonStyles.Offset);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.LabelField("Movement");
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(direction, EditorUtils.CommonStyles.Direction);
                    EditorGUILayout.PropertyField(speed, EditorUtils.CommonStyles.Speed);
                    EditorGUI.indentLevel--;
                    break;
                case Pattern.Stripes:
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(primaryColor, EditorUtils.CommonStyles.PrimaryFillColor);
                    EditorGUILayout.PropertyField(secondaryColor, EditorUtils.CommonStyles.SecondaryFillColor);
                    EditorGUILayout.PropertyField(frequency, EditorUtils.CommonStyles.Frequency);
                    EditorGUILayout.PropertyField(density, EditorUtils.CommonStyles.Density);
                    EditorGUILayout.PropertyField(rotation, EditorUtils.CommonStyles.Rotation);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.LabelField("Movement");
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(speed, EditorUtils.CommonStyles.Speed);
                    EditorGUI.indentLevel--;
                    break;
                case Pattern.Glow:
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(primaryColor, EditorUtils.CommonStyles.FillColor);
                    EditorGUILayout.PropertyField(width, EditorUtils.CommonStyles.Width);
                    EditorGUILayout.PropertyField(softness, EditorUtils.CommonStyles.Softness);
                    EditorGUILayout.PropertyField(power, EditorUtils.CommonStyles.Power);
                    EditorGUI.indentLevel--;
                    break;
                case Pattern.Texture:
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(primaryColor, EditorUtils.CommonStyles.PrimaryFillColor);
                    EditorGUILayout.PropertyField(secondaryColor, EditorUtils.CommonStyles.SecondaryFillColor);
                    EditorGUILayout.PropertyField(texture, EditorUtils.CommonStyles.Texture);
                    EditorGUILayout.PropertyField(channel, EditorUtils.CommonStyles.Channel);
                    EditorGUILayout.PropertyField(scale, EditorUtils.CommonStyles.Scale);
                    EditorGUILayout.PropertyField(rotation, EditorUtils.CommonStyles.Rotation);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.LabelField("Movement");
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(direction, EditorUtils.CommonStyles.Direction);
                    EditorGUILayout.PropertyField(speed, EditorUtils.CommonStyles.Speed);
                    EditorGUI.indentLevel--;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}