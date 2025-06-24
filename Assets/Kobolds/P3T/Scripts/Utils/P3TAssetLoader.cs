using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Object = UnityEngine.Object;

namespace P3T.Scripts.Utils
{
	/// <summary>
	/// Loads assets from asset management system
	/// Current implementation uses Unity Addressables
	/// </summary>
	public static class P3TAssetLoader
	{
		#region Synchronous
		/// <summary>
		/// Load and immediately spawn the asset, does not return it
		/// For immediate spawning when the asset is finished loaded
		/// </summary>
		/// <param name="assetKey"></param>
		/// <param name="onComplete"></param>
		/// <returns></returns>
		public static void LoadAndSpawnAssetByKey(string assetKey, Action<Object> onComplete = null)
		{
			LoadAssetByKey(assetKey, (gameObject) =>
			{
				var obj = Object.Instantiate(gameObject);
				onComplete?.Invoke(obj);
			});
		}

		/// <summary>
		/// public accessor to load, and return an asset
		/// </summary>
		/// <param name="assetKey"></param>
		/// <param name="onComplete"></param>
		public static void LoadAndReturnStoredAssetByKey(string assetKey, Action<GameObject> onComplete)
		{
			LoadAssetByKey(assetKey, (asset) =>
			{
				onComplete?.Invoke(asset);
			});
		}
		
		/// <summary>
		/// Load and return the prefab, does not return the spawned object
		/// </summary>
		/// <param name="assetKey"></param>
		/// <param name="onComplete"></param>
		private static void LoadAssetByKey(string assetKey, Action<GameObject> onComplete)
		{
			AsyncOperationHandle<GameObject> loadAssetAsync = Addressables.LoadAssetAsync<GameObject>(assetKey);
			loadAssetAsync.Completed += (operation) =>
			{
				if (operation.Status != AsyncOperationStatus.Succeeded)
					Debug.LogError($"Failed to load {assetKey}");
				
				onComplete?.Invoke(operation.Result);
			};
		}

		/// <summary>
		/// Loads, spawns, and returns a list of objects by matching a label
		/// </summary>
		/// <param name="label"></param>
		/// <param name="onComplete"></param>
		public static void LoadAndSpawnStoredAssetsByLabel(string label, Action<List<GameObject>> onComplete = null)
		{
			LoadAssetsByLabel(label, (assets) =>
			{
				var gameObjects = new List<GameObject>();
				foreach (var a in assets)
				{
					gameObjects.Add(Object.Instantiate(a));
				}
				onComplete?.Invoke(gameObjects);
			});
		}

		/// <summary>
		/// public accessor to load all assets by label, does not spawn them into the scene
		/// </summary>
		/// <param name="label"></param>
		/// <param name="onComplete"></param>
		public static void LoadAndReturnStoredAssetsByLabel(string label, Action<List<GameObject>> onComplete)
		{
			LoadAssetsByLabel(label, (assets) => onComplete?.Invoke(assets));
		}
		
		/// <summary>
		/// Loads and returns all assets by label from addressables, does not spawn them into the scene
		/// </summary>
		/// <param name="label"></param>
		/// <param name="onComplete"></param>
		private static void LoadAssetsByLabel(string label, Action<List<GameObject>> onComplete)
		{
			var loadedObjects = new List<GameObject>();
			int assetsLoaded = 0;
			AsyncOperationHandle<IList<GameObject>> loadHandle = Addressables.LoadAssetsAsync<GameObject>(
				label, // Either a single key or a List of keys
				addressable =>
				{
					//Gets called for every loaded asset
					if (addressable != null)
					{
						loadedObjects.Add(addressable);
					}
					else
					{
						Debug.LogError($"Failed trying to load asset number {assetsLoaded} with label {label}");
					}
					assetsLoaded++;
				}, Addressables.MergeMode.Union, // How to combine multiple labels
				true); // Whether to fail if any asset fails to load
			loadHandle.Completed += (operation) =>
			{
				if (operation.Status != AsyncOperationStatus.Succeeded)
					Debug.LogError($"Tried to load assets with label {label} but some assets did not load.");
				onComplete?.Invoke(loadedObjects);
			};
		}
		
		
		/// <summary>
		/// Loads, spawns, and returns a list of objects with matching labels
		/// </summary>
		/// <param name="label"></param>
		/// <param name="onComplete"></param>
		public static void LoadAndSpawnStoredAssetsByLabel(string[] label, Action<List<GameObject>> onComplete = null)
		{
			LoadAssetsByLabel(label, (assets) =>
			{
				var gameObjects = new List<GameObject>();
				foreach (var a in assets)
				{
					gameObjects.Add(Object.Instantiate(a));
				}
				onComplete?.Invoke(gameObjects);
			});
		}

		/// <summary>
		/// public accessor to load all assets by labels, does not spawn them into the scene
		/// </summary>
		/// <param name="label"></param>
		/// <param name="onComplete"></param>
		public static void LoadAndReturnStoredAssetsByLabel(string[] label, Action<List<GameObject>> onComplete)
		{
			LoadAssetsByLabel(label, (assets) => onComplete?.Invoke(assets));
		}
		
