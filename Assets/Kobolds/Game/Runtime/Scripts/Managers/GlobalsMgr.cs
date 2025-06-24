using System.Threading.Tasks;
using Kobolds.UI;
using P3T.Scripts.Managers;
using P3T.Scripts.Utils;
using UnityEngine;

namespace Kobolds.Runtime.Managers
{
	/// <summary>
	///     The "don't destroy on load" parent that contains all other global managers
	/// </summary>
	public class GlobalsMgr : Singleton<GlobalsMgr>
	{
		[SerializeField] private string[] DefaultLoadAssetLabels = {"Preload"};

		public override void Awake()
		{
			base.Awake();
			if (Instance == this)
				DontDestroyOnLoad(gameObject);
			
			_ = LoadDefaultAssets();
		}

		private async Task LoadDefaultAssets()
		{
			var prefabs = await P3TAssetLoader.LoadAndReturnStoredAssetsByLabelAsync(DefaultLoadAssetLabels);

			foreach (var go in prefabs)
			{
				Instantiate(go, transform);
			}

			if (SceneMgr.Instance == null)
			{
				gameObject.AddComponent<SceneMgr>();
			}
			
			SceneMgr.Instance.LoadScene(GameScenes.LanguageSelectScene.ToString(), typeof(LanguageSelectMenu));
		}
	}
}