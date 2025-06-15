using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheraBytes.BetterUi.Editor
{
    public class BetterNavigationDebugInfoWindow : EditorWindow
    {
        [MenuItem("Tools/Better UI/Debug/Navigation", false, 120)]
        public static void ShowWindow()
        {
            var win = EditorWindow.GetWindow<BetterNavigationDebugInfoWindow>("Better Navigation Debug Info") as BetterNavigationDebugInfoWindow;
            win.Show();
        }

        Vector2 scroll;

        void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Data is updated during play mode. You are not in play mode.", MessageType.Info);
            }

            EditorGUILayout.ObjectField("Current Better Navigation", 
                BetterNavigation.Current, typeof(BetterNavigation), true);

            EditorGUILayout.ObjectField("Focused Navigation Group",
                NavigationGroup.Current, typeof(NavigationGroup), true);

            EditorGUILayout.Space();
            EditorGUILayout.ObjectField("Last Remembered Selection",
                BetterNavigation.LastSelection, typeof(Selectable), true);

            EditorGUILayout.ObjectField("Current Selection",
                EventSystem.current?.currentSelectedGameObject, typeof(GameObject), true);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField($"Active Navigation Controllers:\t\t{NavigationController.ActiveControllers.Count}");
            
            EditorGUI.indentLevel++;
            foreach (var ctrl in NavigationController.ActiveControllers)
            {
                EditorGUILayout.ObjectField(ctrl, typeof(NavigationController), true);
            }
            
            EditorGUI.indentLevel--;

            var inactiveControllers = NavigationController.AllControllers
                .Where(o => !NavigationController.ActiveControllers.Contains(o));

            EditorGUILayout.LabelField($"Inactive Navigation Controllers:\t\t{inactiveControllers.Count()}");


            EditorGUI.indentLevel++;
            foreach (var ctrl in inactiveControllers)
            {
                EditorGUILayout.ObjectField(ctrl, typeof(NavigationController), true);
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Active Button Interaction Handlers:\t{ButtonInteractionHandler.ActiveHandlers.Count}");

            EditorGUI.indentLevel++;
            foreach (var handler in ButtonInteractionHandler.ActiveHandlers)
            {
                EditorGUILayout.ObjectField(handler, typeof(ButtonInteractionHandler), true);
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Device Type", BetterNavigation.Current?.InputDetector.CurrentNavigationInfo.Device.ToString());
            EditorGUILayout.LabelField("Input Action", BetterNavigation.Current?.InputDetector.CurrentNavigationInfo.Action.ToString());

            EditorGUILayout.EndScrollView();
        }
    }
}
