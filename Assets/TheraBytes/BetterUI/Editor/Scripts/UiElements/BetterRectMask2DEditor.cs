#if UNITY_2020_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TheraBytes.BetterUi.Editor
{
    [CustomEditor(typeof(BetterRectMask2D)), CanEditMultipleObjects]
    public class BetterRectMask2DEditor : RectMask2DEditor
    {
        SerializedProperty paddingFallback;
        SerializedProperty paddingSizers;
        SerializedProperty softnessFallback;
        SerializedProperty softnessSizers;

        protected override void OnEnable()
        {
            base.OnEnable();

            paddingFallback = serializedObject.FindProperty("paddingFallback");
            paddingSizers = serializedObject.FindProperty("paddingSizers");
            softnessFallback = serializedObject.FindProperty("softnessFallback");
            softnessSizers = serializedObject.FindProperty("softnessSizers");
        }

        public override void OnInspectorGUI()
        {
            ScreenConfigConnectionHelper.DrawSizerGui("Padding", paddingSizers, ref paddingFallback);
            ScreenConfigConnectionHelper.DrawSizerGui("Softness", softnessSizers, ref softnessFallback);
        }


        [MenuItem("CONTEXT/RectMask2D/♠ Make Better")]
        public static void MakeBetter(MenuCommand command)
        {
            RectMask2D rectMask = command.context as RectMask2D;
            Vector4 pad = rectMask.padding;
            Vector2Int soft = rectMask.softness;

            var newRectMask = Betterizer.MakeBetter<RectMask2D, BetterRectMask2D>(rectMask) as BetterRectMask2D;
            if (newRectMask != null)
            {
                newRectMask.PaddingSizer.SetSize(newRectMask, new Padding(pad));
                newRectMask.SoftnessSizer.SetSize(newRectMask, soft);
            }
        }
    }
}
#endif