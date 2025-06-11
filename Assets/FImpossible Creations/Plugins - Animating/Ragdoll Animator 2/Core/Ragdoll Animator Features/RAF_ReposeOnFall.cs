using FIMSpace.FGenerating;

#if UNITY_EDITOR

using UnityEditor;

#endif

using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public class RAF_ReposeOnFall : RagdollAnimatorFeatureUpdate
    {
        public enum EBaseTransformRepose
        { AnchorToFootPosition = 0, AnchorBoneBottom = 1, BonesBoundsBottomCenter = 2, SkeletonCenter = 3, }
        public enum EOrientationMode
        { HipsToFeetMiddle = 0, HeadToHips = 1, HipsToHead = 2, MappedHips = 3, }

        public static Vector3 GetReposePosition(IRagdollAnimator2HandlerOwner iHandler, EBaseTransformRepose reposeMode)
        {
            if (reposeMode == EBaseTransformRepose.AnchorToFootPosition)
            {
                return iHandler.GetRagdollHandler.User_GetPosition_HipsToFoot();
            }
            else if (reposeMode == EBaseTransformRepose.AnchorBoneBottom)
            {
                return iHandler.GetRagdollHandler.User_GetPosition_AnchorBottom();
            }
            else if (reposeMode == EBaseTransformRepose.BonesBoundsBottomCenter)
            {
                return iHandler.GetRagdollHandler.User_GetPosition_BottomCenter();
            }
            else //if( reposeMode == EBaseTransformRepose.SkeletonCenter )
            {
                return iHandler.GetRagdollHandler.User_GetPosition_Center();
            }
        }

        public override bool UseFixedUpdate => true;
        public override bool UseLateUpdate => true;

        private FUniversalVariable reposeVar;
        private FUniversalVariable rotationVar;
        private FUniversalVariable orientationVar;

        private float reposeStartAt = -100f;
        bool wasFixed = false;

        public override bool OnInit()
        {
            reposeVar = InitializedWith.RequestVariable("Mode", 1);
            rotationVar = InitializedWith.RequestVariable("Apply Rotation:", false);
            orientationVar = InitializedWith.RequestVariable("Orientation Mode", 0);
            return base.OnInit();
        }

        public override void LateUpdate()
        {
            if (InitializedWith.Enabled == false) return;

            if (ParentRagdollHandler.IsFallingOrSleep)
            {
                if (reposeStartAt < 0f)
                {
                    reposeStartAt = Time.time;
                    return;
                }

                if (Time.time - reposeStartAt < 0.1f) return; // Not allow repose start too soon to avoid transform update conflicts
                if (!wasFixed) return;

                EBaseTransformRepose reposeMode = (EBaseTransformRepose)reposeVar.GetInt();

                ParentRagdollHandler.BaseTransform.position = GetReposePosition(ParentRagdollHandler, reposeMode);

                if (rotationVar.GetBool())
                {
                    var orientMode = (EOrientationMode)orientationVar.GetInt();
                    Quaternion targetRotation = ParentRagdollHandler.BaseTransform.rotation;

                    if (orientMode == EOrientationMode.HipsToFeetMiddle)
                        targetRotation = ParentRagdollHandler.User_GetMappedRotationHipsToLegsMiddle(Vector3.up);
                    else if (orientMode == EOrientationMode.HipsToHead)
                        targetRotation = ParentRagdollHandler.User_GetMappedRotationHipsToHead(Vector3.up);
                    else if (orientMode == EOrientationMode.HeadToHips)
                        targetRotation = ParentRagdollHandler.User_GetMappedRotationHeadToHips(Vector3.up);
                    else if (orientMode == EOrientationMode.MappedHips)
                        targetRotation = ParentRagdollHandler.User_GetRotation_Mapped(Vector3.up);

                    ParentRagdollHandler.BaseTransform.rotation = targetRotation;
                }
            }
            else
            {
                wasFixed = false;
                reposeStartAt = -100f;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (ParentRagdollHandler.IsFallingOrSleep == false) return;
            if (reposeStartAt <= 0f) return;
            wasFixed = true;
        }

#if UNITY_EDITOR

        public override string Editor_FeatureDescription => "Changing base object position (like character controller) to be aligned with ragdolled bones when falling mode.";

        public override void Editor_InspectorGUI(SerializedProperty toDirty, RagdollHandler ragdollHandler, RagdollAnimatorFeatureHelper helper)
        {
            base.Editor_InspectorGUI(toDirty, ragdollHandler, helper);

            var modeV = helper.RequestVariable("Mode", 1);
            modeV.AssignTooltip("Choose option which fits to your Get Up animation origin. Some get up animations can start at hips center, some are starting at feet position.");
            EBaseTransformRepose reposeM = (EBaseTransformRepose)modeV.GetInt();
            reposeM = (EBaseTransformRepose)EditorGUILayout.EnumPopup("Mode:", reposeM);
            modeV.SetValue((int)reposeM);

            var rotationV = helper.RequestVariable("Apply Rotation:", true);
            rotationV.AssignTooltip("Apply calculated rotation to the base object");
            rotationV.Editor_DisplayVariableGUI();

            GUI.enabled = rotationV.GetBool();
            var orientV = helper.RequestVariable("Orientation Mode", 1);
            orientV.AssignTooltip("Define how target rotation should be calculated.");
            EOrientationMode orientM = (EOrientationMode)orientV.GetInt();
            orientM = (EOrientationMode)EditorGUILayout.EnumPopup("Orientation Mode:", orientM);
            orientV.SetValue((int)orientM);
            GUI.enabled = true;
        }

#endif
    }
}