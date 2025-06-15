using System;
using TMPro;
using Unity.BossRoom.Utils;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    public class ProfileListItemUI : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI m_ProfileNameText;

        [Inject] ProfileManager _mProfileManager;

        public void SetProfileName(string profileName)
        {
            m_ProfileNameText.text = profileName;
        }

        public void OnSelectClick()
        {
            _mProfileManager.Profile = m_ProfileNameText.text;
        }

        public void OnDeleteClick()
        {
            _mProfileManager.DeleteProfile(m_ProfileNameText.text);
        }
    }
}
