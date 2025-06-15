using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

namespace TheraBytes.BetterUi.Editor
{
    [CustomEditor(typeof(BetterBorder)), CanEditMultipleObjects]
    public class BetterBorderEditor : UnityEditor.Editor
    {

        SerializedProperty materialProperties;
        SerializedProperty settingsFallback;
        SerializedProperty settingsCollection;
        SerializedProperty useGraphicAlpha;

        Dictionary<string, ReorderableList> offsetListDrawers = new Dictionary<string, ReorderableList>();

        ImageAppearanceProviderEditorHelper materialPropertiesDrawer;
        string lastMaterialType;
        MaterialEffect lastMaterialEffect;

        IImageAppearanceProvider appearanceProvider;
        BetterBorder border;

        Dictionary<string, int> selectedOffsets = new Dictionary<string, int>();
        Dictionary<string, Rect> lastListRects = new Dictionary<string, Rect>();
        Dictionary<string, bool> displayCreator = new Dictionary<string, bool>();
        Vector2 creatorOffset = Vector2.one;
        int steps = 8;
        float angleOffset = 0f;



        private void OnEnable()
        {
            appearanceProvider = target as IImageAppearanceProvider;
            border = target as BetterBorder;

            materialProperties = serializedObject.FindProperty("materialProperties");
            settingsFallback = serializedObject.FindProperty("settingsFallback");
            settingsCollection = serializedObject.FindProperty("settingsCollection");
            useGraphicAlpha = serializedObject.FindProperty("useGraphicAlpha");

            materialPropertiesDrawer = new ImageAppearanceProviderEditorHelper(serializedObject, appearanceProvider, true);

        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"{appearanceProvider.MaterialType} Material", EditorStyles.boldLabel);
            materialPropertiesDrawer.DrawMaterialPropertiesGuiOnly(lastMaterialType, lastMaterialEffect);
            EditorGUILayout.EndVertical();

            EditorGUILayout.PropertyField(useGraphicAlpha);

            ScreenConfigConnectionHelper.DrawGui("Settings", settingsCollection, ref settingsFallback, DrawSettings);

            serializedObject.ApplyModifiedProperties();

            lastMaterialType = appearanceProvider.MaterialType;
            lastMaterialEffect = appearanceProvider.MaterialEffect;
        }

        private void DrawSettings(string configName, SerializedProperty property)
        {
            SerializedProperty primeColorProp = property.FindPropertyRelative("primaryColor");
            SerializedProperty colorModeProp = property.FindPropertyRelative("colorMode");
            SerializedProperty secondColorProp = property.FindPropertyRelative("secondaryColor");

            ImageAppearanceProviderEditorHelper.DrawColorGui(colorModeProp, primeColorProp, secondColorProp);
            SerializedProperty offsets = property.FindPropertyRelative("offsets");

            DrawPresetCreator(configName);
            var r = EditorGUILayout.GetControlRect(false, 1);
            DrawOffsetList(configName, offsets);
            DrawPresetCreatorButton(configName, r);

        }

