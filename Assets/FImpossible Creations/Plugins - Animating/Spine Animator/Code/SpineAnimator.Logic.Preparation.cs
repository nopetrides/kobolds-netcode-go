using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FSpine
{
    public partial class FSpineAnimator
    {
        bool chainReverseFlag;
        //Vector3 firstBoneOff = Vector3.zero;

        public void Init()
        {
            if (SpineBones.Count == 0)
            {
                if (SpineTransforms.Count > 2)
                {
                    CreateSpineChain(SpineTransforms[0], SpineTransforms[SpineTransforms.Count - 1]);
                    Debug.Log("[SPINAL ANIMATOR] Auto Bone Conversion from old version of Spine Animator! Please select your objects with Spine Animator to pre-convert it instead of automatically doing it when game Starts! (" + name + ")");
                }
                else
                {
                    Debug.Log("[SPINAL ANIMATOR] could not initialize Spine Animator inside '" + name + "' because there are no bones to animate!");
                    return;
                }
            }

            if (initialized) { Debug.Log("[Spinal Animator] " + name + " is already initialized!"); return; }

            if (BaseTransform == null) BaseTransform = FindBaseTransform();

            // Checking bones for zero-distance ones
            for (int i = 0; i < SpineBones.Count; i++)
            {
                Vector3 childPos;
                if (i == SpineBones.Count - 1)
                {
                    if (SpineBones[i].transform.childCount > 0)
                        childPos = SpineBones[i].transform.GetChild(0).position;
                    else
                        childPos = SpineBones[i - 1].transform.position + (SpineBones[i - 1].transform.position - SpineBones[i].transform.position);
                }
                else childPos = SpineBones[i + 1].transform.position;

                float dist = Vector3.Distance(SpineBones[i].transform.position, childPos);

                if (dist < 0.01f)
                {
                    float refDistance = (SpineBones[SpineBones.Count - 1].transform.position - SpineBones[SpineBones.Count - 2].transform.parent.position).magnitude;

                    Vector3 forw = SpineBones[i].transform.position - BaseTransform.position;
                    Vector3 loc = BaseTransform.InverseTransformDirection(forw);
                    loc.y = 0f; loc.Normalize();

                    if( i + 1 < SpineBones.Count )
                    {
                        SpineBones[i + 1].DefaultForward = loc;

                        // firstBoneOff
                        SpineBones[i + 1].transform.position = SpineBones[i + 1].transform.position + BaseTransform.TransformDirection( loc ) * refDistance * -0.125f;
                    }
                    else
                    {
                        UnityEngine.Debug.Log( "[Spinal Animator] Check your spine bones hierarchy in '"+name+"'. Some bones are very short, consider removing them from the spine chain." );
                    }

                }
            }

            referenceDistance = 0f;
            // Preparing bones
            for (int i = 0; i < SpineBones.Count; i++)
            {
                SpineBones[i].PrepareBone(BaseTransform, SpineBones, i);
                referenceDistance += SpineBones[i].BoneLength;
            }

            referenceDistance /= (float)(SpineBones.Count);

            frontHead = new HeadBone(SpineBones[0].transform);
            frontHead.PrepareBone(BaseTransform, SpineBones, 0, FlattenReferencePose);
            backHead = new HeadBone(SpineBones[SpineBones.Count - 1].transform);
            backHead.PrepareBone(BaseTransform, SpineBones, SpineBones.Count - 1, FlattenReferencePose);

            if (LastBoneLeading) headBone = backHead; else headBone = frontHead;

            // Collision calculations helper list
            CollidersDataToCheck = new List<FImp_ColliderData_Base>();

            // Straightening spine pose to desired positions and rotations on init
            chainReverseFlag = !LastBoneLeading;

            UpdateChainIndexHelperVariables();
            ReposeSpine();
            //SpineMotion();

            initialized = true;
        }


        /// <summary>
        /// Should be called by inspector window, but you can also do it during playmode for procedural creations
        /// </summary>
        public void CreateSpineChain(Transform start, Transform end)
        {
            if (start == null || end == null)
            {
                Debug.Log("[SPINAL ANIMATOR] Can't create spine chain if one of the bones is null!");
                return;
            }

            List<Transform> fullChain = new List<Transform>();

            Transform p = end;
            while (p != null) { if (p == start) break; fullChain.Add(p); p = p.parent; }


            if (p == null)
            {
                Debug.Log("[SPINAL ANIMATOR] '" + start.name + "' is not child of '" + end.name + "' !");
                return;
            }

            fullChain.Add(start);
            fullChain.Reverse();

            SpineBones = new List<SpineBone>();

            for (int i = 0; i < fullChain.Count; i++)
            {
                SpineBone bone = new SpineBone(fullChain[i]);
                SpineBones.Add(bone);
            }

            // After creating chain we checking if some auto corrections can be done

        }

    }
}