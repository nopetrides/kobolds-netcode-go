using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TheraBytes.BetterUi;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{

    public class SnapAnchorsWindow : EditorWindow
    {
        const string HighlightColor = "#0ef05d";
        private const int TotalControlWidth = 180;

        public enum AnchorMode
        {
            Border,
            Point,
        }

        List<RectTransform> objects;

        AnchorMode mode = AnchorMode.Border;
        bool parentPosition;
        Vector2 point = new Vector2(0.5f, 0.5f);

        Texture2D allBorderPic, verticalBorderPic, horizontalBorderPic, matchParentPic, 
            pointPic, verticalPointPic, horizontalPointPic;

        GUIStyle setPivotStyle, selectPointStyle;

        [MenuItem("Tools/Better UI/Snap Anchors", false, 60)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(SnapAnchorsWindow), false, "Snap Anchors");
        }

        void OnEnable()
        {
            minSize = new Vector2(195, 310);
            setPivotStyle = null;
            selectPointStyle = null;

            Selection.selectionChanged += this.Repaint;

            allBorderPic = Resources.Load<Texture2D>("snap_all_edges");
            pointPic = Resources.Load<Texture2D>("snap_all_direction_point");
            horizontalPointPic = Resources.Load<Texture2D>("snap_horizontal_point");
            verticalPointPic = Resources.Load<Texture2D>("snap_vertical_point");
            horizontalBorderPic = Resources.Load<Texture2D>("snap_horizontal_edges");
            verticalBorderPic = Resources.Load<Texture2D>("snap_vertical_edges");
            matchParentPic = Resources.Load<Texture2D>("snap_to_parent");
        }

        void OnGUI()
        {
            #region init styles
            if(setPivotStyle == null)
            {
                setPivotStyle = new GUIStyle(EditorStyles.miniButton);
                setPivotStyle.richText = true;
                setPivotStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (selectPointStyle == null)
            {
                selectPointStyle = new GUIStyle(EditorStyles.helpBox);
                selectPointStyle.margin = new RectOffset(0, 0, 0, 0);
                selectPointStyle.richText = true;
                selectPointStyle.alignment = TextAnchor.MiddleCenter;
            }

            #endregion

            objects = Selection.gameObjects
                .Where((o) => o.transform is RectTransform)
                .Select((o) => o.transform as RectTransform)
                .ToList();


            EditorGUILayout.Space();
            DrawModeSelection();
            EditorGUILayout.Space();

            bool active = objects.Count > 0;
            if (!(active))
            {
                EditorGUI.BeginDisabledGroup(true);
            }

            if (objects.Count > 0)
            {
                string txt = (objects.Count == 1) ? objects[0].name : string.Format("{0} UI Elements", objects.Count);
                EditorGUILayout.LabelField(txt, EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                GUIStyle warn = GUI.skin.GetStyle("WarningOverlay");
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(5);
                GUILayout.TextArea("No UI Element selected.", warn);
                GUILayout.Space(5);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();


            switch (mode)
            {
                case AnchorMode.Border:
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button(new GUIContent(allBorderPic, "Snap to all borders"), GUILayout.Width(120), GUILayout.Height(120)))
                            SnapBorder(left: true, right: true, top: true, bottom: true);


                        // TOP DOWN
                        if (GUILayout.Button(new GUIContent(verticalBorderPic, "Snap to top and bottom border"), GUILayout.Width(60), GUILayout.Height(120)))
                            SnapBorder(left: false, right: false, top: true, bottom: true);

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        // LEFT RIGHT
                        if (GUILayout.Button(new GUIContent(horizontalBorderPic, "Snap to left and right border"), GUILayout.Width(120), GUILayout.Height(60)))
                            SnapBorder(left: true, right: true, top: false, bottom: false);

                       // EditorGUILayout.LabelField("", GUILayout.Width(60));

                        if(GUILayout.Button(new GUIContent(matchParentPic, "Resize to the size of parent and set the anchors to the borders."), GUILayout.Width(60), GUILayout.Height(60)))
                        {
                            MatchParent();
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.Space();


                        if (!(active))
                            EditorGUI.EndDisabledGroup();

                    }
                    break;

                case AnchorMode.Point:

                    DrawPointButtons();

                    if (!(active))
                        EditorGUI.EndDisabledGroup();

                    // Use Parent Space
                    GUILayout.Space(-10); // move upwards a bit since there is empty space.
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    parentPosition = EditorGUILayout.ToggleLeft("Use Parent Space", parentPosition, GUILayout.Width(120));
                    GUILayout.Space(60);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();

                    // Snap Point
                    EditorGUIUtility.labelWidth = 70;
                    var wide = EditorGUIUtility.wideMode;
                    EditorGUIUtility.wideMode = true;

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    point = EditorGUILayout.Vector2Field("Snap Point", point, GUILayout.Width(TotalControlWidth + 55));
                    GUILayout.Space(-55); // the Vector2 Field leaves empty space at the right side for some reason

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    EditorGUIUtility.wideMode = wide;
                    EditorGUIUtility.labelWidth = 0; // <- reset to default

                    // Set Pivot
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    string btnText = string.Format("Set Pivot to <color={0}>({1:f}, {2:f})</color>", 
                        HighlightColor, point.x, point.y);

                    if (GUILayout.Button(btnText, setPivotStyle, GUILayout.Width(TotalControlWidth)))
                    {
                        Undo.RecordObjects(objects.Select(o => o as UnityEngine.Object).ToArray(), "set pivots");
                        foreach(var obj in objects)
                        {
                            obj.SetPivot(point);
                        }
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    break;

                default:
                    break;
            }

            EditorGUILayout.Space();
        }

        private void MatchParent()
        {
            Undo.RecordObjects(objects.ToArray(), "Match Parent" + DateTime.Now.ToFileTime());
            foreach (RectTransform obj in objects)
            {
                obj.anchorMin = Vector2.zero;
                obj.anchorMax = Vector2.one;
                obj.anchoredPosition = Vector2.zero;
                obj.sizeDelta = Vector2.zero;
            }
        }

        private void SetPoint(float x, float y)
        {
            point = new Vector2(x, y);
        }

        void DrawPointButtons()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent(pointPic, "Snap all directions to position"), GUILayout.Width(120), GUILayout.Height(100)))
                SnapPoint(horizontal: true, vertical: true);

            // TOP DOWN
            if (GUILayout.Button(new GUIContent(verticalPointPic, "Snap vertically to position"), GUILayout.Width(60), GUILayout.Height(100)))
                SnapPoint(horizontal: false, vertical: true);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // LEFT RIGHT
            if (GUILayout.Button(new GUIContent(horizontalPointPic, "Snap horizontally to position"), GUILayout.Width(120), GUILayout.Height(60)))
                SnapPoint(horizontal: true, vertical: false);


            EditorGUILayout.BeginVertical();
            // const string style = "Label";
            var style = selectPointStyle;
            EditorGUILayout.BeginHorizontal();
            DrawSelectionPoint("┌", style, 0f, 1f);
            DrawSelectionPoint("┬", style, 0.5f, 1f);
            DrawSelectionPoint("┐", style, 1f, 1f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawSelectionPoint("├", style, 0f, 0.5f);
            DrawSelectionPoint("┼", style, 0.5f, 0.5f);
            DrawSelectionPoint("┤", style, 1f, 0.5f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawSelectionPoint("└", style, 0f, 0f);
            DrawSelectionPoint("┴", style, 0.5f, 0f);
            DrawSelectionPoint("┘", style, 1f, 0f);
            EditorGUILayout.EndHorizontal();

            if (this.objects.Count == 1)
            {
                var p = this.objects[0].pivot;
                string content = "[ Pivot ]";
                content = HighlightTextIfMatchCoordinate(content, p.x, p.y);
                if (GUILayout.Button(content, style, GUILayout.Width(60), GUILayout.Height(16)))
                {
                    SetPoint(p.x, p.y);
                }
            }
            else
            {
                GUILayout.Label("");
            }

            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

        }

        private void DrawSelectionPoint(string content, GUIStyle style, float x, float y)
        {
            const float size = 20;
            content = HighlightTextIfMatchCoordinate(content, x, y);

            if (GUILayout.Button(content, style, GUILayout.Width(size), GUILayout.Height(size)))
            {
                SetPoint(x, y);
            }
        }

        private string HighlightTextIfMatchCoordinate(string content, float x, float y)
        {
            if (point.x == x && point.y == y)
            {
                content = string.Format("<color={0}>{1}</color>", HighlightColor, content);
            }

            return content;
        }

        void DrawModeSelection()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle((mode == AnchorMode.Border), "Border", EditorStyles.miniButtonLeft)
                && (mode != AnchorMode.Border))
            {
                mode = AnchorMode.Border;
            }

            if (GUILayout.Toggle((mode == AnchorMode.Point), "Point", EditorStyles.miniButtonRight)
                && (mode != AnchorMode.Point))
            {
                mode = AnchorMode.Point;
            }

            EditorGUILayout.EndHorizontal();
        }

        void SnapBorder(bool left, bool right, bool top, bool bottom)
        {
            Undo.SetCurrentGroupName("SnapBorder" + DateTime.Now.ToFileTime());
            int group = Undo.GetCurrentGroup();

            foreach (var obj in objects)
            {
                Undo.RecordObject(obj.transform, "Snap Anchors Border");
                obj.SnapAnchorsToBorders(left, right, top, bottom);
            }

            Undo.CollapseUndoOperations(group);
        }

        void SnapPoint(bool horizontal, bool vertical)
        {
            Undo.SetCurrentGroupName("SnapPoint" + DateTime.Now.ToFileTime());
            int group = Undo.GetCurrentGroup();

            foreach (var obj in objects)
            {
                Undo.RecordObject(obj.transform, "Snap Anchors Point");
                obj.SnapAnchorsToPoint(point, parentPosition, horizontal, vertical);
            }

            Undo.CollapseUndoOperations(group);
        }
    }
}
