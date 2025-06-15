using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Terrain;

namespace TheraBytes.BetterUi.Editor
{
    public class ImageAppearanceProviderEditorHelper
    {
        public static readonly string DEFAULT = "Default";
        public static readonly string CUSTOM = "Custom";

        SerializedObject serializedObject;
        IImageAppearanceProvider img;

        private SerializedProperty materialProperties;
        private SerializedProperty materialProperty1, materialProperty2, materialProperty3;
        private SerializedProperty propMatType, propEffType;
        
        string[] materials;
        int materialIndex, materialEffectIndex;

        public ImageAppearanceProviderEditorHelper(SerializedObject serializedObject, IImageAppearanceProvider img, bool materialPropertiesOnly = false)
        {
            this.serializedObject = serializedObject;
            this.img = img;

            this.materialProperties = serializedObject.FindProperty("materialProperties");
            this.materialProperty1 = serializedObject.FindProperty("materialProperty1");
            this.materialProperty2 = serializedObject.FindProperty("materialProperty2");
            this.materialProperty3 = serializedObject.FindProperty("materialProperty3");

            if (!materialPropertiesOnly)
            {
                propMatType = serializedObject.FindProperty("materialType");
                propEffType = serializedObject.FindProperty("materialEffect");


                List<string> materialOptions = new List<string>();
                materialOptions.Add(DEFAULT);
                materialOptions.AddRange(Materials.Instance.GetAllMaterialNames());
                materialOptions.Add(CUSTOM);
                materials = materialOptions.ToArray();

                materialIndex = materialOptions.IndexOf(img.MaterialType);
                if (materialIndex < 0)
                    materialIndex = 0;

                var effectOptions = Materials.Instance.GetAllMaterialEffects(img.MaterialType).ToList();
                materialEffectIndex = effectOptions.IndexOf(img.MaterialEffect);
                if (materialEffectIndex < 0)
                    materialEffectIndex = 0;
            }
        }

        public void DrawMaterialGui(SerializedProperty material)
        {
            Debug.Assert(materials != null, "The provider was created for properties only but tries to draw also the material selection. Either Use DrawMaterialPropertiesGuiOnly() or pass false in the constructor.");

            // MATERIAL
            DrawMaterialDropdowns(out string materialType, out MaterialEffect effect);
            DrawCustomMaterial(material, materialType);

            TrackMaterialChange(material, materialType, effect, false);
            DrawMaterialProperties(materialType);

        }

        public void DrawMaterialPropertiesGuiOnly(string previousMaterialType, MaterialEffect previousMaterialEffect)
        {
            TrackMaterialChange(null, previousMaterialType, previousMaterialEffect, true);
            DrawMaterialProperties(previousMaterialType);
        }

