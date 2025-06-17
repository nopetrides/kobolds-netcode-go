using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.UIElements;

namespace Kobold.UI.Components
{
	[UxmlElement]
	public partial class KoboldInputPromptLabel : Label
	{
		private InputAction _action;
		private string _description;
		private string _controlScheme;

		public KoboldInputPromptLabel()
		{
			AddToClassList("input-prompt");
			InputUser.onChange += OnInputUserChanged;
		}

		public void Bind(InputAction action, string description)
		{
			_action = action;
			_description = description;
			UpdatePrompt();
		}

		public void Refresh()
		{
			UpdatePrompt();
		}

		public void Dispose()
		{
			InputUser.onChange -= OnInputUserChanged;
		}

		private void OnInputUserChanged(InputUser user, InputUserChange change, InputDevice device)
		{
			if (change == InputUserChange.ControlSchemeChanged && user.controlScheme != null)
			{
				_controlScheme = user.controlScheme.Value.name;
				UpdatePrompt();
			}
		}

		private void UpdatePrompt()
		{
			if (_action == null || string.IsNullOrEmpty(_description)) return;

			string scheme = !string.IsNullOrEmpty(_controlScheme)
				? _controlScheme
				: InputUser.all.Count > 0 && InputUser.all[0].controlScheme != null
					? InputUser.all[0].controlScheme.Value.name
					: "KeyboardMouse";

			var bindingText = KoboldBindingUtils.GetBindingDisplayString(_action, scheme);
			text = $"Press {bindingText} to {_description}";
		}
	}
}
