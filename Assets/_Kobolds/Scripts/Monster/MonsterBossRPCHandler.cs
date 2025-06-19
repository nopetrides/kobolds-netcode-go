using UnityEngine;

namespace Kobolds.Bosses
{
	/// <summary>
	/// Client-side handler for boss visual/audio effects.
	/// Should be disabled on the owner.
	/// </summary>
	public class MonsterBossRPCHandler : MonoBehaviour
	{
		private void Awake()
		{
			if (enabled)
			{
				Debug.Log("[MonsterBossRPCHandler] Enabled for non-owner.");
			}
		}

		public void PlayToppleEffect()
		{
			Debug.Log("[MonsterBossRPCHandler] PlayToppleEffect()");
			// TODO: Trigger camera shake, VFX, sound, etc.
		}

		public void PlayRecoveryEffect()
		{
			Debug.Log("[MonsterBossRPCHandler] PlayRecoveryEffect()");
			// TODO: Play limb reset or reformation visuals
		}

		public void PlayDeathEffect()
		{
			Debug.Log("[MonsterBossRPCHandler] PlayDeathEffect()");
			// TODO: Show boss disintegration, explosion, or collapse
		}

		public void PlayCoreReveal()
		{
			Debug.Log("[MonsterBossRPCHandler] PlayCoreReveal()");
			// TODO: FX/sound for heart core exposure
		}

		public void PlayAoePulse()
		{
			Debug.Log("[MonsterBossRPCHandler] PlayAOEPulse()");
			// TODO: Shockwave particle and player detachment
		}
	}
}
