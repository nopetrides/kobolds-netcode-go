using System;
using UnityEngine;

namespace Unity.BossRoom.Utils
{
	/// <summary>
	///     Singleton class which saves/loads local-client settings.
	///     (This is just a wrapper around the PlayerPrefs system,
	///     so that all the calls are in the same place.)
	/// </summary>
	public static class ClientPrefs
	{
		private const string KMasterVolumeKey = "MasterVolume";
		private const string KMusicVolumeKey = "MusicVolume";
		private const string KSfxVolumeKey = "SfxVolume";
		private const string KFootstepsVolumeKey = "FootstepsVolume";
		private const string KClientGuidKey = "client_guid";
		private const string KAvailableProfilesKey = "AvailableProfiles";

		private const float KDefaultMasterVolume = 0.5f;
		private const float KDefaultMusicVolume = 0.8f;

		public static float GetMasterVolume()
		{
			return PlayerPrefs.GetFloat(KMasterVolumeKey, KDefaultMasterVolume);
		}

		public static float GetMusicVolume()
		{
			return PlayerPrefs.GetFloat(KMusicVolumeKey, KDefaultMusicVolume);
		}

		public static float GetSfxVolume()
		{
			return PlayerPrefs.GetFloat(KSfxVolumeKey, KDefaultMusicVolume);
		}

		public static float GetFootstepsVolume()
		{
			return PlayerPrefs.GetFloat(KFootstepsVolumeKey, KDefaultMusicVolume);
		}

		public static void SetMasterVolume(float volume)
		{
			PlayerPrefs.SetFloat(KMasterVolumeKey, volume);
		}

		public static void SetMusicVolume(float volume)
		{
			PlayerPrefs.SetFloat(KMusicVolumeKey, volume);
		}

		public static void SetSfxVolume(float volume)
		{
			PlayerPrefs.SetFloat(KSfxVolumeKey, volume);
		}

		public static void SetFootstepsVolume(float volume)
		{
			PlayerPrefs.SetFloat(KFootstepsVolumeKey, volume);
		}

		/// <summary>
		///     Either loads a Guid string from Unity preferences, or creates one and checkpoints it, then returns it.
		/// </summary>
		/// <returns>The Guid that uniquely identifies this client install, in string form. </returns>
		public static string GetGuid()
		{
			if (PlayerPrefs.HasKey(KClientGuidKey)) return PlayerPrefs.GetString(KClientGuidKey);

			var guid = Guid.NewGuid();
			var guidString = guid.ToString();

			PlayerPrefs.SetString(KClientGuidKey, guidString);
			return guidString;
		}

		public static string GetAvailableProfiles()
		{
			return PlayerPrefs.GetString(KAvailableProfilesKey, "");
		}

		public static void SetAvailableProfiles(string availableProfiles)
		{
			PlayerPrefs.SetString(KAvailableProfilesKey, availableProfiles);
		}
	}
}
