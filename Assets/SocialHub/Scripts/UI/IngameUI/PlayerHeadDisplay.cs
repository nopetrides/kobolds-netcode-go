using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    class PlayerHeadDisplay : VisualElement
    {
        VivoxParticipant _mParticipant;
        IVisualElementScheduledItem _mScheduler;
        VisualElement _mMicIcon;
        Label _mPlayerNameLabel;

        internal VivoxParticipant VivoxParticipant => _mParticipant;
        internal string PlayerId { get; set; }

        const string KPlayerMutedUSSClass = "player-mic-icon--muted";
        const string KPlayerMicIconHidden = "player-mic-icon--disable";

        /// <summary>
        /// Display that is shown above a players head
        /// </summary>
        /// <param name="asset">Uxml to be used</param>
        internal PlayerHeadDisplay(VisualTreeAsset asset)
        {
            AddToClassList("player-top-ui");
            Add(asset.CloneTree());
            _mPlayerNameLabel = this.Q<Label>();
            _mMicIcon = this.Q<VisualElement>("mic-icon");
            ShowMicIcon(false);
        }

        internal void AttachVivoxParticipant(VivoxParticipant participant)
        {
            _mParticipant = participant;

            _mParticipant.ParticipantMuteStateChanged -= OnParticipantMuteStateChanged;
            _mParticipant.ParticipantMuteStateChanged += OnParticipantMuteStateChanged;

            _mParticipant.ParticipantSpeechDetected -= OnParticipantSpeechDetected;
            _mParticipant.ParticipantSpeechDetected += OnParticipantSpeechDetected;
        }

        internal void RemoveVivoxParticipant()
        {
            if(_mParticipant == null)
                return;

            _mParticipant.ParticipantMuteStateChanged -= OnParticipantMuteStateChanged;
            _mParticipant.ParticipantSpeechDetected -= OnParticipantSpeechDetected;
        }

        void OnParticipantSpeechDetected()
        {
            if(_mParticipant.IsMuted)
                return;

            ShowMicIcon(_mParticipant.SpeechDetected);
        }

        void OnParticipantMuteStateChanged()
        {
            if (_mParticipant.IsMuted)
            {
                _mMicIcon.AddToClassList(KPlayerMutedUSSClass);
                ShowMicIcon(true);
                return;
            }
            _mMicIcon.RemoveFromClassList(KPlayerMutedUSSClass);

        }

        internal void SetPlayerName(string playerName)
        {
            _mPlayerNameLabel.text = playerName;
        }

        void ShowMicIcon(bool show)
        {
            if (show)
                _mMicIcon.RemoveFromClassList(KPlayerMicIconHidden);
            else
                _mMicIcon.AddToClassList(KPlayerMicIconHidden);
        }
    }
}
