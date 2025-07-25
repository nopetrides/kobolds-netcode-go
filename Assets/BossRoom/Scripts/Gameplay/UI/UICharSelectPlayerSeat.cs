using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Gameplay.GameState;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// Controls one of the eight "seats" on the character-select screen (the boxes along the bottom).
    /// </summary>
    public class UICharSelectPlayerSeat : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_InactiveStateVisuals;
        [SerializeField]
        private GameObject m_ActiveStateVisuals;
        [SerializeField]
        private Image m_PlayerNumberHolder;
        [SerializeField]
        private TextMeshProUGUI m_PlayerNameHolder;
        [SerializeField]
        private Image m_Glow;
        [SerializeField]
        private Image m_Checkbox;
        [SerializeField]
        private Button m_Button;
        [SerializeField]
        private Animator m_Animator;
        [SerializeField]
        private string m_AnimatorTriggerWhenLockedIn = "LockedIn";
        [SerializeField]
        private string m_AnimatorTriggerWhenUnlocked = "Unlocked";

        [SerializeField]
        private CharacterTypeEnum m_CharacterClass;

        // just a way to designate which seat we are -- the leftmost seat on the lobby UI is index 0, the next one is index 1, etc.
        private int _mSeatIndex;

        // playerNumber of who is sitting in this seat right now. 0-based; e.g. this is 0 for Player 1, 1 for Player 2, etc. Meaningless when m_State is Inactive (and in that case it is set to -1 for clarity)
        private int _mPlayerNumber;

        // the last SeatState we were assigned
        private NetworkCharSelection.SeatState _mState;

        // once this is true, we're never clickable again!
        private bool _mIsDisabled;

        public void Initialize(int seatIndex)
        {
            _mSeatIndex = seatIndex;
            _mState = NetworkCharSelection.SeatState.Inactive;
            _mPlayerNumber = -1;
            ConfigureStateGraphics();
        }

        public void SetState(NetworkCharSelection.SeatState state, int playerIndex, string playerName)
        {
            if (state == _mState && playerIndex == _mPlayerNumber)
                return; // no actual changes

            _mState = state;
            _mPlayerNumber = playerIndex;
            m_PlayerNameHolder.text = playerName;
            if (_mState == NetworkCharSelection.SeatState.Inactive)
                _mPlayerNumber = -1;
            ConfigureStateGraphics();
        }

        public bool IsLocked()
        {
            return _mState == NetworkCharSelection.SeatState.LockedIn;
        }

        public void SetDisableInteraction(bool disable)
        {
            m_Button.interactable = !disable;
            _mIsDisabled = disable;

            if (!disable)
            {
                // if we were locked move to unlocked state
                PlayUnlockAnim();
            }
        }

        private void PlayLockAnim()
        {
            if (m_Animator)
            {
                m_Animator.ResetTrigger(m_AnimatorTriggerWhenUnlocked);
                m_Animator.SetTrigger(m_AnimatorTriggerWhenLockedIn);
            }
        }

        private void PlayUnlockAnim()
        {
            if (m_Animator)
            {
                m_Animator.ResetTrigger(m_AnimatorTriggerWhenLockedIn);
                m_Animator.SetTrigger(m_AnimatorTriggerWhenUnlocked);
            }
        }

        private void ConfigureStateGraphics()
        {
            if (_mState == NetworkCharSelection.SeatState.Inactive)
            {
                m_InactiveStateVisuals.SetActive(true);
                m_ActiveStateVisuals.SetActive(false);
                m_Glow.gameObject.SetActive(false);
                m_Checkbox.gameObject.SetActive(false);
                m_PlayerNameHolder.gameObject.SetActive(false);
                m_Button.interactable = _mIsDisabled ? false : true;
                PlayUnlockAnim();
            }
            else // either active or locked-in... these states are visually very similar
            {
                m_InactiveStateVisuals.SetActive(false);
                m_PlayerNumberHolder.sprite = ClientCharSelectState.Instance.m_IdentifiersForEachPlayerNumber[_mPlayerNumber].Indicator;
                m_ActiveStateVisuals.SetActive(true);

                m_PlayerNameHolder.gameObject.SetActive(true);
                m_PlayerNameHolder.color = ClientCharSelectState.Instance.m_IdentifiersForEachPlayerNumber[_mPlayerNumber].Color;
                m_Button.interactable = _mIsDisabled ? false : true;

                if (_mState == NetworkCharSelection.SeatState.LockedIn)
                {
                    m_Glow.color = ClientCharSelectState.Instance.m_IdentifiersForEachPlayerNumber[_mPlayerNumber].Color;
                    m_Glow.gameObject.SetActive(true);
                    m_Checkbox.gameObject.SetActive(true);
                    m_Button.interactable = false;
                    PlayLockAnim();
                }
                else
                {
                    m_Glow.gameObject.SetActive(false);
                    m_Checkbox.gameObject.SetActive(false);
                    PlayUnlockAnim();
                }
            }
        }

        // Called directly by Button in UI
        public void OnClicked()
        {
            ClientCharSelectState.Instance.OnPlayerClickedSeat(_mSeatIndex);
        }

    }
}
