using System;
using Kobold.GameManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Kobold.UI
{
	public class PauseMenu : MonoBehaviour
	{
		[SerializeField] private Button _resumeButton;
		[SerializeField] private Button _settingsButton;
		[SerializeField] private Button _mainMenuButton;

		private KoboldCanvasManager _canvasManager;

		public Action OnResume;
		public Action OnSettings;

		public void OnEnable()
		{
			_resumeButton.onClick.AddListener(OnResumePressed);
			_settingsButton.onClick.AddListener(OnSettingsPressed);
			_mainMenuButton.onClick.AddListener(OnMainMenuPressed);
			
			_resumeButton.Select();
			UISelectionIndicator.LastValidSelectable = _resumeButton.gameObject;
		}

		private void OnDisable()
		{
			_resumeButton.onClick.RemoveListener(OnResumePressed);
			_mainMenuButton.onClick.RemoveListener(OnMainMenuPressed);
			_settingsButton.onClick.RemoveListener(OnSettingsPressed);
		}

		private void OnResumePressed()
		{
			OnResume?.Invoke();
		}

		private void OnSettingsPressed()
		{
			OnSettings?.Invoke();
		}

		private void OnMainMenuPressed()
		{
			KoboldEventHandler.ReturnToMainMenuPressed();
		}
	}
}
