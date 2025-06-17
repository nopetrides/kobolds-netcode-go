using UnityEngine.InputSystem;

public static class KoboldBindingUtils
{
	public static string GetBindingDisplayString(InputAction action, string controlScheme)
	{
		int bindingIndex = -1;

		for (int i = 0; i < action.bindings.Count; i++)
		{
			if (action.bindings[i].groups.Contains(controlScheme))
			{
				bindingIndex = i;
				break;
			}
		}

		if (bindingIndex >= 0)
		{
			return action.GetBindingDisplayString(bindingIndex);
		}

		return action.GetBindingDisplayString(); // fallback
	}
}