		/// <summary>
		/// Loads and returns all assets by labels from addressables, does not spawn them into the scene
		/// </summary>
		/// <param name="label"></param>
		/// <param name="onComplete"></param>
		private static void LoadAssetsByLabel(string[] label, Action<List<GameObject>> onComplete)
		{
			var loadedObjects = new List<GameObject>();
			int assetsLoaded = 0;
			AsyncOperationHandle<IList<GameObject>> loadHandle = Addressables.LoadAssetsAsync<GameObject>(
				label, // Either a single key or a List of keys
				addressable =>
				{
					//Gets called for every loaded asset
					if (addressable != null)
					{
						loadedObjects.Add(addressable);
					}
					else
					{
						Debug.LogError($"Failed trying to load asset number {assetsLoaded} with label {label}");
					}
					assetsLoaded++;
				}, Addressables.MergeMode.Union, // How to combine multiple labels
				true); // Whether to fail if any asset fails to load
			loadHandle.Completed += (operation) =>
			{
				if (operation.Status != AsyncOperationStatus.Succeeded)
					Debug.LogError($"Tried to load assets with label {label} but some assets did not load.");
				onComplete?.Invoke(loadedObjects);
			};
		}
		#endregion
		
		#region Asynchronous
		/// <summary>
		/// Load and immediately spawn the asset, does not return it
		/// For immediate spawning when the asset is finished loaded
		/// </summary>
		/// <param name="assetKey"></param>
		/// <returns></returns>
		public static async Task<GameObject> LoadAndSpawnAssetByKeyAsync(string assetKey)
		{
			var prefab = await LoadAndReturnStoredAssetByKeyAsync(assetKey);
			
			var obj = Object.Instantiate(prefab);
			return obj;
		}

		/// <summary>
		/// public accessor to load, and return an asset
		/// </summary>
		/// <param name="assetKey"></param>
		public static async Task<GameObject> LoadAndReturnStoredAssetByKeyAsync(string assetKey)
		{
			return await LoadAssetByKeyAsync(assetKey);
		}
		
		/// <summary>
		/// Load and return the prefab, does not return the spawned object
		/// </summary>
		/// <param name="assetKey"></param>
		private static async Task<GameObject> LoadAssetByKeyAsync(string assetKey)
		{
			var loadAssetAsync = await Addressables.LoadAssetAsync<GameObject>(assetKey).Task;
			if (loadAssetAsync == null) 
				Debug.LogError($"Failed to load {assetKey}");
			return loadAssetAsync;
		}

		/// <summary>
		/// Loads, spawns, and returns a list of objects by matching a label
		/// </summary>
		/// <param name="label"></param>
		public static async Task<IList<GameObject>> LoadAndSpawnStoredAssetsByLabelAsync(string label)
		{
			var prefabs = await LoadAssetsByLabelAsync(label);
			
			var gameObjects = new List<GameObject>();
			foreach (var a in prefabs)
			{
				gameObjects.Add(Object.Instantiate(a));
			}

			return gameObjects;
		}

		/// <summary>
		/// public accessor to load all assets by label, does not spawn them into the scene
		/// </summary>
		/// <param name="label"></param>
		public static async Task<IList<GameObject>> LoadAndReturnStoredAssetsByLabelAsync(string label)
		{
			return await LoadAssetsByLabelAsync(label);
		}
		
		/// <summary>
		/// Loads and returns all assets by label from addressables, does not spawn them into the scene
		/// </summary>
		/// <param name="label"></param>
		private static async Task<IList<GameObject>> LoadAssetsByLabelAsync(string label)
		{
			int assetsLoaded = 0;
			return await Addressables.LoadAssetsAsync<GameObject>(
				label, // Either a single key or a List of keys
				addressable =>
				{
					//Gets called for every loaded asset
					if (addressable != null)
					{
						Debug.Log($"Successfully loaded asset number {assetsLoaded} : {addressable.name} with label {label}");
					}
					else
					{
						Debug.LogError($"Failed trying to load asset number {assetsLoaded} with label {label}");
					}
					assetsLoaded++;
				}, Addressables.MergeMode.Union, // How to combine multiple labels
				true).Task; // Whether to fail if any asset fails to load
		}
		
		/// <summary>
		/// Loads, spawns, and returns a list of objects with matching labels
		/// </summary>
		/// <param name="label"></param>
		public static async Task<IList<GameObject>> LoadAndSpawnStoredAssetsByLabelAsync(string[] label)
		{
			var gameObjects = new List<GameObject>();
			var prefabs = await LoadAssetsByLabelAsync(label);
			foreach (var a in prefabs)
			{
				gameObjects.Add(Object.Instantiate(a));
			}

			return gameObjects;
		}

		/// <summary>
		/// public accessor to load all assets by labels, does not spawn them into the scene
		/// </summary>
		/// <param name="label"></param>
		public static async Task<IList<GameObject>> LoadAndReturnStoredAssetsByLabelAsync(string[] label)
		{
			return await LoadAssetsByLabelAsync(label);
		}
		
		/// <summary>
		/// Loads and returns all assets by labels from addressables, does not spawn them into the scene
		/// </summary>
		/// <param name="label"></param>
		private static async Task<IList<GameObject>> LoadAssetsByLabelAsync(string[] label)
		{
			int assetsLoaded = 0;
			return await Addressables.LoadAssetsAsync<GameObject>(
				label, // Either a single key or a List of keys
				addressable =>
				{
					//Gets called for every loaded asset
					if (addressable != null)
					{
						Debug.Log($"Successfully loaded asset number {assetsLoaded} : {addressable.name} with label {label}");
					}
					else
					{
						Debug.LogError($"Failed trying to load asset number {assetsLoaded} with label {label}");
					}
					assetsLoaded++;
				}, Addressables.MergeMode.Union, // How to combine multiple labels
				true).Task; // Whether to fail if any asset fails to load
		}

		public static async Task<SceneInstance> LoadSceneAsync(string sceneName)
		{
			return await Addressables.LoadSceneAsync(sceneName).Task;
		}
		
		#endregion
	}
}
