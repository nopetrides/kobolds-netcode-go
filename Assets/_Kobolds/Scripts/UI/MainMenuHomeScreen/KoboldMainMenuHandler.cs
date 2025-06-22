using System;
using System.Threading.Tasks;
using Kobold.GameManagement;
using UnityEngine;

namespace Kobold.UI
{
	internal class KoboldMainMenuHandler : MonoBehaviour
	{
		private void Start()
		{
			KoboldInputSystemManager.Instance.EnableUIMode();
			KoboldEventHandler.OnSocialHubConnectionCompleted += OnConnectToSessionCompleted;
			KoboldEventHandler.OnMissionConnectionCompleted += OnConnectToMissionCompleted;
		}

		private void OnDestroy()
		{
			KoboldEventHandler.OnSocialHubConnectionCompleted -= OnConnectToSessionCompleted;
			KoboldEventHandler.OnMissionConnectionCompleted -= OnConnectToMissionCompleted;
		}


		private void OnConnectToSessionCompleted(Task task, string sessionName)
		{
			try
			{
				if (task.IsCompletedSuccessfully) KoboldEventHandler.LoadInGameScene("KoboldHub");
				else Debug.LogError("[KoboldEventHandler.OnConnectToSessionCompleted] Failed to connect");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[KoboldEventHandler.OnConnectToSessionCompleted] Failed to connect {ex}");
			}
		}


		private void OnConnectToMissionCompleted(Task task, string sessionName)
		{
			try
			{
				if (task.IsCompletedSuccessfully) KoboldEventHandler.LoadInGameScene("KoboldMission");
				else Debug.LogError("[KoboldEventHandler.OnConnectToMissionCompleted] Failed to connect");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[KoboldEventHandler.OnConnectToMissionCompleted] Failed to connect {ex}");
			}
		}
	}
}
