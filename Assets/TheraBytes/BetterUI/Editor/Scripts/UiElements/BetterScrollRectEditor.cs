using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UI;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

namespace TheraBytes.BetterUi.Editor
{
    [CustomEditor(typeof(BetterScrollRect)), CanEditMultipleObjects]
    public class BetterScrollRectEditor : ScrollRectEditor
    {
        static GUIContent startFromCurrentValueContent;
        static GUIContent StartFromCurrentValueContent 
        { 
            get
            {
                if (startFromCurrentValueContent == null)
                {
                    startFromCurrentValueContent = new GUIContent(" ↓", "Set value to current scrollbar value");
                }

                return startFromCurrentValueContent;
            }
        }

        SerializedProperty hProp, vProp;
        SerializedProperty hSpacingFallback, hSpacingCollection;
        SerializedProperty vSpacingFallback, vSpacingCollection;

        SerializedProperty alwaysKeepSelectionInView;
        SerializedProperty scrollToSelectionDuration;

        SerializedProperty keepInViewPaddingFallback;
        SerializedProperty customKeepInViewPaddingSizers;

        SerializedProperty scrollSesitivityFallback;
        SerializedProperty customscrollSesitivitySizers;

        SerializedProperty horizontalScrollbar;
        SerializedProperty verticalScrollbar;

        bool foldout = true;

        protected override void OnEnable()
        {
            base.OnEnable();

            hProp = serializedObject.FindProperty("horizontalStartPosition");
            vProp = serializedObject.FindProperty("verticalStartPosition");

            hSpacingFallback = serializedObject.FindProperty("horizontalSpacingFallback");
            hSpacingCollection = serializedObject.FindProperty("customHorizontalSpacingSizers");

            vSpacingFallback = serializedObject.FindProperty("verticalSpacingFallback");
            vSpacingCollection = serializedObject.FindProperty("customVerticalSpacingSizers");

            alwaysKeepSelectionInView = serializedObject.FindProperty("alwaysKeepSelectionInView");
            scrollToSelectionDuration = serializedObject.FindProperty("scrollToSelectionDuration");

            keepInViewPaddingFallback = serializedObject.FindProperty("keepInViewPaddingFallback");
            customKeepInViewPaddingSizers = serializedObject.FindProperty("customKeepInViewPaddingSizers");

            scrollSesitivityFallback = serializedObject.FindProperty("scrollSensitivityFallback");
            customscrollSesitivitySizers = serializedObject.FindProperty("scrollSensitivitySizers");

            horizontalScrollbar = serializedObject.FindProperty("m_HorizontalScrollbar");
            verticalScrollbar = serializedObject.FindProperty("m_VerticalScrollbar");

        }


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, "Better UI");
            if(foldout)
            {
                DrawBetterUiProperties();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawBetterUiProperties()
        {
            BetterScrollRect obj = target as BetterScrollRect;

            EditorGUILayout.PropertyField(alwaysKeepSelectionInView);
            if(alwaysKeepSelectionInView.boolValue)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(scrollToSelectionDuration);
                ScreenConfigConnectionHelper.DrawSizerGui("Keep In View Padding", customKeepInViewPaddingSizers, ref keepInViewPaddingFallback);

                EditorGUI.indentLevel--;
            }


            bool isNavigationEnabled = 
                (obj.horizontal && obj.horizontalScrollbar != null && obj.horizontalScrollbar.navigation.mode != Navigation.Mode.None)
                || (obj.vertical && obj.verticalScrollbar != null && obj.verticalScrollbar.navigation.mode != Navigation.Mode.None);

            if(alwaysKeepSelectionInView.boolValue == isNavigationEnabled)
            {
                if(isNavigationEnabled)
                {
                    if (GUILayout.Button("Disable Scrollbar Navigation"))
                    {
                        var none = new Navigation() { mode = Navigation.Mode.None };
                        SetScrollbarNavigation(obj.horizontalScrollbar, none);
                        SetScrollbarNavigation(obj.verticalScrollbar, none);
                    }
                }
                else
                {
                    if (GUILayout.Button("Enable Scrollbar Navigation"))
                    {
                        var auto = new Navigation() { mode = Navigation.Mode.Automatic };
                        SetScrollbarNavigation(obj.horizontalScrollbar, auto);
                        SetScrollbarNavigation(obj.verticalScrollbar, auto);
                    }
                }

            }

            EditorGUILayout.Space();

            ScreenConfigConnectionHelper.DrawSizerGui("Scroll Sensitivity", customscrollSesitivitySizers, ref scrollSesitivityFallback);

            DrawStartPosition(obj.horizontal, hProp, horizontalScrollbar);
            DrawStartPosition(obj.vertical, vProp, verticalScrollbar);

            if (obj.horizontal)
            {
                if(obj.horizontalScrollbarVisibility == ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport)
                {
                    ScreenConfigConnectionHelper.DrawSizerGui("Horizontal Scrollbar Spacing", hSpacingCollection, ref hSpacingFallback);

                    EditorGUILayout.Separator();
                }
            }

            if(obj.vertical)
            {
                if (obj.verticalScrollbarVisibility == ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport)
                {
                    ScreenConfigConnectionHelper.DrawSizerGui("Vertical Scrollbar Spacing", vSpacingCollection, ref vSpacingFallback);
                }
            }
        }

        private static void SetScrollbarNavigation(Scrollbar scrollbar, Navigation navigation)
        {
            if (scrollbar != null)
            {
                scrollbar.navigation = navigation;
            }
        }

        void DrawStartPosition(bool isEnabled, SerializedProperty scrollStartProp, SerializedProperty scrollbarProp)
        {
            if (!isEnabled)
                return;

            float prev = scrollStartProp.floatValue;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(scrollStartProp);

            if (scrollbarProp.objectReferenceValue != null 
                && GUILayout.Button(StartFromCurrentValueContent, GUILayout.Width(19)))
            {
                scrollStartProp.floatValue = (scrollbarProp.objectReferenceValue as Scrollbar).value;
            }

            EditorGUILayout.EndHorizontal();

            if (prev == scrollStartProp.floatValue)
                return;

            if (scrollbarProp.objectReferenceValue != null)
            {
                var so = new SerializedObject(scrollbarProp.objectReferenceValue);
                var valueProp = so.FindProperty("m_Value");
                valueProp.floatValue = scrollStartProp.floatValue;
                so.ApplyModifiedProperties();
            }
        }

        [MenuItem("CONTEXT/ScrollRect/♠ Make Better")]
        public static void MakeBetter(MenuCommand command)
        {
            ScrollRect obj = command.context as ScrollRect;
            float hSpace = obj.horizontalScrollbarSpacing;
            float vSpace = obj.verticalScrollbarSpacing;
            float sensitivity = obj.scrollSensitivity;

            var newScrollRect = Betterizer.MakeBetter<ScrollRect, BetterScrollRect>(obj);
            var betterVersion = newScrollRect as BetterScrollRect;
            if(betterVersion != null)
            {
                betterVersion.HorizontalSpacingSizer.SetSize(betterVersion, hSpace);
                betterVersion.VerticalSpacingSizer.SetSize(betterVersion, vSpace);
                betterVersion.ScrollSensitivitySizer.SetSize(betterVersion, sensitivity);
            }
        }
    }
}
