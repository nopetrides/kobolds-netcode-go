using UnityEngine;

namespace P3T.Scripts.AnimatedIntro
{
    public class SetAnimationOnAwake : MonoBehaviour
    {
        [SerializeField] private Animator Ac;
        [SerializeField] private string AnimationName;
		[SerializeField] private int AnimationLayer = -1;

        private void Awake()
        {
            if (Ac != null)
                Ac.Play(AnimationName, AnimationLayer, normalizedTime:Random.value);
        }
    }
}
