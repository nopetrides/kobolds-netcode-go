using UnityEngine;

public class MonsterAnimationEvents : MonoBehaviour
{
    [SerializeField] private ParticleSystem StepLeft;
	[SerializeField] private ParticleSystem StepRight;

	// TODO replace with RPC
	public void OnStepLeft() => StepLeft.Play();
	public void OnStepRight() => StepRight.Play();
}
