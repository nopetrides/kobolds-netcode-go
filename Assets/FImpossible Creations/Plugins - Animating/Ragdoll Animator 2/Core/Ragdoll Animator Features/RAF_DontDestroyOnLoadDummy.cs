using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public class RAF_DontDestroyOnLoadDummy : RagdollAnimatorFeatureBase
    {
#if UNITY_EDITOR
        public override bool Editor_DisplayEnableSwitch => false;
        public override string Editor_FeatureDescription => "Marking generated ragdoll dummy as Don'tDestroyOnLoad so it will be not destroyed if scenes are changing. Use it when your character is Don'tDestroyOnLoad as well.";
#endif

        public override bool OnInit()
        {
            GameObject.DontDestroyOnLoad(ParentRagdollHandler.Dummy_Container);
            return base.OnInit();
        }
    }
}