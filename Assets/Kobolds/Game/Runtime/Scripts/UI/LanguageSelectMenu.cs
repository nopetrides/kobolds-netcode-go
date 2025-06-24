using Kobolds.Runtime;
using P3T.Scripts.Managers;
using P3T.Scripts.UI;
using UnityEngine;
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

		public void ButtonContinue()
		{
			SceneMgr.Instance.LoadScene(GameScenes.AnimatedScene.ToString(), null);
		}
	}
}