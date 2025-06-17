using System;
using Unity.BossRoom.Gameplay.UserInput;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// Responsible for managing and creating a feedback icon where the player clicked to move
    /// </summary>
    [RequireComponent(typeof(ClientInputSender))]
    public class ClientClickFeedback : NetworkBehaviour
    {
        [SerializeField]
        GameObject m_FeedbackPrefab;

        GameObject _mFeedbackObj;

        ClientInputSender _mClientSender;

        ClickFeedbackLerper _mClickFeedbackLerper;


        void Start()
        {
            if (NetworkManager.Singleton.LocalClientId != OwnerClientId)
            {
                enabled = false;
                return;
            }

            _mClientSender = GetComponent<ClientInputSender>();
            _mClientSender.ClientMoveEvent += OnClientMove;
            _mFeedbackObj = Instantiate(m_FeedbackPrefab);
            _mFeedbackObj.SetActive(false);
            _mClickFeedbackLerper = _mFeedbackObj.GetComponent<ClickFeedbackLerper>();
        }

        void OnClientMove(Vector3 position)
        {
            _mFeedbackObj.SetActive(true);
            _mClickFeedbackLerper.SetTarget(position);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (_mClientSender)
            {
                _mClientSender.ClientMoveEvent -= OnClientMove;
            }

        }
    }
}
