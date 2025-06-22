using UnityEngine;

namespace Kobold.Utils
{
    /// <summary>
    /// Singleton class which saves/loads local-client settings.
    /// (This is just a wrapper around the PlayerPrefs system,
    /// so that all the calls are in the same place.)
    /// </summary>
    public static class KoboldPrefs
    {
		private const string KMasterVolumeKey = "MasterVolume";
		private const string KMusicVolumeKey = "MusicVolume";
		private const string KSfxVolumeKey = "SfxVolume";
		private const string KFootstepsVolumeKey = "FootstepsVolume";
        private const string KClientGuidKey = "client_guid";

		private const float KDefaultMasterVolume = 0.5f;
		private const float KDefaultMusicVolume = 0.8f;
		private const float KDefaultSfxVolume = 0.8f;
		private const float KDefaultFootstepsVolume = 0.8f;

        public static float GetMasterVolume()
        {
            return PlayerPrefs.GetFloat(KMasterVolumeKey, KDefaultMasterVolume);
        }

        public static void SetMasterVolume(float volume)
        {
            PlayerPrefs.SetFloat(KMasterVolumeKey, volume);
        }

        public static float GetMusicVolume()
        {
            return PlayerPrefs.GetFloat(KMusicVolumeKey, KDefaultMusicVolume);
        }

        public static void SetMusicVolume(float volume)
        {
            PlayerPrefs.SetFloat(KMusicVolumeKey, volume);
        }
		
		public static float GetSfxVolume()
		{
			return PlayerPrefs.GetFloat(KSfxVolumeKey, KDefaultSfxVolume);
		}

		public static void SetSfxVolume(float volume)
		{
			PlayerPrefs.SetFloat(KSfxVolumeKey, volume);
		}

		public static float GetFootstepsVolume()
		{
			return PlayerPrefs.GetFloat(KFootstepsVolumeKey, KDefaultFootstepsVolume);
		}

		public static void SetFootstepsVolume(float volume)
		{
			PlayerPrefs.SetFloat(KFootstepsVolumeKey, volume);
		}
		
        /// <summary>
        /// Either loads a Guid string from Unity preferences, or creates one and checkpoints it, then returns it.
        /// </summary>
        /// <returns>The Guid that uniquely identifies this client install, in string form. </returns>
        public static string GetGuid()
        {
            if (PlayerPrefs.HasKey(KClientGuidKey))
            {
                return PlayerPrefs.GetString(KClientGuidKey);
            }

            var guid = System.Guid.NewGuid();
            var guidString = guid.ToString();

            PlayerPrefs.SetString(KClientGuidKey, guidString);
            return guidString;
        }
		
		public static void SetMuted(bool muted) => PlayerPrefs.SetInt("Muted", muted ? 1 : 0);
		public static bool IsMuted() => PlayerPrefs.GetInt("Muted", 0) == 1;
    }
}