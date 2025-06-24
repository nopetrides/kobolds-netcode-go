using System;
using Kobolds.Runtime;
using P3T.Scripts.Managers;
using P3T.Scripts.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kobolds.UI
{
	public class LanguageSelectMenu : MenuBase
	{
		[SerializeField] private Button ContinueButton;

		private void Awake()
		{
			ContinueButton.Select();
		}

		private void OnEnable()
		{
			ContinueButton.onClick.AddListener(ButtonContinue);
		}
		
		private void OnDisable()
		{
			ContinueButton.onClick.RemoveListener(ButtonContinue);
		}

		public void ButtonContinue()
		{
			EventSystem.current.SetSelectedGameObject(null);
			SceneMgr.Instance.LoadScene(nameof(GameScenes.AnimatedScene), null);
		}
	}
}