        private void DrawPresetCreator(string configName)
        {
            if (!displayCreator.TryGetValue(configName, out bool display) || !display)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("Create Border from Presets", EditorStyles.boldLabel);
            if (GUILayout.Button(new GUIContent("Shadow", "Create a single offsetted border (Shadow)"), GUILayout.Width(56)))
            {
                CreatePresetBorders(configName, creatorOffset);
            }

            if (GUILayout.Button(new GUIContent("Outline", "Create four borders"), GUILayout.Width(56)))
            {
                CreatePresetBorders(configName,
                    new Vector2( + creatorOffset.x, + creatorOffset.y),
                    new Vector2( + creatorOffset.x, - creatorOffset.y),
                    new Vector2( - creatorOffset.x, + creatorOffset.y),
                    new Vector2( - creatorOffset.x, - creatorOffset.y));
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            creatorOffset = EditorGUILayout.Vector2Field(GUIContent.none, creatorOffset);
            if (GUILayout.Button(new GUIContent("x8", "Create offsets in 8 directions"), GUILayout.Width(25)))
            {
                CreatePresetBorders(configName, GetBorderVectors(8, angleOffset));
            }
            if (GUILayout.Button(new GUIContent("x12", "Create offsets 12 directions"), GUILayout.Width(30)))
            {
                CreatePresetBorders(configName, GetBorderVectors(12, angleOffset));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            var lw = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 75;
            angleOffset = EditorGUILayout.FloatField("Angle Offset", angleOffset);
            EditorGUIUtility.labelWidth = 40;
            steps = EditorGUILayout.IntField("Count", steps);
            EditorGUIUtility.labelWidth = lw;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("Apply", "Create offsets according to the inputted values"), GUILayout.Width(58)))
            {
                CreatePresetBorders(configName, GetBorderVectors(steps, angleOffset));
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private Vector2[] GetBorderVectors(int count, float angleOffsetDeg = 0)
        {
            Vector2[] directions = new Vector2[count];

            Vector2 norm = creatorOffset.normalized;
            float mag = creatorOffset.magnitude;
            float step = (2 * Mathf.PI) / count;
            float angleOffset = Mathf.Deg2Rad * angleOffsetDeg;

            for (int i = 0; i < count; i++)
            {
                float angle = angleOffset + i * step;
                float x = mag * norm.x * Mathf.Cos(angle);
                float y = mag * norm.y * Mathf.Sin(angle);

                directions[i] = new Vector2(x, y);
            }

            return directions;
        }

        void CreatePresetBorders(string configName, params Vector2[] borderValues)
        {
            Undo.RecordObject(border, "Apply Border Offset Preset");

            var offsets = border.GetSettings(configName).Offsets;
            offsets.Clear();

            foreach (var b in borderValues)
            {
                offsets.Add(new Vector2SizeModifier(b, -600 * Vector2.one, 600 * Vector2.one));
            }

            if (offsetListDrawers.TryGetValue(configName, out var list))
            {
                list.index = -1;
                selectedOffsets[configName] = 0;
            }

            border.SetDirty();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private void DrawPresetCreatorButton(string configName, Rect controlRect)
        {
            Rect rect = new Rect(controlRect.xMax - 22, controlRect.y + 5, 20, 16);
            displayCreator.TryGetValue(configName, out var display);
            displayCreator[configName] = GUI.Toggle(rect, display, new GUIContent("♣", "Opens tool to easily create borders."), EditorStyles.miniButton);
        }

        private void DrawOffsetList(string configName, SerializedProperty offsets)
        {
            var settings =  border.GetSettings(configName);
            ReorderableList listDrawer;
            if (!offsetListDrawers.TryGetValue(configName, out listDrawer))
            {
                listDrawer = new ReorderableList(offsets.serializedObject, offsets, true, true, true, true);
                listDrawer.drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Offsets");
                };

                listDrawer.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    var item = settings.Offsets[index];
                    var element = offsets.GetArrayElementAtIndex(index);
                    var optSize = element.FindPropertyRelative("OptimizedSize");

                    Rect r = rect;
                    r.width = 20;
                    EditorGUI.LabelField(r, $"{index + 1}.", EditorStyles.miniBoldLabel);
                    r.x += r.width;
                    r.width = 120;
                    EditorGUI.PropertyField(r, optSize, GUIContent.none);
                    r.x += r.width + 20;
                    EditorGUI.LabelField(r, item.LastCalculatedSize.ToString(), EditorStyles.boldLabel);
                };

                listDrawer.onSelectCallback = (o) =>
                {
                    selectedOffsets[configName] = o.index + 1;
                };

                listDrawer.onRemoveCallback = (o) =>
                {
                    selectedOffsets[configName] = 0;
                    offsets.DeleteArrayElementAtIndex(o.index);
                };

                offsetListDrawers.Add(configName, listDrawer);
            }

            Rect cr = EditorGUILayout.GetControlRect(false, listDrawer.GetHeight());
            if (Event.current.type == EventType.Repaint)
            {
                lastListRects[configName] = cr;
            }

            if (lastListRects.TryGetValue(configName, out var lastListRect))
            {
                // stupid unity layouting requires the lastListRects-Hack which throws the first frame.
                try
                {
                    listDrawer.DoList(lastListRect);
                }
                catch (ArgumentException) { }
            }

            DisplaySelectedOffsetElement(configName, offsets);
        }

        private void DisplaySelectedOffsetElement(string configName, SerializedProperty offsets)
        {
            if (!selectedOffsets.TryGetValue(configName, out int numbering))
                return;

            var currentOffsets = border.GetSettings(configName).Offsets;

            if (numbering <= 0 || numbering > currentOffsets.Count)
                return;

            int index = numbering - 1;
            var element = offsets.GetArrayElementAtIndex(index);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"{numbering}. Offset", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("^", EditorStyles.miniButton, GUILayout.Width(18)))
            {
                selectedOffsets[configName] = 0;
            }

            EditorGUILayout.EndHorizontal();

            try
            {
                EditorGUILayout.PropertyField(element);
            }
            catch (ArgumentException)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        [MenuItem("CONTEXT/Shadow/♠ Make Better")]
        public static void MakeBetter(MenuCommand command)
        {
            Shadow effect = command.context as Shadow;
            bool isOutline = effect is Outline; // Outline derives from Shadow
            Vector2 distance = effect.effectDistance; // TODO: calculate value
            bool useGraphicAlpha = effect.useGraphicAlpha;
            Color32 color = effect.effectColor;

            // as Better Border doesn't derive from Shadow, we are sure that border is "border" will be a BetterBorder.
            var border = Betterizer.MakeBetter<BaseMeshEffect, BetterBorder>(effect) as BetterBorder;
            border.UseGraphicAlpha = useGraphicAlpha;
            border.PrimaryColor = color;
            border.SecondColor = color;

            border.CurrentBorderSettings.Offsets.Clear();

            var b1 = new Vector2SizeModifier(Vector2.zero, -600 * Vector2.one, 600 * Vector2.one);
            b1.SetSize(border, distance);
            border.CurrentBorderSettings.Offsets.Add(b1);

            if (isOutline)
            {
                var b2 = new Vector2SizeModifier(Vector2.zero, -600 * Vector2.one, 600 * Vector2.one);
                b2.SetSize(border, new Vector2(distance.x, -distance.y));
                border.CurrentBorderSettings.Offsets.Add(b2);

                var b3 = new Vector2SizeModifier(Vector2.zero, -600 * Vector2.one, 600 * Vector2.one);
                b3.SetSize(border, new Vector2(-distance.x, distance.y));
                border.CurrentBorderSettings.Offsets.Add(b3);

                var b4 = new Vector2SizeModifier(Vector2.zero, -600 * Vector2.one, 600 * Vector2.one);
                b4.SetSize(border, new Vector2(-distance.x, -distance.y));
                border.CurrentBorderSettings.Offsets.Add(b4);
            }
        }


    }
}
