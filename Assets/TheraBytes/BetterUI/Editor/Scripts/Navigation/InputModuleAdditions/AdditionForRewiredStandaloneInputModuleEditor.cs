
#if REWIRED

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Rewired;
using Rewired.Utils;

namespace TheraBytes.BetterUi.Editor
{
    [CustomEditor(typeof(AdditionForRewiredStandaloneInputModule))]
    public class AdditionForRewiredStandaloneInputModuleEditor : InputModuleAdditionWithAxisEditor
    {
        public static GUIContent[] actionNamesContent;
        public static int[] actionIds;

        private static readonly List<int> reusableIntList = new List<int>();

        InputManager_Base prevInputManager;

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            var obj = target as AdditionForRewiredStandaloneInputModule;
            if (obj.InputModule?.RewiredInputManager == null)
            {
                EditorGUILayout.HelpBox("Please assign your Rewired Input Manager instance to the Rewired Standalone Input Module.", MessageType.Error);
                return;
            }

            base.OnInspectorGUI();
        }

        protected override void DrawButtonSelection(SerializedProperty button)
        {
            var obj = target as AdditionForRewiredStandaloneInputModule;
            if (obj.InputModule?.RewiredInputManager == null)
                return;

            if(actionIds == null || prevInputManager != obj.InputModule.RewiredInputManager)
            {
                InitializeActions(obj.InputModule.RewiredInputManager);
            }

            var index = IndexOfActionId(button.intValue);

            var label = new GUIContent(ObjectNames.NicifyVariableName(button.name));
            EditorGuiUtils.DrawPopup(label, index, actionNamesContent, 
                indexChanged: (idx) =>
                {
                    index = idx;
                    button.intValue = actionIds[idx];
                    serializedObject.ApplyModifiedProperties();
                },
                selectionNameConvertMethod: (name) => name.Split('/').Last());

            if (EditorGUI.EndChangeCheck())
            {
                button.intValue = actionIds[index];
            }
        }

        private void InitializeActions(InputManager_Base inputManager)
        {
            inputManager.userData.GetActionIds(reusableIntList);
            reusableIntList.Insert(0, -1);
            actionIds = reusableIntList.ToArray();

            actionNamesContent = actionIds
                .Select(id => inputManager.userData.GetActionById(id))
                .Select(a => new GUIContent((a == null) 
                    ? "None" 
                    : $"{inputManager.userData.GetActionCategoryById(a.categoryId)?.name}/{a.name}"))
                .ToArray();


            prevInputManager = inputManager;
        }

        private int IndexOfActionId(int actionId)
        {
            if (actionIds == null)
                return 0;

            for (var i = 0; i < actionIds.Length; i++)
            {
                if (actionIds[i] == actionId)
                {
                    return i;
                }
            }

            return 0;
        }


        [MenuItem("CONTEXT/RewiredStandaloneInputModule/♠ Add Additional Button Mapping", false)]
        public static void AddAddition(MenuCommand command)
        {
            var ctx = command.context as MonoBehaviour;
            ctx.gameObject.AddComponent<AdditionForRewiredStandaloneInputModule>();
        }

        [MenuItem("CONTEXT/RewiredStandaloneInputModule/♠ Add Additional Button Mapping", true)]
        public static bool CheckAddition(MenuCommand command)
        {
            var ctx = command.context as MonoBehaviour;
            return ctx.gameObject.GetComponent<AdditionForRewiredStandaloneInputModule>() == null;
        }
    }
}

#endif
