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
			KoboldEventHandler.OnConnectToSessionCompleted += OnConnectToSessionCompleted;
			KoboldEventHandler.OnSocialHubConnectionCompleted += OnConnectToSessionCompleted;
		}

		private void OnDestroy()
		{
			KoboldEventHandler.OnConnectToSessionCompleted -= OnConnectToSessionCompleted;
			KoboldEventHandler.OnSocialHubConnectionCompleted -= OnConnectToSessionCompleted;
		}

		private void OnConnectToSessionCompleted(Task task, string sessionName)
		{
			try
			{
				if (task.IsCompletedSuccessfully) KoboldEventHandler.LoadInGameScene("KoboldHub");
				else Debug.LogError("[KoboldEventHandler.OnConnectToSessionCompleted] Failed to connect");
			}
			catch (Exception e)
			{
				Debug.LogError("[KoboldEventHandler.OnConnectToSessionCompleted] Failed to connect");
			}
		}
	}
}
