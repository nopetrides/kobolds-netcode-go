﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor.AlignDistribute
{
    public class AlignDistributeWindow : EditorWindow
    {
        private const float LabelWidth = 100f;

        static GUIStyle rightAlignedLabel;
        static GUIStyle RightAlignedLabel
        {
            get
            {
                if (rightAlignedLabel == null)
                    rightAlignedLabel = new GUIStyle(EditorStyles.label)
                    { alignment = TextAnchor.UpperRight, wordWrap = false };

                return rightAlignedLabel;
            }
        }
        static GUIStyle leftAlignedLabel;
        static GUIStyle LeftAlignedLabel
        {
            get
            {
                if (leftAlignedLabel == null)
                    leftAlignedLabel = new GUIStyle(EditorStyles.label) 
                    { alignment = TextAnchor.LowerLeft, wordWrap = false };

                return leftAlignedLabel;
            }
        }

        static string[] GetNiceEnumNames<T>()
        {
            return Enum.GetNames(typeof(T))
                .Select(o => ObjectNames.NicifyVariableName(o))
                .ToArray();
        }

        private static ActiveWindow activeWindow = ActiveWindow.Align;
        private static AlignTo alignTo = AlignTo.SelectionBounds;
        internal static DistributeTo distributeTo = DistributeTo.SelectionBounds;
        private static DistanceOption distanceOption = DistanceOption.Space;
        private static SortOrder sortOrder;
        internal static AnchorMode anchorMode = AnchorMode.FollowObject;

        private static string[] alignToOptions;
        private static string[] distanceOptions;
        private static string[] anchorModeOptions;
        private static string[] sortOrderOptions;

        private static bool showPadding = true;
        private static float paddingLeftBottomPixels = 0f;
        private static float paddingRightTopPixels = 0f;

        private Texture2D alignLeft, alignCenter, alignRight, alignBottom, alignMiddle, alignTop;
        private Texture2D distributeHorizontal, distributeVertical;

        [MenuItem("Tools/Better UI/Align and Distribute", false, 62)]
        public static void ShowWindow()
        {
            EditorWindow window = GetWindow(typeof(AlignDistributeWindow), false, "Align/Distribute");
            window.minSize = new Vector2(195, 310);
        }

        private void OnEnable()
        {
            alignLeft = Resources.Load<Texture2D>("allign_left");
            alignCenter = Resources.Load<Texture2D>("allign_center");
            alignRight = Resources.Load<Texture2D>("allign_right");
            alignBottom = Resources.Load<Texture2D>("allign_bottom");
            alignMiddle = Resources.Load<Texture2D>("allign_middle");
            alignTop = Resources.Load<Texture2D>("allign_top");

            distributeHorizontal = Resources.Load<Texture2D>("distribute_horizontally");
            distributeVertical = Resources.Load<Texture2D>("distribute_vertically");

            alignToOptions = GetNiceEnumNames<AlignTo>();
            distanceOptions = GetNiceEnumNames<DistanceOption>();
            anchorModeOptions = GetNiceEnumNames<AnchorMode>();
            sortOrderOptions = GetNiceEnumNames<SortOrder>();
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUIUtility.labelWidth = 2f;
            EditorGUILayout.Space();
            DrawModeSelection();
            EditorGUILayout.Space();

            DrawSelectionInfo();

            switch (activeWindow)
            {
                case ActiveWindow.Align:
                    DrawAlignButtons();
                    EditorGUILayout.Space();

                    DrawAlignTo();
                    break;

                case ActiveWindow.Distribute:
                    DrawDistributeButtons();
                    EditorGUILayout.Space();

                    DrawPadding();
                    EditorGUILayout.Space();

                    DrawDistributeTo();
                    EditorGUILayout.Space();

                    DrawOrderOptions();
                    EditorGUILayout.Space();

                    DrawDistanceOptions();
                    break;
            }

            EditorGUILayout.Space();
            DrawAnchorMode();

            EditorGUIUtility.labelWidth = 0f;
        }

        private void DrawModeSelection()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Toggle((activeWindow == ActiveWindow.Align), "Align", EditorStyles.miniButtonLeft))
            {
                activeWindow = ActiveWindow.Align;
            }

            if (GUILayout.Toggle((activeWindow == ActiveWindow.Distribute), "Distribute", EditorStyles.miniButtonRight))
            {
                activeWindow = ActiveWindow.Distribute;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSelectionInfo()
        {
            SelectionStatus selectionStatus = Utility.IsSelectionValid();
            if (selectionStatus != SelectionStatus.Valid)
            {
                DrawInvalidSelectionInfo(selectionStatus);
            }
            else
            {
                Transform[] selection = Selection.transforms;

                string label = (selection.Length == 1) ? selection[0].name : string.Format("{0} UI Elements", selection.Length);
                EditorGUILayout.LabelField(label, EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.Space();
        }

        private void DrawInvalidSelectionInfo(SelectionStatus selectionStatus)
        {
            GUIStyle warn = GUI.skin.GetStyle("WarningOverlay");
            
            EditorGUI.BeginDisabledGroup(true);

            string message;

            switch (selectionStatus)
            {
                case SelectionStatus.NothingSelected:
                    message = "Nothing selected";
                    break;

                case SelectionStatus.ParentIsNull:
                case SelectionStatus.ParentIsNoRectTransform:
                    message = "Objects must be inside a Canvas.";
                    break;

                case SelectionStatus.ContainsNoRectTransform:
                    message = "All objects must have a RectTransform.";
                    break;

                case SelectionStatus.UnequalParents:
                    message = "Objects must have the same parent.";
                    break;

                case SelectionStatus.Valid:
                    // Function should never be called when selection is valid.
                    message = "Unknown problem discovered.";
                    break;

                default:
                    Debug.LogError("Invalid SelectionStatus: " + selectionStatus);
                    throw new ArgumentOutOfRangeException();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.TextArea(message, warn);
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAlignButtons()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent(alignLeft, "Align to the left"), GUILayout.Width(60f), GUILayout.Height(60f)))
            {
                Align.AlignSelection(AlignMode.Left, alignTo);
            }
            if (GUILayout.Button(new GUIContent(alignCenter, "Align to the center"), GUILayout.Width(60f), GUILayout.Height(60f)))
            {
                Align.AlignSelection(AlignMode.Vertical, alignTo);
            }
            if (GUILayout.Button(new GUIContent(alignRight, "Align to the right"), GUILayout.Width(60f), GUILayout.Height(60f)))
            {
                Align.AlignSelection(AlignMode.Right, alignTo);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent(alignTop, "Align to the top"), GUILayout.Width(60f), GUILayout.Height(60f)))
            {
                Align.AlignSelection(AlignMode.Top, alignTo);
            }
            if (GUILayout.Button(new GUIContent(alignMiddle, "Align to the middle"), GUILayout.Width(60f), GUILayout.Height(60f)))
            {
                Align.AlignSelection(AlignMode.Horizontal, alignTo);
            }
            if (GUILayout.Button(new GUIContent(alignBottom, "Align to the bottom"), GUILayout.Width(60f), GUILayout.Height(60f)))
            {
                Align.AlignSelection(AlignMode.Bottom, alignTo);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDistributeButtons()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent(distributeHorizontal, "Distribute horizontally"), GUILayout.Width(60f), GUILayout.Height(60f)))
            {
                if (Selection.GetTransforms(SelectionMode.Unfiltered).Length > 1)
                {
                    Distribute.DistributeSelection(AlignMode.Horizontal, distanceOption, sortOrder,
                        paddingLeftBottomPixels, paddingRightTopPixels);
                }
                else
                {
                    Align.AlignSelection(AlignMode.Horizontal, AlignTo.Parent);
                }
            }
            if (GUILayout.Button(new GUIContent(distributeVertical, "Distribute vertically"), GUILayout.Width(60f), GUILayout.Height(60f)))
            {
                if (Selection.GetTransforms(SelectionMode.Unfiltered).Length > 1)
                {
                    Distribute.DistributeSelection(AlignMode.Vertical, distanceOption, sortOrder,
                        paddingLeftBottomPixels, paddingRightTopPixels);
                }
                else
                {
                    Align.AlignSelection(AlignMode.Vertical, AlignTo.Parent);
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDistanceOptions()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Align by", GUILayout.Width(LabelWidth));
            distanceOption = (DistanceOption) EditorGUILayout.Popup((int)distanceOption, distanceOptions);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPadding()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical();
                if (showPadding)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Padding", GUILayout.Width(LabelWidth));
                    paddingRightTopPixels = EditorGUILayout.FloatField(paddingRightTopPixels);
                    EditorGUILayout.LabelField("═╗", RightAlignedLabel, GUILayout.Width(13));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(LabelWidth - 12);
                    EditorGUILayout.LabelField("╚═", LeftAlignedLabel, GUILayout.Width(14));
                    paddingLeftBottomPixels = EditorGUILayout.FloatField(paddingLeftBottomPixels);
                    GUILayout.Space(13);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawOrderOptions()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Sorting Order", GUILayout.Width(LabelWidth));
            sortOrder = (SortOrder)EditorGUILayout.Popup((int)sortOrder, sortOrderOptions);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDistributeTo()
        {
            const float selectionBoundsWidth = 120;
            const float parentWidth = 68;
            float totalWidth = LabelWidth + selectionBoundsWidth + parentWidth;
            bool fitsInOneLine = totalWidth < base.position.width;

            if (fitsInOneLine)
            {
                EditorGUILayout.BeginHorizontal();
            }

            EditorGUILayout.LabelField("Distribute along", GUILayout.Width(LabelWidth));

            if (!fitsInOneLine)
            {
                EditorGUILayout.BeginHorizontal();
            }

            if (GUILayout.Toggle(distributeTo == DistributeTo.SelectionBounds, " Selection Bounds", "Radio",
                GUILayout.Width(selectionBoundsWidth)))
            {
                distributeTo = DistributeTo.SelectionBounds;
            }

            if (GUILayout.Toggle(distributeTo == DistributeTo.Parent, " Parent", "Radio",
                GUILayout.Width(parentWidth)))
            {
                distributeTo = DistributeTo.Parent;
            }

            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAlignTo()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Align to", GUILayout.Width(LabelWidth));
            alignTo = (AlignTo)EditorGUILayout.Popup((int)alignTo, alignToOptions);

            EditorGUILayout.EndHorizontal();
        }


        private void DrawAnchorMode()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Anchors", GUILayout.Width(LabelWidth));
            anchorMode = (AnchorMode)EditorGUILayout.Popup((int)anchorMode, anchorModeOptions);

            EditorGUILayout.EndHorizontal();
        }
    }
}
