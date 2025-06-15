using System;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// UI controller which displays nameplate and mic icon on top of each connected player.
    /// </summary>
    class PlayersTopUIController : MonoBehaviour
    {
        [SerializeField]
        UIDocument m_UIDocument;

        [SerializeField]
        VisualTreeAsset m_NameplateAsset;

        [SerializeField]
        float m_PanelMinSize = 0.8f;

        [SerializeField]
        float m_PanelMaxSize = 1.1f;

        [SerializeField]
        float m_DisplayYOffset = 1.3f;

        [SerializeField]
        Camera m_Camera;

        List<PlayerHeadDisplay> _mPlayerHeadDisplayPool = new();

        Dictionary<GameObject, PlayerHeadDisplay> _mPlayerToPlayerDisplayDict = new();

        VisualElement _mRoot;

        const int KPoolSize = 12;

        void OnEnable()
        {
            _mPlayerHeadDisplayPool = new List<PlayerHeadDisplay>();
            for (var i = 0; i < KPoolSize; i++)
            {
                _mPlayerHeadDisplayPool.Add(new PlayerHeadDisplay(m_NameplateAsset));
            }

            _mRoot = m_UIDocument.rootVisualElement.Q<VisualElement>("player-top-display-container");

            GameplayEventHandler.OnParticipantJoinedVoiceChat -= AttachVivoxParticipant;
            GameplayEventHandler.OnParticipantJoinedVoiceChat += AttachVivoxParticipant;

            GameplayEventHandler.OnParticipantLeftVoiceChat -= RemoveVivoxParticipant;
            GameplayEventHandler.OnParticipantLeftVoiceChat += RemoveVivoxParticipant;
        }

        void Update()
        {
            foreach (var playerPair in _mPlayerToPlayerDisplayDict)
            {
                UpdateDisplayPosition(playerPair.Key.transform, playerPair.Value);
            }
        }

        internal void AddOrUpdatePlayer(GameObject player, string playerName, string playerId)
        {
            // if player has already been added, update values and return
            if (_mPlayerToPlayerDisplayDict.TryGetValue(player, out var playerHeadDisplay))
            {
                playerHeadDisplay.SetPlayerName(playerName);
                playerHeadDisplay.PlayerId = playerId;
                return;
            }

            var display = GetDisplayForPlayer();
            display.SetPlayerName(playerName);
            display.PlayerId = playerId;

            UpdateDisplayPosition(player.transform, display);
            _mPlayerToPlayerDisplayDict.Add(player, display);
        }

        internal void RemovePlayer(GameObject player)
        {
            var display = _mPlayerToPlayerDisplayDict[player];
            display.RemoveFromHierarchy();
            _mPlayerHeadDisplayPool.Add(display);
            _mPlayerToPlayerDisplayDict.Remove(player);
        }

        PlayerHeadDisplay GetDisplayForPlayer()
        {
            if (_mPlayerHeadDisplayPool.Count > 0)
            {
                var display = _mPlayerHeadDisplayPool[0];
                _mPlayerHeadDisplayPool.RemoveAt(0);
                _mRoot.Add(display);
                return display;
            }

            var newDisplay = new PlayerHeadDisplay(m_NameplateAsset);
            _mRoot.Add(newDisplay);
            return newDisplay;
        }

        void UpdateDisplayPosition(Transform playerTransform, VisualElement headDisplay)
        {
            headDisplay.TranslateVeWorldToScreenspace(m_Camera, playerTransform, m_DisplayYOffset);
            var distance = Vector3.Distance(m_Camera.transform.position, playerTransform.position);
            var mappedScale = Mathf.Lerp(m_PanelMaxSize, m_PanelMinSize, Mathf.InverseLerp(5, 20, distance));
            headDisplay.style.scale = new StyleScale(new Vector2(mappedScale, mappedScale));
        }

        void OnDisable()
        {
            foreach (var display in _mPlayerToPlayerDisplayDict.Values)
            {
                display.RemoveFromHierarchy();
                display.RemoveVivoxParticipant();
            }
            _mPlayerHeadDisplayPool.Clear();
            _mPlayerToPlayerDisplayDict.Clear();

            GameplayEventHandler.OnParticipantJoinedVoiceChat -= AttachVivoxParticipant;
            GameplayEventHandler.OnParticipantLeftVoiceChat -= RemoveVivoxParticipant;
        }

        void AttachVivoxParticipant(VivoxParticipant vivoxParticipant)
        {
            foreach (var headDisplay in _mPlayerToPlayerDisplayDict.Values)
            {
                if(headDisplay.PlayerId == vivoxParticipant.PlayerId)
                {
                    headDisplay.AttachVivoxParticipant(vivoxParticipant);
                    return;
                }
            }

            Debug.LogWarning("Could not find player avatar to attach vivox user.");
        }

        void RemoveVivoxParticipant(VivoxParticipant vivoxParticipant)
        {
            foreach (var headDisplay in _mPlayerToPlayerDisplayDict.Values)
            {
                if(headDisplay.VivoxParticipant != null && headDisplay.VivoxParticipant== vivoxParticipant)
                {
                    headDisplay.RemoveVivoxParticipant();
                    return;
                }
            }

            Debug.LogWarning("Could not find player avatar display to remove vivox participant");
        }
    }
}
