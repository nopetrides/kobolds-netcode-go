using System;
using Unity.BossRoom.Gameplay.UserInput;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Gameplay.GameplayObjects;
using UnityEngine;
using SkillTriggerStyle = Unity.BossRoom.Gameplay.UserInput.ClientInputSender.SkillTriggerStyle;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// Provides logic for a Hero Emote Bar
    /// This button bar tracks button clicks and also hides after any click
    /// </summary>
    public class HeroEmoteBar : MonoBehaviour
    {
        ClientInputSender _mInputSender;

        void Awake()
        {
            ClientPlayerAvatar.LocalClientSpawned += RegisterInputSender;
            ClientPlayerAvatar.LocalClientDespawned += DeregisterInputSender;
        }

        void RegisterInputSender(ClientPlayerAvatar clientPlayerAvatar)
        {
            if (!clientPlayerAvatar.TryGetComponent(out ClientInputSender inputSender))
            {
                Debug.LogError("ClientInputSender not found on ClientPlayerAvatar!", clientPlayerAvatar);
            }

            if (_mInputSender != null)
            {
                Debug.LogWarning($"Multiple ClientInputSenders in scene? Discarding sender belonging to {_mInputSender.gameObject.name} and adding it for {inputSender.gameObject.name} ");
            }

            _mInputSender = inputSender;

            gameObject.SetActive(false);
        }

        void DeregisterInputSender()
        {
            _mInputSender = null;
        }

        void OnDestroy()
        {
            ClientPlayerAvatar.LocalClientSpawned -= RegisterInputSender;
            ClientPlayerAvatar.LocalClientDespawned -= DeregisterInputSender;
        }

        public void OnButtonClicked(int buttonIndex)
        {
            if (_mInputSender != null)
            {
                switch (buttonIndex)
                {
                    case 0: _mInputSender.RequestAction(GameDataSource.Instance.Emote1ActionPrototype.ActionID, SkillTriggerStyle.UI); break;
                    case 1: _mInputSender.RequestAction(GameDataSource.Instance.Emote2ActionPrototype.ActionID, SkillTriggerStyle.UI); break;
                    case 2: _mInputSender.RequestAction(GameDataSource.Instance.Emote3ActionPrototype.ActionID, SkillTriggerStyle.UI); break;
                    case 3: _mInputSender.RequestAction(GameDataSource.Instance.Emote4ActionPrototype.ActionID, SkillTriggerStyle.UI); break;
                }
            }

            // also deactivate the emote panel
            gameObject.SetActive(false);
        }
    }
}
