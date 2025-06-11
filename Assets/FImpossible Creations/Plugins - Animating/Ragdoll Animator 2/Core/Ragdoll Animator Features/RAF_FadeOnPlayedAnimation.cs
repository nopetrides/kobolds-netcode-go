using System.Collections.Generic;
using FIMSpace.FGenerating;
#if UNITY_EDITOR
using FIMSpace.FEditor;
using UnityEditor;
#endif

using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public class RAF_FadeOnPlayedAnimation : RagdollAnimatorFeatureBase
    {
        FUniversalVariable _fadeSpeedV;
        FUniversalVariable _layerV;

        float fadeValue = 1f;
        float sd_eneMul = 0f;

        List<int> stateHashes;
        List<int> tagHashes;

        enum ELayerSelectMode { ByIndex, Auto }
        FUniversalVariable _layerMode;
        FUniversalVariable _layerSkip;
        List<int> layersToCheck = null;
        int lastAutoWeightIndex = 0;


        #region Auto Layers Check Init

        bool InitLayerCheck(RagdollAnimatorFeatureHelper helper)
        {
            if (helper.ParentRagdollHandler.Mecanim == null) return false;
            if (_layerMode.GetInt() == 0) return false;
            if (_layerMode == null || _layerSkip == null) return false;

            layersToCheck = new List<int>();

            string[] args = _layerSkip.GetString().Split(',');

            for (int i = 0; i < helper.ParentRagdollHandler.Mecanim.layerCount; i++) layersToCheck.Add(i);

            for (int a = 0; a < args.Length; a++)
            {
                int parsed;
                if (int.TryParse(args[a], out parsed))
                {
                    layersToCheck.Remove(parsed);
                }
                else
                {
                    int layerNameIndex = -1;
                    for (int i = 0; i < helper.ParentRagdollHandler.Mecanim.layerCount; i++)
                    {
                        if (helper.ParentRagdollHandler.Mecanim.GetLayerName(i) == args[a])
                        {
                            layerNameIndex = i;
                            break;
                        }
                    }

                    if (layerNameIndex != -1) layersToCheck.Remove(layerNameIndex);
                }
            }

            return true;
        }

        #endregion


        #region Handling Whole Animator Stuff

        public override bool OnInit()
        {
            if (ParentRagdollHandler.Mecanim == null)
            {
                Debug.Log("[Legs Animator] Fade On Animation Module: Not found animator reference in legs animator Extra/Control!");
                Helper.Enabled = false;
                return false;
            }

            _layerV = Helper.RequestVariable("Animation Layer", 0);
            _fadeSpeedV = Helper.RequestVariable("Fade Speed", 0.75f);

            var tagsV = Helper.RequestVariable("Animation State Tag", "");
            var statesV = Helper.RequestVariable("Animation State Name", "");

            // Prepare target animation hashes for quick checking animator state
            string animStates = statesV.GetString();
            var statesSeparated = animStates.Split(',');

            #region Prepare mecanim hashes

            if (statesSeparated.Length > 0)
            {
                stateHashes = new List<int>();
                for (int i = 0; i < statesSeparated.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(statesSeparated[i])) continue;
                    stateHashes.Add(Animator.StringToHash(statesSeparated[i]));
                }
            }

            string tagNames = tagsV.GetString();
            var tagsSeparated = tagNames.Split(',');

            if (tagsSeparated.Length > 0)
            {
                tagHashes = new List<int>();
                for (int i = 0; i < tagsSeparated.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(tagsSeparated[i])) continue;
                    tagHashes.Add(Animator.StringToHash(tagsSeparated[i]));
                }
            }

            if (stateHashes.Count == 0 && tagHashes.Count == 0)
            {
                Helper.Enabled = false;
                Debug.Log("[Ragdoll Animator] Fade On Played Animation: No assigned animation state names/tags to control feature on!");
                return false;
            }

            #endregion

            if (_layerV.GetInt() < 0) _layerV.SetValue(0); if (_layerV.GetInt() > ParentRagdollHandler.Mecanim.layerCount - 1) _layerV.SetValue(ParentRagdollHandler.Mecanim.layerCount - 1);

            // Auto Layers Check
            _layerMode = Helper.RequestVariable("Mode", 0);
            _layerSkip = Helper.RequestVariable("Skip", "");

            if (_layerMode.GetInt() == 1)
            {
                if (InitLayerCheck(Helper) == false) _layerMode.SetValue(0);
            }

            ParentRagdollHandler.AddToUpdateLoop(UpdateFeature);

            return base.OnInit();
        }

        public override void OnDestroyFeature()
        {
            ParentRagdollHandler.RemoveFromUpdateLoop(UpdateFeature);
            base.OnDestroyFeature();
        }


        void UpdateFeature()
        {
            if (Helper.Enabled == false) return;

            Animator anim = ParentRagdollHandler.Mecanim;
            if (anim == null) return;

            int layer = _layerV.GetInt();

            if (_layerMode.GetInt() == 1)
            {
                #region Auto Layer Check

                float mostWeight = 0f;
                int mostWeightI = -1;

                for (int i = layersToCheck.Count - 1; i >= 0; i--) // Reverse for to stop checking on 100% weight top layer
                {
                    int idx = layersToCheck[i];
                    float weight = anim.GetLayerWeight(idx);
                    if (weight > 0.95f) // Dont check if layer has 
                    {
                        mostWeightI = idx;
                        break;
                    }
                    else
                    {
                        if (weight > mostWeight)
                        {
                            mostWeight = weight;
                            mostWeightI = idx;
                        }
                    }
                }

                layer = mostWeightI;
                lastAutoWeightIndex = layer;

                #endregion
            }

            AnimatorStateInfo animatorInfo = anim.IsInTransition(layer) ? anim.GetNextAnimatorStateInfo(layer) : anim.GetCurrentAnimatorStateInfo(layer);

            bool fadeOut = false;

            for (int n = 0; n < stateHashes.Count; n++)
            {
                if (animatorInfo.shortNameHash == stateHashes[n]) { fadeOut = true; break; }
            }

            if (!fadeOut)
            {
                for (int t = 0; t < tagHashes.Count; t++)
                {
                    if (animatorInfo.tagHash == tagHashes[t]) { fadeOut = true; break; }
                }
            }

            float fadeDur = 0.3f - _fadeSpeedV.GetFloat() * 0.299f;

            if (fadeOut)
            {
                fadeValue = Mathf.SmoothDamp(fadeValue, -0.001f, ref sd_eneMul, fadeDur * 0.9f, 100000f, ParentRagdollHandler.Delta);
            }
            else
            {
                fadeValue = Mathf.SmoothDamp(fadeValue, 1.01f, ref sd_eneMul, fadeDur, 100000f, ParentRagdollHandler.Delta);
            }

            fadeValue = Mathf.Clamp01((float)fadeValue);

            ApplyBlends();
        }

        #endregion


        public virtual void ApplyBlends()
        {
            ParentRagdollHandler.RagdollBlend = Mathf.Max(0.0001f, fadeValue);
        }

        #region Editor Code

