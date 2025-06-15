using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.BossRoom.Utils;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using TMPro;

namespace Unity.BossRoom.Gameplay.UI
{
    public class UIProfileSelector : MonoBehaviour
    {
        [SerializeField]
        ProfileListItemUI m_ProfileListItemPrototype;
        [SerializeField]
        TMP_InputField m_NewProfileField;
        [SerializeField]
        Button m_CreateProfileButton;
        [SerializeField]
        CanvasGroup m_CanvasGroup;
        [SerializeField]
        Graphic m_EmptyProfileListLabel;

        List<ProfileListItemUI> _mProfileListItems = new List<ProfileListItemUI>();

        [Inject] IObjectResolver _mResolver;
        [Inject] ProfileManager _mProfileManager;

        // Authentication service only accepts profile names of 30 characters or under 
        const int KAuthenticationMaxProfileLength = 30;

        void Awake()
        {
            m_ProfileListItemPrototype.gameObject.SetActive(false);
            Hide();
            m_CreateProfileButton.interactable = false;
        }

        /// <summary>
        /// Added to the InputField component's OnValueChanged callback for the join code text.
        /// </summary>
        public void SanitizeProfileNameInputText()
        {
            m_NewProfileField.text = SanitizeProfileName(m_NewProfileField.text);
            m_CreateProfileButton.interactable = m_NewProfileField.text.Length > 0 && !_mProfileManager.AvailableProfiles.Contains(m_NewProfileField.text);
        }

        string SanitizeProfileName(string dirtyString)
        {
            var output = Regex.Replace(dirtyString, "[^a-zA-Z0-9]", "");
            return output[..Math.Min(output.Length, KAuthenticationMaxProfileLength)];
        }

        public void OnNewProfileButtonPressed()
        {
            var profile = m_NewProfileField.text;
            if (!_mProfileManager.AvailableProfiles.Contains(profile))
            {
                _mProfileManager.CreateProfile(profile);
                _mProfileManager.Profile = profile;
            }
            else
            {
                PopupManager.ShowPopupPanel("Could not create new Profile", "A profile already exists with this same name. Select one of the already existing profiles or create a new one.");
            }
        }

        public void InitializeUI()
        {
            EnsureNumberOfActiveUISlots(_mProfileManager.AvailableProfiles.Count);
            for (var i = 0; i < _mProfileManager.AvailableProfiles.Count; i++)
            {
                var profileName = _mProfileManager.AvailableProfiles[i];
                _mProfileListItems[i].SetProfileName(profileName);
            }

            m_EmptyProfileListLabel.enabled = _mProfileManager.AvailableProfiles.Count == 0;
        }

        void EnsureNumberOfActiveUISlots(int requiredNumber)
        {
            int delta = requiredNumber - _mProfileListItems.Count;

            for (int i = 0; i < delta; i++)
            {
                CreateProfileListItem();
            }

            for (int i = 0; i < _mProfileListItems.Count; i++)
            {
                _mProfileListItems[i].gameObject.SetActive(i < requiredNumber);
            }
        }

        void CreateProfileListItem()
        {
            var listItem = Instantiate(m_ProfileListItemPrototype.gameObject, m_ProfileListItemPrototype.transform.parent)
                .GetComponent<ProfileListItemUI>();
            _mProfileListItems.Add(listItem);
            listItem.gameObject.SetActive(true);
            _mResolver.Inject(listItem);
        }

        public void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;
            m_NewProfileField.text = "";
            InitializeUI();
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.blocksRaycasts = false;
        }
    }
}
