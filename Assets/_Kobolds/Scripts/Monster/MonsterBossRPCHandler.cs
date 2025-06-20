using UnityEngine;

namespace Kobold.Bosses
{
	/// <summary>
	/// Client-side handler for boss visual/audio effects.
	/// Should be disabled on the owner.
	/// </summary>
	public class MonsterBossRPCHandler : MonoBehaviour
	{
		[SerializeField] private BossVFXManager vfxManager;
		
		private void Awake()
		{
			if (enabled)
			{
				Debug.Log("[MonsterBossRPCHandler] Enabled");
			}
		}

		public void PlayToppleEffect() => vfxManager?.PlayToppleVFX();
		public void PlayRecoveryEffect() => vfxManager?.PlayRecoveryVFX();
		public void PlayDeathEffect() => vfxManager?.PlayDeathVFX();
		public void PlayCoreReveal() => vfxManager?.PlayCoreRevealVFX();
		public void PlayAoePulse() => vfxManager?.PlayAoePulseVFX();

		
	}
}
