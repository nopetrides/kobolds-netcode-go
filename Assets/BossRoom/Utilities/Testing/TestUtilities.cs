using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;



namespace Unity.Multiplayer.Samples.Utilities
{
	public abstract class TestUtilities
	{
		private const float KMaxSceneLoadDuration = 10f;

		/// <summary>
		///     Helper wrapper method for asserting the completion of a network scene load to be used inside Playmode tests.
		///     A scene is either loaded successfully, or the loading process has timed out and will throw an exception.
		/// </summary>
		/// <param name="sceneName"> Name of scene </param>
		/// <param name="networkSceneManager"> NetworkSceneManager instance </param>
		/// <returns> IEnumerator to track scene load process </returns>
		public static IEnumerator AssertIsNetworkSceneLoaded(string sceneName, NetworkSceneManager networkSceneManager)
		{
			Assert.That(networkSceneManager != null, "NetworkSceneManager instance is null!");

			yield return new WaitForNetworkSceneLoad(sceneName, networkSceneManager);
		}

		/// <summary>
		///     Custom IEnumerator class to validate the loading of a Scene by name. If a scene load lasts longer than
		///     k_MaxSceneLoadDuration it is considered a timeout.
		/// </summary>
		public class WaitForSceneLoad : CustomYieldInstruction
		{
			private readonly float _mLoadSceneStart;

			private readonly float _mMaxLoadDuration;
			private readonly string _mSceneName;

			public WaitForSceneLoad(string sceneName, float maxLoadDuration = KMaxSceneLoadDuration)
			{
				_mLoadSceneStart = Time.time;
				_mSceneName = sceneName;
				_mMaxLoadDuration = maxLoadDuration;
			}

			public override bool keepWaiting
			{
				get
				{
					var scene = SceneManager.GetSceneByName(_mSceneName);

					var isSceneLoaded = scene.IsValid() && scene.isLoaded;

					if (Time.time - _mLoadSceneStart >= _mMaxLoadDuration)
						throw new Exception($"Timeout for scene load for scene name {_mSceneName}");

					return !isSceneLoaded;
				}
			}
		}

		/// <summary>
		///     Custom IEnumerator class to validate the loading of a Scene through Netcode for GameObjects by name.
		///     If a scene load lasts longer than k_MaxSceneLoadDuration it is considered a timeout.
		/// </summary>
		private class WaitForNetworkSceneLoad : CustomYieldInstruction
		{
			private bool _mIsNetworkSceneLoaded;

			private readonly float _mLoadSceneStart;

			private readonly float _mMaxLoadDuration;

			private readonly NetworkSceneManager _mNetworkSceneManager;
			private readonly string _mSceneName;

			public WaitForNetworkSceneLoad(
				string sceneName, NetworkSceneManager networkSceneManager,
				float maxLoadDuration = KMaxSceneLoadDuration)
			{
				_mLoadSceneStart = Time.time;
				_mSceneName = sceneName;
				_mMaxLoadDuration = maxLoadDuration;

				_mNetworkSceneManager = networkSceneManager;

				_mNetworkSceneManager.OnLoadEventCompleted += ConfirmSceneLoad;
			}

			public override bool keepWaiting
			{
				get
				{
					if (Time.time - _mLoadSceneStart >= _mMaxLoadDuration)
					{
						_mNetworkSceneManager.OnLoadEventCompleted -= ConfirmSceneLoad;

						throw new Exception($"Timeout for network scene load for scene name {_mSceneName}");
					}

					return !_mIsNetworkSceneLoaded;
				}
			}

			private void ConfirmSceneLoad(
				string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted,
				List<ulong> clientsTimedOut)
			{
				if (sceneName == _mSceneName)
				{
					_mIsNetworkSceneLoaded = true;

					_mNetworkSceneManager.OnLoadEventCompleted -= ConfirmSceneLoad;
				}
			}
		}
	}
}
