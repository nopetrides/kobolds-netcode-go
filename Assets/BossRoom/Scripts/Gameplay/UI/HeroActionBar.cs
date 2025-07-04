using System;
using System.Collections.Generic;
using Unity.BossRoom.Gameplay.UserInput;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using UnityEngine;
using Action = Unity.BossRoom.Gameplay.Actions.Action;
using SkillTriggerStyle = Unity.BossRoom.Gameplay.UserInput.ClientInputSender.SkillTriggerStyle;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// Provides logic for a Hero Action Bar with attack, skill buttons and a button to open emotes panel
    /// This bar tracks button clicks on hero action buttons for later use by ClientInputSender
    /// </summary>
    public class HeroActionBar : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The button that activates the basic action (comparable to right-clicking the mouse)")]
        UihudButton m_BasicActionButton;

        [SerializeField]
        [Tooltip("The button that activates the hero's first special move")]
        UihudButton m_SpecialAction1Button;

        [SerializeField]
        [Tooltip("The button that activates the hero's second special move")]
        UihudButton m_SpecialAction2Button;

        [SerializeField]
        [Tooltip("The button that opens/closes the Emote bar")]
        UihudButton m_EmoteBarButton;

        [SerializeField]
        [Tooltip("The Emote bar that will be enabled or disabled when clicking the Emote bar button")]
        GameObject m_EmotePanel;

        /// <summary>
        /// Our input-sender. Initialized in RegisterInputSender()
        /// </summary>
        ClientInputSender _mInputSender;

        /// <summary>
        /// Identifiers for the buttons on the action bar.
        /// </summary>
        enum ActionButtonType
        {
            BasicAction,
            Special1,
            Special2,
            EmoteBar,
        }

        /// <summary>
        /// Cached UI information about one of the buttons on the action bar.
        /// Takes care of registering/unregistering click-event messages,
        /// and routing the events into HeroActionBar.
        /// </summary>
        class ActionButtonInfo
        {
            public readonly ActionButtonType Type;
            public readonly UihudButton Button;
            public readonly UITooltipDetector Tooltip;

            /// <summary> T
            /// The current Action that is used when this button is pressed.
            /// </summary>
            public Action CurAction;

            readonly HeroActionBar _mOwner;

            public ActionButtonInfo(ActionButtonType type, UihudButton button, HeroActionBar owner)
            {
                Type = type;
                Button = button;
                Tooltip = button.GetComponent<UITooltipDetector>();
                _mOwner = owner;
            }

            public void RegisterEventHandlers()
            {
                Button.OnPointerDownEvent += OnClickDown;
                Button.OnPointerUpEvent += OnClickUp;
            }

            public void UnregisterEventHandlers()
            {
                Button.OnPointerDownEvent -= OnClickDown;
                Button.OnPointerUpEvent -= OnClickUp;
            }

            void OnClickDown()
            {
                _mOwner.OnButtonClickedDown(Type);
            }

            void OnClickUp()
            {
                _mOwner.OnButtonClickedUp(Type);
            }
        }

        /// <summary>
        /// Dictionary of info about all the buttons on the action bar.
        /// </summary>
        Dictionary<ActionButtonType, ActionButtonInfo> _mButtonInfo;

        /// <summary>
        /// Cache the input sender from a <see cref="ClientPlayerAvatar"/> and self-initialize.
        /// </summary>
        /// <param name="clientPlayerAvatar"></param>
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
            _mInputSender.Action1ModifiedCallback += Action1ModifiedCallback;

            Action action1 = null;
            if (_mInputSender.ActionState1 != null)
            {
                GameDataSource.Instance.TryGetActionPrototypeByID(_mInputSender.ActionState1.ActionID, out action1);
            }
            UpdateActionButton(_mButtonInfo[ActionButtonType.BasicAction], action1);

            Action action2 = null;
            if (_mInputSender.ActionState2 != null)
            {
                GameDataSource.Instance.TryGetActionPrototypeByID(_mInputSender.ActionState2.ActionID, out action2);
            }
            UpdateActionButton(_mButtonInfo[ActionButtonType.Special1], action2);

            Action action3 = null;
            if (_mInputSender.ActionState3 != null)
            {
                GameDataSource.Instance.TryGetActionPrototypeByID(_mInputSender.ActionState3.ActionID, out action3);
            }
            UpdateActionButton(_mButtonInfo[ActionButtonType.Special2], action3);
        }

        void Action1ModifiedCallback()
        {
            var action = GameDataSource.Instance.GetActionPrototypeByID(_mInputSender.ActionState1.ActionID);

            UpdateActionButton(_mButtonInfo[ActionButtonType.BasicAction],
                action,
                _mInputSender.ActionState1.Selectable);
        }

        void DeregisterInputSender()
        {
            if (_mInputSender)
            {
                _mInputSender.Action1ModifiedCallback -= Action1ModifiedCallback;
            }
            _mInputSender = null;
        }

        void Awake()
        {
            _mButtonInfo = new Dictionary<ActionButtonType, ActionButtonInfo>()
            {
                [ActionButtonType.BasicAction] = new ActionButtonInfo(ActionButtonType.BasicAction, m_BasicActionButton, this),
                [ActionButtonType.Special1] = new ActionButtonInfo(ActionButtonType.Special1, m_SpecialAction1Button, this),
                [ActionButtonType.Special2] = new ActionButtonInfo(ActionButtonType.Special2, m_SpecialAction2Button, this),
                [ActionButtonType.EmoteBar] = new ActionButtonInfo(ActionButtonType.EmoteBar, m_EmoteBarButton, this),
            };

            ClientPlayerAvatar.LocalClientSpawned += RegisterInputSender;
            ClientPlayerAvatar.LocalClientDespawned += DeregisterInputSender;
        }

        void OnEnable()
        {
            foreach (ActionButtonInfo buttonInfo in _mButtonInfo.Values)
            {
                buttonInfo.RegisterEventHandlers();
            }
        }

        void OnDisable()
        {
            foreach (ActionButtonInfo buttonInfo in _mButtonInfo.Values)
            {
                buttonInfo.UnregisterEventHandlers();
            }
        }

        void OnDestroy()
        {
            DeregisterInputSender();

            ClientPlayerAvatar.LocalClientSpawned -= RegisterInputSender;
            ClientPlayerAvatar.LocalClientDespawned -= DeregisterInputSender;
        }

        void Update()
        {
            if (Input.GetKeyUp(KeyCode.Alpha4))
            {
                _mButtonInfo[ActionButtonType.EmoteBar].Button.OnPointerUpEvent.Invoke();
            }
        }

        void OnButtonClickedDown(ActionButtonType buttonType)
        {
            if (buttonType == ActionButtonType.EmoteBar)
            {
                return; // this is the "emote" button; we won't do anything until they let go of the button
            }

            if (_mInputSender == null)
            {
                //nothing to do past this point if we don't have an InputSender.
                return;
            }

            // send input to begin the action associated with this button
            _mInputSender.RequestAction(_mButtonInfo[buttonType].CurAction.ActionID, SkillTriggerStyle.UI);
        }

        void OnButtonClickedUp(ActionButtonType buttonType)
        {
            if (buttonType == ActionButtonType.EmoteBar)
            {
                m_EmotePanel.SetActive(!m_EmotePanel.activeSelf);
                return;
            }

            if (_mInputSender == null)
            {
                //nothing to do past this point if we don't have an InputSender.
                return;
            }

            // send input to complete the action associated with this button
            _mInputSender.RequestAction(_mButtonInfo[buttonType].CurAction.ActionID, SkillTriggerStyle.UIRelease);
        }

        void UpdateActionButton(ActionButtonInfo buttonInfo, Action action, bool isClickable = true)
        {
            // first find the info we need (sprite and description)
            Sprite sprite = null;
            string description = "";

            if (action != null)
            {
                sprite = action.Config.Icon;
                description = action.Config.Description;
            }

            // set up UI elements appropriately
            if (sprite == null)
            {
                buttonInfo.Button.gameObject.SetActive(false);
            }
            else
            {
                buttonInfo.Button.gameObject.SetActive(true);
                buttonInfo.Button.interactable = isClickable;
                buttonInfo.Button.image.sprite = sprite;
                buttonInfo.Tooltip.SetText(description);
            }

            // store the action type so that we can retrieve it in click events
            buttonInfo.CurAction = action;
        }
    }
}