#if UNITY_EDITOR

        public override string Editor_FeatureDescription => "Changing ragdoll blend when certain animations are played in the AnimatorController component.";

        public override void Editor_InspectorGUI(SerializedProperty handlerProp, RagdollHandler ragdollHandler, RagdollAnimatorFeatureHelper helper)
        {
            if (ragdollHandler.Mecanim == null)
            {
                EditorGUILayout.HelpBox("Unity Animator Reference (Mecanim) is required by this module. Go to Extra/Control category and assign Mecanim reference there!", UnityEditor.MessageType.Warning);
            }

            Animator anim = ragdollHandler.Mecanim;

            bool drawLayer = true;
            if (anim)
            {
                if (anim.layerCount < 2) drawLayer = false;
            }

            if (drawLayer)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUIUtility.labelWidth = 34;
                var layerMode = helper.RequestVariable("Mode", 0);

                if (Initialized) GUI.enabled = false;
                ELayerSelectMode selMode = (ELayerSelectMode)layerMode.GetInt();
                selMode = (ELayerSelectMode)EditorGUILayout.EnumPopup(new GUIContent("", "If layer to read animator state/tag from should be selected by index, or by top layer with biggest weight fade"), selMode, GUILayout.MaxWidth(74));
                layerMode.SetValue((int)selMode);
                GUI.enabled = true;

                EditorGUIUtility.labelWidth = 40;

                if (selMode == ELayerSelectMode.ByIndex)
                {
                    GUILayout.Space(6);
                    var layerInd = helper.RequestVariable("Animation Layer", 0);
                    EditorGUIUtility.labelWidth = 42;
                    int indx = EditorGUILayout.IntField(new GUIContent("Index:", "Index to read animator state/tag from"), layerInd.GetInt());
                    if (indx < 0) indx = 0;
                    if (anim) if (indx > anim.layerCount - 1) indx = anim.layerCount - 1;
                    layerInd.SetValue(indx);
                }
                else
                {
                    GUILayout.Space(6);
                    var skipVar = helper.RequestVariable("Skip", "");
                    EditorGUIUtility.labelWidth = 35;
                    string skip = skipVar.GetString();
                    if (Initialized) GUI.enabled = false;
                    skip = EditorGUILayout.TextField(new GUIContent("Skip:", "Write here indexes of upper body layers to skip checking them. You can also write here layer names. To skip multiple layers, use command ',' like: 3,4,6"), skip);
                    skipVar.SetValue(skip);
                    GUI.enabled = true;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = 0;

                if (selMode == ELayerSelectMode.Auto) EditorGUILayout.HelpBox("Automatic Layer: " + lastAutoWeightIndex, UnityEditor.MessageType.None);
            }

            GUILayout.Space(6);

            var fadeSpd = helper.RequestVariable("Fade Speed", 0.75f);
            fadeSpd.SetMinMaxSlider(0f, 1f);
            fadeSpd.Editor_DisplayVariableGUI();

            GUILayout.Space(4);
            FGUI_Inspector.DrawUILineCommon(8);

            GUI.enabled = !ragdollHandler.WasInitialized;
            EditorGUILayout.LabelField("Fade On:", EditorStyles.centeredGreyMiniLabel);
            var hipsVar = helper.RequestVariable("Animation State Tag", "");
            hipsVar.Editor_DisplayVariableGUI();

            GUILayout.Space(3);
            var extraMultiplier = helper.RequestVariable("Animation State Name", "");
            extraMultiplier.Editor_DisplayVariableGUI();
            EditorGUILayout.LabelField("Use commas ',' to take into account multiple clips/tags", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(3);
            GUI.enabled = true;

            if (ragdollHandler.WasInitialized)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUI.enabled = false;
                EditorGUILayout.Slider("Current Weight: ", fadeValue, 0f, 1f);
                GUI.enabled = true;

                EditorGUILayout.EndVertical();
            }
        }

#endif

        #endregion

    }

}