using Kobold.GameManagement;
using UnityEngine;

namespace Kobold
{
	public class KoboldInGameHandler : MonoBehaviour
	{
		void Start()
		{
			KoboldInputSystemManager.Instance.EnableGameplayMode();
			KoboldEventHandler.OnReturnToMainMenuButtonPressed += LoadMainMenuScene;
			KoboldEventHandler.OnExitedSession += LoadMainMenuScene;
		}

		void OnDestroy()
		{
			KoboldEventHandler.OnReturnToMainMenuButtonPressed -= LoadMainMenuScene;
			KoboldEventHandler.OnExitedSession -= LoadMainMenuScene;
		}

		private void LoadMainMenuScene()
		{
			KoboldEventHandler.LoadMainMenuScene(nameof(SceneNames.KoboldMainMenu));
		}
	}
}
