using System;
using System.Threading.Tasks;
using Kobold.GameManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Kobold.UI
{
	public class KoboldMainMenuCanvas : MonoBehaviour
	{
		[SerializeField] private Button _socialHubButton;
		[SerializeField] private Button _missionButton;
		[SerializeField] private Button _settingsButton;
		[SerializeField] private Button _quitButton;

		[SerializeField] private GameObject _loadingSpinnerPanel;

		public Action OnSettings;

		private bool _allowInteraction;

		private void Awake()
		{
			_socialHubButton.onClick.AddListener(OnSocialHubPressed);
			_missionButton.onClick.AddListener(OnMissionPressed);
			_settingsButton.onClick.AddListener(OnSettingsPressed);
			_quitButton.onClick.AddListener(OnQuitPressed);
		}

		private void OnDestroy()
		{
			_socialHubButton.onClick.RemoveListener(OnSocialHubPressed);
			_missionButton.onClick.RemoveListener(OnMissionPressed);
			_settingsButton.onClick.RemoveListener(OnSettingsPressed);
			_quitButton.onClick.RemoveListener(OnQuitPressed);
		}

		private void OnEnable()
		{
			_socialHubButton.Select();
			UISelectionIndicator.LastValidSelectable = _socialHubButton.gameObject;
			_allowInteraction = true;
			_loadingSpinnerPanel.SetActive(false);
		}

		private void OnSocialHubPressed()
		{
			if (!_allowInteraction) return;
			
			Debug.Log("[KoboldHomeScreenView] Social Hub button pressed");

			_allowInteraction = false;
			_loadingSpinnerPanel.SetActive(true);
			
			KoboldEventHandler.OnSocialHubConnectionCompleted += OnSocialHubConnected;
			var playerName = PlayerPrefs.GetString("PlayerName", "Kobold");
			var sessionName = PlayerPrefs.GetString("LastSession", nameof(SceneNames.KoboldHub));

			KoboldEventHandler.StartSocialHubPressed(playerName, sessionName);
		}

		private void OnMissionPressed()
		{
			if (!_allowInteraction) return;
			
			Debug.Log("[KoboldHomeScreenView] Quick Mission button pressed");

			KoboldEventHandler.OnMissionConnectionCompleted += OnMissionConnected;
			_allowInteraction = false;
			_loadingSpinnerPanel.SetActive(true);

			var playerName = PlayerPrefs.GetString("PlayerName", "Kobold");
			KoboldEventHandler.StartKoboldMissionPressed(playerName, "QuickMatch");
		}

		private void OnSettingsPressed()
		{
			if (!_allowInteraction) return;
			OnSettings?.Invoke();
		}
	
		private void OnQuitPressed()
		{
			if (!_allowInteraction) return;
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
		}
		
		private void OnSocialHubConnected(Task task, string sessionName)
		{
			KoboldEventHandler.OnSocialHubConnectionCompleted -= OnSocialHubConnected;
			
			_allowInteraction = true;
			_loadingSpinnerPanel.SetActive(false);
			
			if (!task.IsCompletedSuccessfully)
			{
				Debug.LogError($"[KoboldEventHandler.OnSocialHubConnected] Failed to connect to session {sessionName}");
			}
		}
		
		private void OnMissionConnected(Task task, string sessionName)
		{
			KoboldEventHandler.OnMissionConnectionCompleted -= OnMissionConnected;
			
			_allowInteraction = true;
			_loadingSpinnerPanel.SetActive(false);
			
			if (!task.IsCompletedSuccessfully)
			{
				Debug.LogError($"[KoboldEventHandler.OnMissionConnected] Failed to connect to session {sessionName}");
			}
		}
	}
}