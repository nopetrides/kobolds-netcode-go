using System;
using System.Threading.Tasks;
using P3T.Scripts.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace P3T.Scripts.Managers
{
    /// <summary>
    ///     Manages the current scene and scene transitions
    ///     Ensures the UI clears and opens to the correct menu after loading is complete
    /// </summary>
    public class SceneMgr : Singleton<SceneMgr>
	{
		public void LoadScene(string sceneToLoad, Type menuToOpen)
		{
			_ = PerformLoadSequence(sceneToLoad, menuToOpen);
		}

		private async Task PerformLoadSequence(string sceneToLoad, Type menuToOpen)
		{
			UiMgr.Instance.CloseAllMenus();

			await UiMgr.Instance.ShowMenu<ScreenFadeOverlay>();
			
			await P3TAssetLoader.LoadSceneAsync(sceneToLoad);

			UiMgr.Instance.HideMenu(typeof(ScreenFadeOverlay));
			
			await UiMgr.Instance.ShowMenu(menuToOpen);
			

			var method = typeof(UiMgr).GetMethod(
				nameof(UiMgr.ShowMenu), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			
			if (method == null)
			{
				throw new InvalidOperationException($"Method {nameof(UiMgr.ShowMenu)} not found in UiMgr");
			}
			
			var genericMethod = method.MakeGenericMethod(menuToOpen);
			
			// Invoke the method, which returns Task<T>
			var task = (Task)genericMethod.Invoke(UiMgr.Instance, new object[]{menuToOpen});
			
			if (task == null)
			{
				throw new InvalidOperationException("ShowMenu<T> did not return a valid Task.");
			}

			await task;
			
			Debug.Log("Scene loaded and menu opened");
			
			/*
			// if needed to run operations on MenuBase
			if (task == null)
			{
				throw new InvalidOperationException($"Generic method for {nameof(UiMgr.ShowMenu)} did not return a valid Task.");
			}
			
			try
			{
				// Await and extract the result safely
				var menuBase = await ConvertTaskResultToMenuBase(task);

				if (menuBase == null)
				{
					throw new InvalidOperationException("The resulting menu is null.");
				}

				return menuBase;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error showing menu: {ex.Message}");
			}
			*/
		}
		
		/*
		// Helper method to safely await the task and extract the result
		private async Task<MenuBase> ConvertTaskResultToMenuBase(Task task)
		{
			await task; // Await completion of the task

			var resultProperty = task.GetType().GetProperty("Result");

			if (resultProperty == null)
			{
				throw new InvalidOperationException("Task does not have a Result property.");
			}

			return resultProperty.GetValue(task) as MenuBase;
		}*/
	}
}