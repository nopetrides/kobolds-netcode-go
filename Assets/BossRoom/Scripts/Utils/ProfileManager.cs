using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

#if UNITY_EDITOR
using System.Security.Cryptography;
using System.Text;
#endif

using UnityEngine;

namespace Unity.BossRoom.Utils
{
    public class ProfileManager
    {
        public const string AuthProfileCommandLineArg = "-AuthProfile";

        string _mProfile;

        public string Profile
        {
            get
            {
                if (_mProfile == null)
                {
                    _mProfile = GetProfile();
                }

                return _mProfile;
            }
            set
            {
                _mProfile = value;
                OnProfileChanged?.Invoke();
            }
        }

        public event Action OnProfileChanged;

        List<string> _mAvailableProfiles;

        public ReadOnlyCollection<string> AvailableProfiles
        {
            get
            {
                if (_mAvailableProfiles == null)
                {
                    LoadProfiles();
                }

                return _mAvailableProfiles.AsReadOnly();
            }
        }

        public void CreateProfile(string profile)
        {
            _mAvailableProfiles.Add(profile);
            SaveProfiles();
        }

        public void DeleteProfile(string profile)
        {
            _mAvailableProfiles.Remove(profile);
            SaveProfiles();
        }

        static string GetProfile()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (arguments[i] == AuthProfileCommandLineArg)
                {
                    var profileId = arguments[i + 1];
                    return profileId;
                }
            }

#if UNITY_EDITOR

            // When running in the Editor make a unique ID from the Application.dataPath.
            // This will work for cloning projects manually, or with Virtual Projects.
            // Since only a single instance of the Editor can be open for a specific
            // dataPath, uniqueness is ensured.
            var hashedBytes = new MD5CryptoServiceProvider()
                .ComputeHash(Encoding.UTF8.GetBytes(Application.dataPath));
            Array.Resize(ref hashedBytes, 16);
            // Authentication service only allows profile names of maximum 30 characters. We're generating a GUID based
            // on the project's path. Truncating the first 30 characters of said GUID string suffices for uniqueness.
            return new Guid(hashedBytes).ToString("N")[..30];
#else
            return "";
#endif
        }

        void LoadProfiles()
        {
            _mAvailableProfiles = new List<string>();
            var loadedProfiles = ClientPrefs.GetAvailableProfiles();
            foreach (var profile in loadedProfiles.Split(',')) // this works since we're sanitizing our input strings
            {
                if (profile.Length > 0)
                {
                    _mAvailableProfiles.Add(profile);
                }
            }
        }

        void SaveProfiles()
        {
            var profilesToSave = "";
            foreach (var profile in _mAvailableProfiles)
            {
                profilesToSave += profile + ",";
            }
            ClientPrefs.SetAvailableProfiles(profilesToSave);
        }

    }
}
