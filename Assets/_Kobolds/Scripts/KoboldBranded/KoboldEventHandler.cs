using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kobold.GameManagement
{
	/// <summary>
	///     Handles game-wide events
	/// </summary>
	/// <remarks>
	///     Copied / extended from <see cref="Unity.Multiplayer.Samples.SocialHub.GameManagement.GameplayEventHandler" />
	/// </remarks>
	public static class KoboldEventHandler
	{
		// Network Events
		public static event Action<NetworkObject> OnNetworkObjectDespawned;
		public static event Action<NetworkObject, ulong, ulong> OnNetworkObjectOwnershipChanged;

		// Session Events - Split into Social Hub and Generic
		public static event Action<string, string> OnStartButtonPressed; // Keep for compatibility
		public static event Action<string, string> OnStartSocialHubPressed; // New - specific to social hub
		public static event Action<string, string> OnQuickMissionPressed; // New - for direct mission join
		public static event Action OnReturnToMainMenuButtonPressed;
		public static event Action OnReturnToSocialHubPressed; // New - from mission to hub
		public static event Action OnQuitGameButtonPressed;
		public static event Action<Task, string> OnConnectToSessionCompleted;
		public static event Action<Task, string> OnSocialHubConnectionCompleted; // New
		public static event Action<Task, string> OnMissionConnectionCompleted; // New
		public static event Action OnExitedSession;
		public static event Action OnExitedSocialHub; // New - specific exit events
		public static event Action OnExitedMission; // New

		// Chat Events
		public static event Action<string, string, bool> OnTextMessageReceived;
		public static event Action<string> OnSendTextMessage;
		public static event Action<bool, string> OnChatIsReady;
		public static event Action<VivoxParticipant> OnParticipantJoinedVoiceChat;
		public static event Action<VivoxParticipant> OnParticipantLeftVoiceChat;

		// Gameplay Events
		public static event Action<PickupState, Transform> OnPickupStateChanged;

		// Mission-specific Events
		public static event Action<string, int> OnCreateMissionPressed;
		public static event Action<string> OnJoinMissionPressed;
		public static event Action OnLeaveMissionPressed;
		public static event Action<string, int> OnMissionCreated;
		public static event Action<string> OnMissionJoined;
		public static event Action OnMissionStarting;
		public static event Action OnMissionComplete;
		public static event Action<string> OnMissionFailed;

		// Player Events (new)
		public static event Action<string> OnPlayerLoadoutChanged;
		public static event Action<int> OnPlayerReadyStateChanged;

		// Scene Management Events
		public static event Action<string> OnSceneLoadStarted;
		public static event Action<string> OnSceneLoadCompleted;

#region Gameplay Events

		public static void SetAvatarPickupState(PickupState state, Transform pickup)
		{
			OnPickupStateChanged?.Invoke(state, pickup);
		}

#endregion

#region Network Events

		public static void NetworkObjectDespawned(NetworkObject networkObject)
		{
			OnNetworkObjectDespawned?.Invoke(networkObject);
		}

		public static void NetworkObjectOwnershipChanged(NetworkObject networkObject, ulong previous, ulong current)
		{
			OnNetworkObjectOwnershipChanged?.Invoke(networkObject, previous, current);
		}

#endregion

#region Session Events

		public static void StartButtonPressed(string playerName, string sessionName)
		{
			OnStartButtonPressed?.Invoke(playerName, sessionName);
		}

		public static void StartSocialHubPressed(string playerName, string sessionName)
		{
			OnStartSocialHubPressed?.Invoke(playerName, sessionName);
		}

		public static void QuickMissionPressed(string playerName, string missionType)
		{
			OnQuickMissionPressed?.Invoke(playerName, missionType);
		}

		public static void ReturnToMainMenuPressed()
		{
			OnReturnToMainMenuButtonPressed?.Invoke();
		}

		public static void ReturnToSocialHubPressed()
		{
			OnReturnToSocialHubPressed?.Invoke();
		}

		public static void QuitGamePressed()
		{
			OnQuitGameButtonPressed?.Invoke();
		}

		public static void ConnectToSessionComplete(Task task, string sessionName)
		{
			OnConnectToSessionCompleted?.Invoke(task, sessionName);
		}

		public static void SocialHubConnectionComplete(Task task, string sessionName)
		{
			OnSocialHubConnectionCompleted?.Invoke(task, sessionName);
		}

		public static void MissionConnectionComplete(Task task, string sessionName)
		{
			OnMissionConnectionCompleted?.Invoke(task, sessionName);
		}

		public static void ExitedSession()
		{
			OnExitedSession?.Invoke();
		}

		public static void ExitedSocialHub()
		{
			OnExitedSocialHub?.Invoke();
		}

		public static void ExitedMission()
		{
			OnExitedMission?.Invoke();
		}

#endregion

#region Scene Loading

		public static void LoadMainMenuScene()
		{
			SceneManager.LoadScene("MainMenu");
		}

		public static void LoadInGameScene()
		{
			SceneManager.LoadScene("HubScene_TownMarket");
		}

		public static void LoadMissionScene(string missionSceneName)
		{
			SceneManager.LoadScene(missionSceneName);
		}

#endregion

#region Chat Events

		public static void ProcessTextMessageReceived(string senderName, string message, bool fromSelf)
		{
			OnTextMessageReceived?.Invoke(senderName, message, fromSelf);
		}

		public static void SendTextMessage(string message)
		{
			OnSendTextMessage?.Invoke(message);
		}

		public static void SetTextChatReady(bool enabled, string channelName)
		{
			OnChatIsReady?.Invoke(enabled, channelName);
		}

		public static void ParticipantJoinedVoiceChat(VivoxParticipant vivoxParticipant)
		{
			OnParticipantJoinedVoiceChat?.Invoke(vivoxParticipant);
		}

		public static void ParticipantLeftVoiceChat(VivoxParticipant vivoxParticipant)
		{
			OnParticipantLeftVoiceChat?.Invoke(vivoxParticipant);
		}

#endregion

#region Mission Events

		public static void CreateMissionPressed(string missionName, int maxPlayers)
		{
			OnCreateMissionPressed?.Invoke(missionName, maxPlayers);
		}

		public static void JoinMissionPressed(string sessionCode)
		{
			OnJoinMissionPressed?.Invoke(sessionCode);
		}

		public static void LeaveMissionPressed()
		{
			OnLeaveMissionPressed?.Invoke();
		}

		public static void MissionCreated(string missionId, int maxPlayers)
		{
			OnMissionCreated?.Invoke(missionId, maxPlayers);
		}

		public static void MissionJoined(string missionId)
		{
			OnMissionJoined?.Invoke(missionId);
		}

		public static void MissionStarting()
		{
			OnMissionStarting?.Invoke();
		}

		public static void MissionComplete()
		{
			OnMissionComplete?.Invoke();
		}

		public static void MissionFailed(string reason)
		{
			OnMissionFailed?.Invoke(reason);
		}

#endregion

#region Player Events

		public static void PlayerLoadoutChanged(string loadoutData)
		{
			OnPlayerLoadoutChanged?.Invoke(loadoutData);
		}

		public static void PlayerReadyStateChanged(int readyCount)
		{
			OnPlayerReadyStateChanged?.Invoke(readyCount);
		}

#endregion

#region Scene Loading

		public static void SceneLoadStarted(string sceneName)
		{
			OnSceneLoadStarted?.Invoke(sceneName);
		}

		public static void SceneLoadCompleted(string sceneName)
		{
			OnSceneLoadCompleted?.Invoke(sceneName);
		}

#endregion
	}

	public enum PickupState
	{
		Inactive,
		PickupInRange,
		Carry
	}
}
