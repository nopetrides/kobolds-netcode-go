using Kobold.UI;
using UnityEngine;

namespace Kobold.GameManagement
{
	/// <summary>
	///     Simple controller that shows the main menu when the scene loads
	/// </summary>
	public class MainMenuSceneController : MonoBehaviour
	{
		[Header("Settings")]
		[SerializeField] private float _delayBeforeShowingMenu = 0.1f;

		[SerializeField] private bool _hideCurrentMenuFirst = true;

		private void Start()
		{
			// Small delay to ensure all systems are ready
			if (_delayBeforeShowingMenu > 0)
				Invoke(nameof(ShowMainMenu), _delayBeforeShowingMenu);
			else
				ShowMainMenu();
		}

		private void OnDestroy()
		{
			// Hide menu when leaving scene
			KoboldUISystem.Instance?.HideCurrentMenu();
		}

		private void ShowMainMenu()
		{
			var uiSystem = KoboldUISystem.Instance;
			if (uiSystem == null)
			{
				Debug.LogError(
					"[MainMenuSceneController] KoboldUISystem not found! It should be created in the boot scene.");
				return;
			}

			// Hide any current menu if needed
			if (_hideCurrentMenuFirst) uiSystem.HideCurrentMenu();

			// Show the main menu
			uiSystem.ShowMenu(KoboldMenu.MainMenu);

			Debug.Log("[MainMenuSceneController] Main menu displayed");
		}
	}
}
