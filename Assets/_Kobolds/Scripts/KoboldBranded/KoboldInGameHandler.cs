using Kobold.GameManagement;
using P3T.Scripts.Managers;
using UnityEngine;

namespace Kobold
{
	public class KoboldInGameHandler : MonoBehaviour
	{
		void Start()
		{
			KoboldInputSystemManager.Instance.EnableGameplayMode();
			//KoboldEventHandler.OnReturnToMainMenuButtonPressed += LoadMainMenuScene;
			KoboldEventHandler.OnExitedSession += LoadMainMenuScene;
			KoboldEventHandler.OnAllBossesDefeated += LoadMainMenuScene;
		}

		void OnDestroy()
		{
			//KoboldEventHandler.OnReturnToMainMenuButtonPressed -= LoadMainMenuScene;
			KoboldEventHandler.OnExitedSession -= LoadMainMenuScene;
			KoboldEventHandler.OnAllBossesDefeated -= LoadMainMenuScene;
		}


		private void LoadMainMenuScene()
		{
			KoboldInputSystemManager.Instance.EnableUIMode();
			SceneMgr.Instance?.LoadScene(nameof(SceneNames.KoboldMainMenu), null);
		}
		
	}
}
