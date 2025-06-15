using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
    public class SmartParentWindow : EditorWindow
    {
        bool isFreeMovementEnabled;
        bool scheduleFreeMovementToggle;

        RectTransform selection;
        RectTransformData previousTransform;


        Texture2D snapAllPic, snapVerticalPic, snapHorizontalPic, freeParentModeOnPic, freeParentModeOffPic;
        GUIContent snapAllContent, snapVerticalContent, snapHorizontalContent, freeParentModeOnContent, freeParentModeOffContent;

        public static bool IsFreeMovementEnabled 
        {
            get { return GetWindow().isFreeMovementEnabled; } 
            set {
                var wnd = GetWindow();
                if (wnd.isFreeMovementEnabled == value)
                    return;

                wnd.scheduleFreeMovementToggle = true;
            } 
        }
        
        private static SmartParentWindow GetWindow()
        {
            return EditorWindow.GetWindow(typeof(SmartParentWindow), false, "Smart Parent") as SmartParentWindow;
        }

        [MenuItem("Tools/Better UI/Smart Parent", false, 61)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(SmartParentWindow), false, "Smart Parent");
        }

        void OnEnable()
        {
            minSize = new Vector2(195, 245);
            isFreeMovementEnabled = false;

            snapAllPic = Resources.Load<Texture2D>("snap_to_childs_all");
            snapHorizontalPic = Resources.Load<Texture2D>("snap_to_childs_h");
            snapVerticalPic = Resources.Load<Texture2D>("snap_to_childs_v");
            freeParentModeOnPic = Resources.Load<Texture2D>("free_parent_mode_on");
            freeParentModeOffPic = Resources.Load<Texture2D>("free_parent_mode_off");

            snapAllContent = new GUIContent(snapAllPic, "Trims size to children horizontally and vertically. Also snap Anchors to borders.");
            snapVerticalContent = new GUIContent(snapVerticalPic, "Trims size to children vertically. Also snap Anchors to borders vertically.");
            snapHorizontalContent = new GUIContent(snapHorizontalPic, "Trims size to children horizontally. Also snap Anchors to borders horizontally.");
            freeParentModeOnContent = new GUIContent(freeParentModeOnPic, "When this mode is enabled children are not moved along with the parent.");
            freeParentModeOffContent = new GUIContent(freeParentModeOffPic, "When this mode is enabled children are not moved along with the parent.");

            Selection.selectionChanged += SelectionChanged;
            EditorApplication.update += UpdateTransforms;

            SelectionChanged();
        }

        void OnDisable()
        {
            isFreeMovementEnabled = false;

            Selection.selectionChanged -= SelectionChanged;
            EditorApplication.update -= UpdateTransforms;
        }


        void OnGUI()
        {
            EditorGUILayout.Space();

            var go = Selection.activeObject as GameObject;
            bool canSelectParent = Selection.objects.Length == 1
                && go != null
                && go.transform as RectTransform != null
                && go.transform.parent != null;

            if (canSelectParent)
            {
                if (GUILayout.Button("Select Parent", EditorStyles.miniButton))
                {
                    Selection.activeObject = go.transform.parent.gameObject;
                }
            }
            else
            {
                GUILayout.Label("");
            }

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(selection == null);

            if (selection != null)
            {
                EditorGUILayout.LabelField(selection.name, EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                DrawEmphasisedLabel("No valid object selected.");
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            #region snap all
            if (GUILayout.Button(snapAllContent, GUILayout.Width(120), GUILayout.Height(120)))
            {
                SnapToChildren(true, true);
            }
            #endregion

            #region snap vertically

            if (GUILayout.Button(snapVerticalContent, GUILayout.Width(60), GUILayout.Height(120)))
            {
                SnapToChildren(false, true);
            }

            #endregion

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            #region snap horizontally
            if (GUILayout.Button(snapHorizontalContent, GUILayout.Width(120), GUILayout.Height(60)))
            {
                SnapToChildren(true, false);
            }
            #endregion

            EditorGUI.EndDisabledGroup();

            #region free parent mode

            bool prev = isFreeMovementEnabled;
            var content = (prev) ? freeParentModeOnContent : freeParentModeOffContent;
            isFreeMovementEnabled = GUILayout.Toggle(isFreeMovementEnabled, content, "Button", GUILayout.Width(60), GUILayout.Height(60));

            if(scheduleFreeMovementToggle)
            {
                isFreeMovementEnabled = !isFreeMovementEnabled;
                scheduleFreeMovementToggle = false;
            }

            bool turnedOn = !prev && isFreeMovementEnabled;
            if (turnedOn && selection != null)
            {
                previousTransform = new RectTransformData(selection);
            }

            #endregion

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (isFreeMovementEnabled && selection != null)
            {
                DrawEmphasisedLabel("Children are detached.");
            }
        }


        private static void DrawEmphasisedLabel(string text)
        {
            GUIStyle warn = GUI.skin.GetStyle("WarningOverlay");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label(text, warn);
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
        }

        private void SelectionChanged()
        {
            var sel = Selection.GetFiltered(typeof(RectTransform), SelectionMode.TopLevel);

            if (sel.Length != 1)
            {
                selection = null;
                this.Repaint();
                return;
            }

            var rt = sel[0] as RectTransform;
            if(rt.childCount == 0 || rt.parent == null)
            {
                selection = null;
                this.Repaint();
                return;
            }

            if (rt  == selection)
                return;

            selection = rt;
            previousTransform = new RectTransformData(selection);

            this.Repaint();
        }

        private void UpdateTransforms()
        {
            if (!isFreeMovementEnabled || selection == null)
                return;

            RectTransformData currentTransform = new RectTransformData(selection);
            selection.MoveChildsToRetainPreviousLocations(currentTransform, previousTransform);
            previousTransform = currentTransform;
        }

        private void SnapToChildren(bool snapHorizontally, bool snapVertically)
        {
            selection.SnapSizeAndAnchorsToChildren(snapHorizontally, snapVertically,
                retainChildLocations: !isFreeMovementEnabled, recordUndo: true);
        }
    }
}