        private void DrawCustomMaterial(SerializedProperty material, string materialType)
        {
            if (materialType != CUSTOM)
                return;

            EditorGUILayout.PropertyField(material, new GUILayoutOption[0]);

            if (material.objectReferenceValue == null)
                return;

            bool isOrig = !(material.objectReferenceValue.name.EndsWith("(Clone)")); // TODO: find better check
            EditorGUILayout.BeginHorizontal(new GUILayoutOption[0]);

            GUILayout.Label((isOrig) ? "Material: SHARED" : "Material: CLONED",
                GUILayout.Width(EditorGUIUtility.labelWidth));

            if (GUILayout.Button((isOrig) ? "Clone" : "Remove",
                EditorStyles.miniButton, new GUILayoutOption[0]))
            {
                material.objectReferenceValue = (isOrig)
                    ? Material.Instantiate(img.material)
                    : null;

                img.SetMaterialDirty();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMaterialProperties(string materialType)
        {
            if (materialType == CUSTOM || materialType == DEFAULT)
                return;

            var floats = materialProperties.FindPropertyRelative("FloatProperties");
            if (floats != null)
            {
                for (int i = 0; i < floats.arraySize; i++)
                {
                    var f = img.MaterialProperties.FloatProperties[i];
                    var p = floats.GetArrayElementAtIndex(i);
                    string displayName = p.FindPropertyRelative("Name").stringValue;

                    SerializedProperty valProp;
                    switch (i)
                    {
                        case 0:
                            valProp = materialProperty1;
                            break;
                        case 1:
                            valProp = materialProperty2;
                            break;
                        case 2:
                            valProp = materialProperty3;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (f.IsRestricted)
                    {
                        EditorGUILayout.Slider(valProp, f.Min, f.Max, displayName);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(valProp, new GUIContent(displayName));
                    }

                    SerializedProperty innerProp = p.FindPropertyRelative("Value");
                    innerProp.floatValue = valProp.floatValue;
                }
            }
        }

        private void TrackMaterialChange(SerializedProperty material, string materialType, MaterialEffect effect, bool parametersAreOldValues)
        {
            // material type changed
            bool materialChanged = img.MaterialType != materialType;
            bool effectChanged = img.MaterialEffect != effect;

            if(parametersAreOldValues)
            {
                materialType = img.MaterialType;
                effect = img.MaterialEffect;
            }

            if (materialChanged || effectChanged)
            {
                if (propMatType != null)
                {
                    propMatType.stringValue = materialType;
                }

                if (material != null)
                {
                    var materialInfo = Materials.Instance.GetMaterialInfo(materialType, effect);
                    material.objectReferenceValue = (materialInfo != null) ? materialInfo.Material : null;
                }

                if (propEffType != null)
                {
                    propEffType.enumValueIndex = (int)effect;
                }

                int infoIdx = Materials.Instance.GetMaterialInfoIndex(materialType, effect);
                if (infoIdx >= 0)
                {
                    SerializedObject obj = new SerializedObject(Materials.Instance);
                    var source = obj.FindProperty("materials")
                        .GetArrayElementAtIndex(infoIdx)
                        .FindPropertyRelative("Properties");

                    SerializedPropertyUtil.Copy(source, materialProperties);
                    materialProperties = serializedObject.FindProperty("materialProperties");
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();

                    // update material properties
                    var floats = materialProperties.FindPropertyRelative("FloatProperties");
                    if (floats != null)
                    {
                        for (int i = 0; i < floats.arraySize; i++)
                        {
                            var p = floats.GetArrayElementAtIndex(i);
                            SerializedProperty innerProp = p.FindPropertyRelative("Value");
                            if (innerProp == null)
                                continue;

                            SerializedProperty valProp;
                            switch (i)
                            {
                                case 0:
                                    valProp = materialProperty1;
                                    break;
                                case 1:
                                    valProp = materialProperty2;
                                    break;
                                case 2:
                                    valProp = materialProperty3;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            if (materialChanged && !parametersAreOldValues)
                                valProp.floatValue = innerProp.floatValue;
                            else if (effectChanged && parametersAreOldValues)
                                innerProp.floatValue = valProp.floatValue;
                        }
                    }
                }

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private void DrawMaterialDropdowns(out string materialType, out MaterialEffect effect)
        {
            materialIndex = EditorGUILayout.Popup("Material", materialIndex, materials);
            materialType = materials[materialIndex];
            if (materialType == CUSTOM || materialType == DEFAULT)
            {
                effect = MaterialEffect.Normal;
            }
            else
            {
                var options = Materials.Instance.GetAllMaterialEffects(materialType).Select(o => o.ToString()).ToArray();
                materialEffectIndex = EditorGUILayout.Popup("Effect", materialEffectIndex, options);
                if (materialEffectIndex >= options.Length)
                    materialEffectIndex = 0;

                effect = (MaterialEffect)Enum.Parse(typeof(MaterialEffect), options[materialEffectIndex]);
            }
        }

        public static void DrawColorGui(SerializedProperty colorMode, SerializedProperty firstColor, SerializedProperty secondColor)
        {
            // COLOR
            EditorGUILayout.PropertyField(colorMode);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();

            EditorGUILayout.PropertyField(firstColor, new GUILayoutOption[0]);

            var mode = (ColorMode)colorMode.intValue;
            if (mode != ColorMode.Color)
            {
                EditorGUILayout.PropertyField(secondColor);
            }

            EditorGUILayout.EndVertical();
            if (mode != ColorMode.Color)
            {
                if (GUILayout.Button("↕",
                    GUILayout.Width(25), GUILayout.Height(2 * EditorGUIUtility.singleLineHeight)))
                {
                    Color a = firstColor.colorValue;
                    firstColor.colorValue = secondColor.colorValue;
                    secondColor.colorValue = a;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();
        }
    }
}
