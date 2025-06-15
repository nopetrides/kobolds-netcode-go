using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;



namespace Unity.Multiplayer.Samples.Utilities
{
	/// <summary>
	///     This script handles the use of a loading screen with a progress bar and the name of the loaded scene shown. It
	///     must be started and stopped from outside this script. It also allows updating the loading screen when a new
	///     loading operation starts before the loading screen is stopped.
	/// </summary>
	public class ClientLoadingScreen : MonoBehaviour
	{
		[SerializeField] private CanvasGroup m_CanvasGroup;

		[SerializeField] private float m_DelayBeforeFadeOut = 0.5f;

		[SerializeField] private float m_FadeOutDuration = 0.1f;

		[SerializeField] private Slider m_ProgressBar;

		[SerializeField] private TMP_Text m_SceneName;

		[SerializeField] private List<Slider> m_OtherPlayersProgressBars;

		[SerializeField] private List<Text> m_OtherPlayerNamesTexts;

		[SerializeField]
		protected LoadingProgressManager m_LoadingProgressManager;

		private Coroutine _mFadeOutCoroutine;

		protected Dictionary<ulong, LoadingProgressBar> MLoadingProgressBars = new();

		private bool _mLoadingScreenRunning;

		private void Awake()
		{
			DontDestroyOnLoad(this);
			Assert.AreEqual(
				m_OtherPlayersProgressBars.Count, m_OtherPlayerNamesTexts.Count,
				"There should be the same number of progress bars and name labels");
		}

		private void Start()
		{
			SetCanvasVisibility(false);
			m_LoadingProgressManager.OnTrackersUpdated += OnProgressTrackersUpdated;
		}

		private void Update()
		{
			if (_mLoadingScreenRunning) m_ProgressBar.value = m_LoadingProgressManager.LocalProgress;
		}

		private void OnDestroy()
		{
			m_LoadingProgressManager.OnTrackersUpdated -= OnProgressTrackersUpdated;
		}

		private void OnProgressTrackersUpdated()
		{
			// deactivate progress bars of clients that are no longer tracked
			var clientIdsToRemove = new List<ulong>();
			foreach (var clientId in MLoadingProgressBars.Keys)
				if (!m_LoadingProgressManager.ProgressTrackers.ContainsKey(clientId))
					clientIdsToRemove.Add(clientId);

			foreach (var clientId in clientIdsToRemove) RemoveOtherPlayerProgressBar(clientId);

			// Add progress bars for clients that are now tracked
			foreach (var progressTracker in m_LoadingProgressManager.ProgressTrackers)
			{
				var clientId = progressTracker.Key;
				if (clientId != NetworkManager.Singleton.LocalClientId && !MLoadingProgressBars.ContainsKey(clientId))
					AddOtherPlayerProgressBar(clientId, progressTracker.Value);
			}
		}

		public void StopLoadingScreen()
		{
			if (_mLoadingScreenRunning)
			{
				if (_mFadeOutCoroutine != null) StopCoroutine(_mFadeOutCoroutine);
				_mFadeOutCoroutine = StartCoroutine(FadeOutCoroutine());
			}
		}

		public void StartLoadingScreen(string sceneName)
		{
			SetCanvasVisibility(true);
			_mLoadingScreenRunning = true;
			UpdateLoadingScreen(sceneName);
			ReinitializeProgressBars();
		}

		private void ReinitializeProgressBars()
		{
			// deactivate progress bars of clients that are no longer tracked
			var clientIdsToRemove = new List<ulong>();
			foreach (var clientId in MLoadingProgressBars.Keys)
				if (!m_LoadingProgressManager.ProgressTrackers.ContainsKey(clientId))
					clientIdsToRemove.Add(clientId);

			foreach (var clientId in clientIdsToRemove) RemoveOtherPlayerProgressBar(clientId);

			for (var i = 0; i < m_OtherPlayersProgressBars.Count; i++)
			{
				m_OtherPlayersProgressBars[i].gameObject.SetActive(false);
				m_OtherPlayerNamesTexts[i].gameObject.SetActive(false);
			}

			var index = 0;

			foreach (var progressTracker in m_LoadingProgressManager.ProgressTrackers)
			{
				var clientId = progressTracker.Key;
				if (clientId != NetworkManager.Singleton.LocalClientId) UpdateOtherPlayerProgressBar(clientId, index++);
			}
		}

