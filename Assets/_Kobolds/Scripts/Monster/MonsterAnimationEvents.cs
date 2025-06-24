using UnityEngine;

namespace Kobold.Bosses
{
	public class MonsterAnimationEvents : MonoBehaviour
	{
		[SerializeField] private BossEffectManager _effectManager;

		public void OnStepLeft()
		{
			_effectManager.TriggerEffect(BossEffectType.StepLeft);
		}

		public void OnStepRight()
		{
			_effectManager.TriggerEffect(BossEffectType.StepRight);
		}

		public void OnAoePulseCharge()
		{
			_effectManager.TriggerEffect(BossEffectType.AoePulseCharge);
		}
		
		public void OnAoePulseAttack()
		{
			_effectManager.TriggerEffect(BossEffectType.AoePulseAttack);
		}
	}
}