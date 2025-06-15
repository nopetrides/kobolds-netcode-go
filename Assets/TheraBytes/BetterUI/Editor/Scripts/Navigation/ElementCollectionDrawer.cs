using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TheraBytes.BetterUi.Editor
{
    public class NavigationGroupCollectionDrawer : ElementCollectionDrawer
    {

        static GUIContent rememberPreviousContent = new GUIContent("Remember Previous Navigation Group",
            "If checked, the Navigation Group that was previously focused will be focused when the controller is focused. Otherwise, the Navigation Group is determined by the configuration below.");

        static GUIContent initialElementContent = new GUIContent("Initial Navigation Group", "The Navigation Group which is Focused the first time the controller is activated.");

        static GUIContent elementOnFocusContent = new GUIContent("Navigation Group On Focus", "The Navigation Group which is focused every time the controller is activated.");

        protected override GUIContent RememberPreviousContent { get { return rememberPreviousContent; } }
        protected override GUIContent InitialElementContent { get { return initialElementContent; } }
        protected override GUIContent ElementOnFocusContent { get { return elementOnFocusContent; } }

        public NavigationGroupCollectionDrawer(SerializedProperty parentField) : base(parentField)
        { }

        public void Draw()
        {
            base.Draw(true);
        }
    }

    public class SelectableCollectionDrawer : ElementCollectionDrawer
    {

        static GUIContent rememberPreviousContent = new GUIContent("Remember Previous Selectable",
            "If checked, the selectable that was previously selected will be selected when the group is focused again. Otherwise, the Selectable is determined by the configuration below.");

        static GUIContent initialElementContent = new GUIContent("Initial Selectable", "The selectable which is selected the first time the group is focused.");

        static GUIContent elementOnFocusContent = new GUIContent("Selectable On Focus", "The selectable which is selected every time the group is focused (unless temporarily overridden by a navigation controller).");

        protected override GUIContent RememberPreviousContent { get { return rememberPreviousContent; } }
        protected override GUIContent InitialElementContent { get { return initialElementContent; } }
        protected override GUIContent ElementOnFocusContent { get { return elementOnFocusContent; } }

        public SelectableCollectionDrawer(SerializedProperty parentField) : base(parentField)
        { }

        public void Draw()
        {
            base.Draw(false);
        }
    }

    public abstract class ElementCollectionDrawer
    {
        SerializedProperty parentField;

        SerializedProperty rememberPrevious;
        SerializedProperty initialFocused;
        SerializedProperty initialElement;
        SerializedProperty relativeCoordinate;
        SerializedProperty collectingStrategy;
        SerializedProperty elements;

        protected abstract GUIContent RememberPreviousContent { get; }
        protected abstract GUIContent InitialElementContent { get; }
        protected abstract GUIContent ElementOnFocusContent { get; }

        public ElementCollectionDrawer(SerializedProperty parentField)
        {
            this.parentField = parentField;
            rememberPrevious = parentField.FindPropertyRelative("rememberPrevious");
            initialFocused = parentField.FindPropertyRelative("initialFocused");
            initialElement = parentField.FindPropertyRelative("initialElement");
            relativeCoordinate = parentField.FindPropertyRelative("relativeCoordinate");
            collectingStrategy = parentField.FindPropertyRelative("collectingStrategy");
            elements = parentField.FindPropertyRelative("elements");
        }

        protected void Draw(bool supportPriority)
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField(parentField.displayName, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;


            EditorGUILayout.PropertyField(rememberPrevious, RememberPreviousContent);

            var label = rememberPrevious.boolValue ? InitialElementContent : ElementOnFocusContent;
            EditorGUILayout.PropertyField(initialFocused, label);

            SelectionOnFocus state = (SelectionOnFocus)initialFocused.intValue;
            switch (state)
            {
                case SelectionOnFocus.Specific:
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(initialElement);
                    EditorGUI.indentLevel--;
                    break;
                case SelectionOnFocus.ClosestToCoordinate:
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(relativeCoordinate);
                    EditorGUI.indentLevel--;
                    break;
                case SelectionOnFocus.HighestPriority:
                    if(!supportPriority)
                    {
                        initialFocused.intValue = (int)SelectionOnFocus.FirstInHierarchy;
                    }
                    break;
            }


            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(collectingStrategy);

            if(EditorApplication.isPlaying)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(elements);
                EditorGUI.EndDisabledGroup();
            }
            else if((CollectingElementsStrategy)collectingStrategy.intValue == CollectingElementsStrategy.FixedSet)
            {
                EditorGUILayout.PropertyField(elements);
            }
            

            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
        }

    }
}
