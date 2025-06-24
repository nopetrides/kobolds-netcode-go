using UnityEngine;
/// <summary>
/// Play winged animations from matching base animations, instead of triggering manually
/// </summary>
public class WingedMatchingAnimator : MonoBehaviour
{
	[SerializeField] private Animator Ac;
	
	public void PlayOtherAnimation(string animationName)
	{
		if (Ac != null)
			Ac.Play(animationName);
	}
}
