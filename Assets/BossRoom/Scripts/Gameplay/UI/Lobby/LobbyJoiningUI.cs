using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.BossRoom.Infrastructure;
using Unity.BossRoom.UnityServices.Lobbies;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// Handles the list of LobbyListItemUIs and ensures it stays synchronized with the lobby list from the service.
    /// </summary>
    public class LobbyJoiningUI : MonoBehaviour
    {
        [SerializeField]
        LobbyListItemUI m_LobbyListItemPrototype;
        [SerializeField]
        InputField m_JoinCodeField;
        [SerializeField]
        CanvasGroup m_CanvasGroup;
        [SerializeField]
        Graphic m_EmptyLobbyListLabel;
        [SerializeField]
        Button m_JoinLobbyButton;

        IObjectResolver _mContainer;
        LobbyUIMediator _mLobbyUIMediator;
        UpdateRunner _mUpdateRunner;
        ISubscriber<LobbyListFetchedMessage> _mLocalLobbiesRefreshedSub;

        List<LobbyListItemUI> _mLobbyListItems = new List<LobbyListItemUI>();

        void Awake()
        {
            m_LobbyListItemPrototype.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            if (_mUpdateRunner != null)
            {
                _mUpdateRunner.Unsubscribe(PeriodicRefresh);
            }
        }

        void OnDestroy()
        {
            if (_mLocalLobbiesRefreshedSub != null)
            {
                _mLocalLobbiesRefreshedSub.Unsubscribe(UpdateUI);
            }
        }

        [Inject]
        void InjectDependenciesAndInitialize(
            IObjectResolver container,
            LobbyUIMediator lobbyUIMediator,
            UpdateRunner updateRunner,
            ISubscriber<LobbyListFetchedMessage> localLobbiesRefreshedSub)
        {
            _mContainer = container;
            _mLobbyUIMediator = lobbyUIMediator;
            _mUpdateRunner = updateRunner;
            _mLocalLobbiesRefreshedSub = localLobbiesRefreshedSub;
            _mLocalLobbiesRefreshedSub.Subscribe(UpdateUI);
        }

        /// <summary>
        /// Added to the InputField component's OnValueChanged callback for the join code text.
        /// </summary>
        public void OnJoinCodeInputTextChanged()
        {
            m_JoinCodeField.text = SanitizeJoinCode(m_JoinCodeField.text);
            m_JoinLobbyButton.interactable = m_JoinCodeField.text.Length > 0;
        }

        string SanitizeJoinCode(string dirtyString)
        {
            return Regex.Replace(dirtyString.ToUpper(), "[^A-Z0-9]", "");
        }

        public void OnJoinButtonPressed()
        {
            _mLobbyUIMediator.JoinLobbyWithCodeRequest(SanitizeJoinCode(m_JoinCodeField.text));
        }

        void PeriodicRefresh(float _)
        {
            //this is a soft refresh without needing to lock the UI and such
            _mLobbyUIMediator.QueryLobbiesRequest(false);
        }

        public void OnRefresh()
        {
            _mLobbyUIMediator.QueryLobbiesRequest(true);
        }

        void UpdateUI(LobbyListFetchedMessage message)
        {
            EnsureNumberOfActiveUISlots(message.LocalLobbies.Count);

            for (var i = 0; i < message.LocalLobbies.Count; i++)
            {
                var localLobby = message.LocalLobbies[i];
                _mLobbyListItems[i].SetData(localLobby);
            }

            if (message.LocalLobbies.Count == 0)
            {
                m_EmptyLobbyListLabel.enabled = true;
            }
            else
            {
                m_EmptyLobbyListLabel.enabled = false;
            }
        }

        void EnsureNumberOfActiveUISlots(int requiredNumber)
        {
            int delta = requiredNumber - _mLobbyListItems.Count;

            for (int i = 0; i < delta; i++)
            {
                _mLobbyListItems.Add(CreateLobbyListItem());
            }

            for (int i = 0; i < _mLobbyListItems.Count; i++)
            {
                _mLobbyListItems[i].gameObject.SetActive(i < requiredNumber);
            }
        }

        LobbyListItemUI CreateLobbyListItem()
        {
            var listItem = Instantiate(m_LobbyListItemPrototype.gameObject, m_LobbyListItemPrototype.transform.parent)
                .GetComponent<LobbyListItemUI>();
            listItem.gameObject.SetActive(true);

            _mContainer.Inject(listItem);

            return listItem;
        }

        public void OnQuickJoinClicked()
        {
            _mLobbyUIMediator.QuickJoinRequest();
        }

        public void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;
            m_JoinCodeField.text = "";
            _mUpdateRunner.Subscribe(PeriodicRefresh, 10f);
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.blocksRaycasts = false;
            _mUpdateRunner.Unsubscribe(PeriodicRefresh);
        }
    }
}
