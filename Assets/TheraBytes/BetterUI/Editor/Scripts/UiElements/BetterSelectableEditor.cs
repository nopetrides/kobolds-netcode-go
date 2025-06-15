using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine.UI;

namespace TheraBytes.BetterUi.Editor
{
    [CustomEditor(typeof(BetterSelectable)), CanEditMultipleObjects]
    public class BetterSelectableEditor : SelectableEditor
    {
        BetterElementHelper<Selectable, BetterSelectable> helper =
            new BetterElementHelper<Selectable, BetterSelectable>();

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            helper.DrawGui(serializedObject);

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("CONTEXT/Selectable/♠ Make Better", validate = true)]
        public static bool ValidateMakeBetter(MenuCommand command)
        {
            return !(command.context is BetterNavigation);
        }


        [MenuItem("CONTEXT/Selectable/♠ Make Better")]
        public static void MakeBetter(MenuCommand command)
        {
            Selectable sel = command.context as Selectable;
            Betterizer.MakeBetter<Selectable, BetterSelectable>(sel);
        }


        [MenuItem("CONTEXT/Selectable/♠ Convert to Better Toggle", validate = true)]
        public static bool ValidateConvertToToggle(MenuCommand command)
        {
            return command.context is Selectable
                && !(command.context is Toggle)
                && !(command.context is BetterNavigation);
        }

        [MenuItem("CONTEXT/Selectable/♠ Convert to Better Toggle", priority = -10)]
        public static void ConvertToToggle(MenuCommand command)
        {
            Selectable selectable = command.context as Selectable;
            List<Transitions> transitionsToCopy = null;

            if (selectable is BetterSelectable betterSelectable)
            {
                transitionsToCopy = betterSelectable.BetterTransitions.ToList();
            }
            else if (selectable is BetterButton betterButton)
            {
                transitionsToCopy = betterButton.BetterTransitions.ToList();
            }

            var sel = Betterizer.MakeBetter<Selectable, BetterToggle>(selectable);
            if (transitionsToCopy != null)
            {
                var tgl = sel as BetterToggle;
                foreach (var transition in transitionsToCopy)
                {
                    tgl.BetterTransitions.Add(transition);
                }
            }
        }


        [MenuItem("CONTEXT/Selectable/♠ Convert to Better Button", validate = true)]
        public static bool ValidateConvertToButton(MenuCommand command)
        {
            return command.context is Selectable
                && !(command.context is Button)
                && !(command.context is BetterNavigation);
        }

        [MenuItem("CONTEXT/Selectable/♠ Convert to Better Button", priority = -11)]
        public static void ConvertToButton(MenuCommand command)
        {
            Selectable selectable = command.context as Selectable;
            List<Transitions> transitionsToCopy = null;

            if (selectable is BetterSelectable betterSelectable)
            {
                transitionsToCopy = betterSelectable.BetterTransitions.ToList();
            }
            else if (selectable is BetterToggle betterToggle)
            {
                transitionsToCopy = betterToggle.BetterTransitions;
                transitionsToCopy = transitionsToCopy.Concat(betterToggle.BetterTransitionsWhenOff
                    .Where(o => !transitionsToCopy.Any(x => o.TransitionStates.Target == x.TransitionStates.Target)))
                    .ToList();
            }

            var sel = Betterizer.MakeBetter<Selectable, BetterButton>(selectable);
            if (transitionsToCopy != null)
            {
                var tgl = sel as BetterButton;
                foreach (var transition in transitionsToCopy)
                {
                    tgl.BetterTransitions.Add(transition);
                }
            }
        }


        [MenuItem("CONTEXT/Selectable/♠ Downgrade to Better Selectable", validate = true)]
        public static bool ValidateConvertToSelectable(MenuCommand command)
        {
            System.Type type = command.context.GetType();
            return command.context is Selectable        // derives from Selectable
                && type != typeof(Selectable)           // but is not a Selectable
                && type != typeof(BetterSelectable)     // also, not a Better one.
                && type != typeof(BetterNavigation);    // BetterNavigation also derives from Selectable for internal reasons
        }

        [MenuItem("CONTEXT/Selectable/♠ Downgrade to Better Selectable", priority = -12)]
        public static void ConvertToSelectable(MenuCommand command)
        {
            Selectable selectable = command.context as Selectable;
            List<Transitions> transitionsToCopy = null;

            if (selectable is BetterButton betterButton)
            {
                transitionsToCopy = betterButton.BetterTransitions.ToList();
            }
            else if (selectable is BetterToggle betterToggle)
            {
                transitionsToCopy = betterToggle.BetterTransitions;
                transitionsToCopy = transitionsToCopy.Concat(betterToggle.BetterTransitionsWhenOff
                    .Where(o => !transitionsToCopy.Any(x => o.TransitionStates.Target == x.TransitionStates.Target)))
                    .ToList();
            }

            var sel = Betterizer.MakeBetter<Selectable, BetterSelectable>(selectable);
            if (transitionsToCopy != null)
            {
                var tgl = sel as BetterSelectable;
                foreach (var transition in transitionsToCopy)
                {
                    tgl.BetterTransitions.Add(transition);
                }
            }
        }
    }
}
