using System.Threading.Tasks;
using Kobold.Input;
using UnityEngine;

namespace Kobold.GameManagement
{
	public class KoboldMainMenuHandler : MonoBehaviour
	{
		private void Start()
		{
			// Ensure we're in UI mode for the menu
			if (KoboldInputSystemManager.Instance != null) KoboldInputSystemManager.Instance.EnableUIMode();

			KoboldEventHandler.OnConnectToSessionCompleted += OnConnectToSessionCompleted;
			KoboldEventHandler.OnSocialHubConnectionCompleted += OnSocialHubConnectionCompleted;
			KoboldEventHandler.OnMissionConnectionCompleted += OnMissionConnectionCompleted;
		}

		private void OnDestroy()
		{
			KoboldEventHandler.OnConnectToSessionCompleted -= OnConnectToSessionCompleted;
			KoboldEventHandler.OnSocialHubConnectionCompleted -= OnSocialHubConnectionCompleted;
			KoboldEventHandler.OnMissionConnectionCompleted -= OnMissionConnectionCompleted;
		}

		private void OnConnectToSessionCompleted(Task task, string sessionName)
		{
			if (task.IsCompletedSuccessfully)
				// Default behavior - assume social hub
				KoboldEventHandler.LoadInGameScene();
		}

		private void OnSocialHubConnectionCompleted(Task task, string sessionName)
		{
			if (task.IsCompletedSuccessfully) KoboldEventHandler.LoadInGameScene();
		}

		private void OnMissionConnectionCompleted(Task task, string sessionName)
		{
			if (task.IsCompletedSuccessfully)
				// Load mission scene or mission lobby
				KoboldEventHandler.LoadMissionScene("MissionLobby");
		}
	}
}
