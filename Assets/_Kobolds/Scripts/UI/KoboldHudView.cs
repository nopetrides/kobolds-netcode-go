using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.UIElements;
using Kobold.UI.Components;

namespace Kobold.UI
{
    public class KoboldHUDView : KoboldUIView
    {
        [SerializeField] private InputActionAsset inputActions;

        private KoboldInputPromptLabel _movePrompt;
        private KoboldInputPromptLabel _lookPrompt;
        private KoboldInputPromptLabel _jumpPrompt;
        private KoboldInputPromptLabel _sprintPrompt;
        private KoboldInputPromptLabel _gripLeftPrompt;
        private KoboldInputPromptLabel _gripRightPrompt;
        private KoboldInputPromptLabel _latchPrompt;
        private KoboldInputPromptLabel _flopPrompt;

        private string _controlScheme;

        public override void Initialize(VisualElement root)
        {
            base.Initialize(root);

            var playerMap = inputActions.FindActionMap("Player");
            if (playerMap == null)
            {
                Debug.LogError("[KoboldHUDView] 'Player' action map not found.");
                return;
            }

            _movePrompt = MRoot.Q<KoboldInputPromptLabel>("move-prompt");
            _lookPrompt = MRoot.Q<KoboldInputPromptLabel>("look-prompt");
            _jumpPrompt = MRoot.Q<KoboldInputPromptLabel>("jump-prompt");
            _sprintPrompt = MRoot.Q<KoboldInputPromptLabel>("sprint-prompt");
            _gripLeftPrompt = MRoot.Q<KoboldInputPromptLabel>("grip-left-prompt");
            _gripRightPrompt = MRoot.Q<KoboldInputPromptLabel>("grip-right-prompt");
            _latchPrompt = MRoot.Q<KoboldInputPromptLabel>("latch-prompt");
            _flopPrompt = MRoot.Q<KoboldInputPromptLabel>("flop-prompt");

            BindPrompt(_movePrompt, playerMap.FindAction("Move"), "Move");
            BindPrompt(_lookPrompt, playerMap.FindAction("Look"), "Look");
            BindPrompt(_jumpPrompt, playerMap.FindAction("Jump"), "Jump");
            BindPrompt(_sprintPrompt, playerMap.FindAction("Sprint"), "Sprint");
            BindPrompt(_gripLeftPrompt, playerMap.FindAction("GripLeft"), "Grip Left");
            BindPrompt(_gripRightPrompt, playerMap.FindAction("GripRight"), "Grip Right");
            BindPrompt(_latchPrompt, playerMap.FindAction("Latch"), "Latch/Detach");
            BindPrompt(_flopPrompt, playerMap.FindAction("Flop"), "Flop");
        }

        private void BindPrompt(KoboldInputPromptLabel prompt, InputAction action, string description)
        {
            if (prompt == null || action == null) return;
            prompt.Bind(action, description); // Assumes you extend KoboldInputPromptLabel with a Bind() method
        }

        protected override void RegisterEvents()
        {
            InputUser.onChange += OnInputUserChanged;
        }

        protected override void UnregisterEvents()
        {
            InputUser.onChange -= OnInputUserChanged;

            _movePrompt?.Dispose();
            _lookPrompt?.Dispose();
            _jumpPrompt?.Dispose();
            _sprintPrompt?.Dispose();
            _gripLeftPrompt?.Dispose();
            _gripRightPrompt?.Dispose();
            _latchPrompt?.Dispose();
            _flopPrompt?.Dispose();
        }

        private void OnInputUserChanged(InputUser user, InputUserChange change, InputDevice device)
        {
            if (change == InputUserChange.ControlSchemeChanged && user.controlScheme != null)
            {
                _controlScheme = user.controlScheme.Value.name;

                // Trigger prompts to refresh if needed
                _movePrompt?.Refresh();
                _lookPrompt?.Refresh();
                _jumpPrompt?.Refresh();
                _sprintPrompt?.Refresh();
                _gripLeftPrompt?.Refresh();
                _gripRightPrompt?.Refresh();
                _latchPrompt?.Refresh();
                _flopPrompt?.Refresh();
            }
        }
    }
}
