
#if ENABLE_INPUT_SYSTEM

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheraBytes.BetterUi.Editor
{
    [CustomEditor(typeof(AdditionForInputSystemUiInputModule))]
    public class AdditionForInputSystemUiInputModuleEditor : InputModuleAdditionEditor
    {
        InputActionReference[] actionReferences;
        string[] actionNames;

        InputActionAsset prevAsset;

        protected override void OnEnable()
        {
            base.OnEnable();
            actionReferences = null; // force a rebuild, so that the user can update maps
        }

        protected override void DrawButtonSelection(SerializedProperty button)
        {
            var obj = target as AdditionForInputSystemUiInputModule;
            if (obj.InputModule?.actionsAsset == null)
                return;

            if(actionReferences == null || prevAsset != obj.InputModule.actionsAsset)
            {
                InitializeActions(obj.InputModule.actionsAsset);
            }

            var index = IndexOfInputActionInAsset(
                        ((InputActionReference)button?.objectReferenceValue)?.action);

            EditorGUI.BeginChangeCheck();
            var label = new GUIContent(ObjectNames.NicifyVariableName(button.name));
            index = DrawPopup(button, label, index, actionNames);
            
            if (EditorGUI.EndChangeCheck())
            {
                button.objectReferenceValue = (index > 0)
                    ? actionReferences[index - 1] 
                    : null;
            }
        }

        int DrawPopup(SerializedProperty property, GUIContent label, int index, string[] actionNames)
        {
            EditorGuiUtils.GetControlSubRects(out Rect rect, out Rect labelRect, out Rect buttonRect);
            EditorGUI.BeginProperty(rect, label, property);
            EditorGUI.PrefixLabel(labelRect, label);

            string displayName = NicifyActionName(actionNames[index]);
            if (GUI.Button(buttonRect, displayName, EditorStyles.popup))
            {
                GenericMenu menu = new GenericMenu();
                for(int i = 0; i < actionNames.Length; i++)
                {
                    int curIndex = i;
                    string name = actionNames[i];
                    menu.AddItem(new GUIContent(name), NicifyActionName(name) == displayName,
                        () =>
                        {
                            property.objectReferenceValue = (curIndex > 0)
                                ? actionReferences[curIndex - 1]
                                : null;

                            property.serializedObject.ApplyModifiedProperties();
                        });
                }

                menu.DropDown(buttonRect);
            }

            EditorGUI.EndProperty();

            return index;
        }
        private void InitializeActions(InputActionAsset actionsAsset)
        {
            var path = AssetDatabase.GetAssetPath(actionsAsset);
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);

            actionReferences = assets
                .OfType<InputActionReference>()
                .ToArray();

            actionNames = new[] { "None" }
                .Concat(actionReferences.Select(x => x.name))
                .ToArray();

            prevAsset = actionsAsset;
        }

        private int IndexOfInputActionInAsset(InputAction inputAction)
        {
            // return 0 instead of -1 here because the zero-th index refers to the 'None' binding.
            if (inputAction == null)
                return 0;

            if (actionReferences == null)
                return 0;

            var index = 0;
            for (var j = 0; j < actionNames.Length; j++)
            {
                if (actionReferences[j].action != null &&
                    actionReferences[j].action == inputAction)
                {
                    index = j + 1;
                    break;
                }
            }

            return index;
        }

        string NicifyActionName(string name)
        {
            // using the same "ugly hack" for another unicode slash as unity's new input system.
            return name.Replace("/", "\uFF0F");
        }

        [MenuItem("CONTEXT/InputSystemUIInputModule/♠ Add Additional Button Mapping", false)]
        public static void AddAddition(MenuCommand command)
        {
            var ctx = command.context as MonoBehaviour;
            ctx.gameObject.AddComponent<AdditionForInputSystemUiInputModule>();
        }

        [MenuItem("CONTEXT/InputSystemUIInputModule/♠ Add Additional Button Mapping", true)]
        public static bool CheckAddition(MenuCommand command)
        {
            var ctx = command.context as MonoBehaviour;
            return ctx.gameObject.GetComponent<AdditionForInputSystemUiInputModule>() == null;
        }
    }
}

#endif