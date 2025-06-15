using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;



namespace Unity.Multiplayer.Samples.Utilities
{
	/// <summary>
	///     This NetworkBehavior, when added to a GameObject containing a collider (or multiple colliders) with the
	///     IsTrigger property On, allows the server to load or unload a scene additively according to the position of
	///     player-owned objects. The scene is loaded when there is at least one NetworkObject with the specified tag that
	///     enters its collider. It also unloads it when all such NetworkObjects leave the collider, after a specified
	///     delay to prevent it from repeatedly loading and unloading the same scene.
	/// </summary>
	public class ServerAdditiveSceneLoader : NetworkBehaviour
	{
		[SerializeField] private float m_DelayBeforeUnload = 5.0f;

		[SerializeField] private string m_SceneName;

		/// <summary>
		///     We assume that all NetworkObjects with this tag are player-owned
		/// </summary>
		[SerializeField] private string m_PlayerTag;

		/// <summary>
		///     We keep the clientIds of every player-owned object inside the collider's volume
		/// </summary>
		private List<ulong> _mPlayersInTrigger;

		private SceneState _mSceneState = SceneState.Unloaded;

		private Coroutine _mUnloadCoroutine;

		private bool IsActive => IsServer && IsSpawned;

		private void FixedUpdate()
		{
			if (IsActive) // make sure that OnNetworkSpawn has been called before this
			{
				if (_mSceneState == SceneState.Unloaded && _mPlayersInTrigger.Count > 0)
				{
					var status = NetworkManager.SceneManager.LoadScene(m_SceneName, LoadSceneMode.Additive);
					// if successfully started a LoadScene event, set state to Loading
					if (status == SceneEventProgressStatus.Started) _mSceneState = SceneState.Loading;
				}
				else if (_mSceneState == SceneState.Loaded && _mPlayersInTrigger.Count == 0)
				{
					// using a coroutine here to add a delay before unloading the scene
					_mUnloadCoroutine = StartCoroutine(WaitToUnloadCoroutine());
					_mSceneState = SceneState.WaitingToUnload;
				}
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (IsActive) // make sure that OnNetworkSpawn has been called before this
				if (other.CompareTag(m_PlayerTag) && other.TryGetComponent(out NetworkObject networkObject))
				{
					_mPlayersInTrigger.Add(networkObject.OwnerClientId);

					if (_mUnloadCoroutine != null)
					{
						// stopping the unloading coroutine since there is now a player-owned NetworkObject inside
						StopCoroutine(_mUnloadCoroutine);
						if (_mSceneState == SceneState.WaitingToUnload) _mSceneState = SceneState.Loaded;
					}
				}
		}

		private void OnTriggerExit(Collider other)
		{
			if (IsActive) // make sure that OnNetworkSpawn has been called before this
				if (other.CompareTag(m_PlayerTag) && other.TryGetComponent(out NetworkObject networkObject))
					_mPlayersInTrigger.Remove(networkObject.OwnerClientId);
		}

		public override void OnNetworkSpawn()
		{
			if (IsServer)
			{
				// Adding this to remove all pending references to a specific client when they disconnect, since objects
				// that are destroyed do not generate OnTriggerExit events.
				NetworkManager.OnClientDisconnectCallback += RemovePlayer;

				NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
				_mPlayersInTrigger = new List<ulong>();
			}
		}

		public override void OnNetworkDespawn()
		{
			if (IsServer)
			{
				NetworkManager.OnClientDisconnectCallback -= RemovePlayer;
				NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
			}
		}

		private void OnSceneEvent(SceneEvent sceneEvent)
		{
			if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted && sceneEvent.SceneName == m_SceneName)
				_mSceneState = SceneState.Loaded;
			else if (sceneEvent.SceneEventType == SceneEventType.UnloadEventCompleted &&
					sceneEvent.SceneName == m_SceneName) _mSceneState = SceneState.Unloaded;
		}

		private void RemovePlayer(ulong clientId)
		{
			// remove all references to this clientId. There could be multiple references if a single client owns
			// multiple NetworkObjects with the m_PlayerTag, or if this script's GameObject has overlapping colliders
			while (_mPlayersInTrigger.Remove(clientId))
			{
			}
		}

		private IEnumerator WaitToUnloadCoroutine()
		{
			yield return new WaitForSeconds(m_DelayBeforeUnload);
			var scene = SceneManager.GetSceneByName(m_SceneName);
			if (scene.isLoaded)
			{
				var status = NetworkManager.SceneManager.UnloadScene(SceneManager.GetSceneByName(m_SceneName));
				// if successfully started an UnloadScene event, set state to Unloading, if not, reset state to Loaded so a new Coroutine will start
				_mSceneState = status == SceneEventProgressStatus.Started ? SceneState.Unloading : SceneState.Loaded;
			}
		}

		private enum SceneState
		{
			Loaded,
			Unloaded,
			Loading,
			Unloading,
			WaitingToUnload
		}
	}
}
