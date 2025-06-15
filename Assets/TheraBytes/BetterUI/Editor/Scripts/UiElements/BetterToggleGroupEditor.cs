using UnityEditor;
using UnityEngine.UI;

namespace TheraBytes.BetterUi.Editor
{
    [CustomEditor(typeof(BetterToggleGroup)), CanEditMultipleObjects]
    public class BetterToggleGroupEditor : UnityEditor.Editor
    {

        [MenuItem("CONTEXT/ToggleGroup/♠ Make Better")]
        public static void MakeBetter(MenuCommand command)
        {
            ToggleGroup tgl = command.context as ToggleGroup;
            Betterizer.MakeBetter<ToggleGroup, BetterToggleGroup>(tgl);
        }
    }
}