		protected virtual void UpdateOtherPlayerProgressBar(ulong clientId, int progressBarIndex)
		{
			MLoadingProgressBars[clientId].ProgressBar = m_OtherPlayersProgressBars[progressBarIndex];
			MLoadingProgressBars[clientId].ProgressBar.gameObject.SetActive(true);
			MLoadingProgressBars[clientId].NameText = m_OtherPlayerNamesTexts[progressBarIndex];
			MLoadingProgressBars[clientId].NameText.gameObject.SetActive(true);
		}

		protected virtual void AddOtherPlayerProgressBar(
			ulong clientId, NetworkedLoadingProgressTracker progressTracker)
		{
			if (MLoadingProgressBars.Count < m_OtherPlayersProgressBars.Count &&
				MLoadingProgressBars.Count < m_OtherPlayerNamesTexts.Count)
			{
				var index = MLoadingProgressBars.Count;
				MLoadingProgressBars[clientId] = new LoadingProgressBar(
					m_OtherPlayersProgressBars[index], m_OtherPlayerNamesTexts[index]);
				progressTracker.Progress.OnValueChanged += MLoadingProgressBars[clientId].UpdateProgress;
				MLoadingProgressBars[clientId].ProgressBar.value = progressTracker.Progress.Value;
				MLoadingProgressBars[clientId].ProgressBar.gameObject.SetActive(true);
				MLoadingProgressBars[clientId].NameText.gameObject.SetActive(true);
				MLoadingProgressBars[clientId].NameText.text = $"Client {clientId}";
			}
			else
			{
				throw new Exception("There are not enough progress bars to track the progress of all the players.");
			}
		}

		private void RemoveOtherPlayerProgressBar(
			ulong clientId, NetworkedLoadingProgressTracker progressTracker = null)
		{
			if (progressTracker != null)
				progressTracker.Progress.OnValueChanged -= MLoadingProgressBars[clientId].UpdateProgress;
			MLoadingProgressBars[clientId].ProgressBar.gameObject.SetActive(false);
			MLoadingProgressBars[clientId].NameText.gameObject.SetActive(false);
			MLoadingProgressBars.Remove(clientId);
		}

		public void UpdateLoadingScreen(string sceneName)
		{
			if (_mLoadingScreenRunning)
			{
				m_SceneName.text = sceneName;
				if (_mFadeOutCoroutine != null) StopCoroutine(_mFadeOutCoroutine);
			}
		}

		private void SetCanvasVisibility(bool visible)
		{
			m_CanvasGroup.alpha = visible ? 1 : 0;
			m_CanvasGroup.blocksRaycasts = visible;
		}

		private IEnumerator FadeOutCoroutine()
		{
			yield return new WaitForSeconds(m_DelayBeforeFadeOut);
			_mLoadingScreenRunning = false;

			float currentTime = 0;
			while (currentTime < m_FadeOutDuration)
			{
				m_CanvasGroup.alpha = Mathf.Lerp(1, 0, currentTime / m_FadeOutDuration);
				yield return null;
				currentTime += Time.deltaTime;
			}

			SetCanvasVisibility(false);
		}

		protected class LoadingProgressBar
		{
			public LoadingProgressBar(Slider otherPlayerProgressBar, Text otherPlayerNameText)
			{
				ProgressBar = otherPlayerProgressBar;
				NameText = otherPlayerNameText;
			}

			public Slider ProgressBar { get; set; }

			public Text NameText { get; set; }

			public void UpdateProgress(float value, float newValue)
			{
				ProgressBar.value = newValue;
			}
		}
	}
}
