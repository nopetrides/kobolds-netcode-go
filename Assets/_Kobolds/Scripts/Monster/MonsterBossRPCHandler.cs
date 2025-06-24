using Unity.Netcode;
using UnityEngine;

namespace Kobold.Bosses
{
	/// <summary>
	/// Handles networked propagation of boss visual/audio effects.
	/// Coordinates with BossEffectManager for playing effects.
	/// Only synchronizes with clients that are not the effect owner.
	/// </summary>
	public class MonsterBossRPCHandler : NetworkBehaviour
	{
		[SerializeField] private MonsterBossController _controller;
		[SerializeField] private BossEffectManager _effectManager;
		
		/// <summary>
		/// Triggers a synchronized damage application across the network.
		/// </summary>
		[Rpc(SendTo.Owner)]
		public void TriggerApplyDamageRpc(float amount, bool isWeakSpot = false, bool isCore = false, string limbName = null)
		{
			if (_controller == null)
			{
				Debug.LogError("[MonsterBossRPCHandler] _controller is null in TriggerApplyDamageRpc!");
				return;
			}

			_controller.ApplyDamage(amount, isWeakSpot, isCore, limbName);
		}
		
		/// <summary>
		/// Triggers a synchronized effect for all clients except the owner.
		/// </summary>
		[Rpc(SendTo.NotOwner)]
		public void TriggerEffectRpc(BossEffectType effectType)
		{
			if (_effectManager == null)
			{
				Debug.LogError("[MonsterBossRPCHandler] _effectManager is null in TriggerEffectRpc!");
				return;
			}
			
			// Play the effect on the clients that receive this call.
			_effectManager.TriggerEffect(effectType);
		}
	}
}
