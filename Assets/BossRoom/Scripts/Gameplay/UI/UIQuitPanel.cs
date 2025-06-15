using System;
using Unity.BossRoom.ApplicationLifecycle.Messages;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Infrastructure;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    public class UIQuitPanel : MonoBehaviour
    {
        enum QuitMode
        {
            ReturnToMenu,
            QuitApplication
        }

        [SerializeField]
        QuitMode m_QuitMode = QuitMode.ReturnToMenu;

        [Inject]
        ConnectionManager _mConnectionManager;

        [Inject]
        IPublisher<QuitApplicationMessage> _mQuitApplicationPub;

        public void Quit()
        {
            switch (m_QuitMode)
            {
                case QuitMode.ReturnToMenu:
                    _mConnectionManager.RequestShutdown();
                    break;
                case QuitMode.QuitApplication:
                    _mQuitApplicationPub.Publish(new QuitApplicationMessage());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            gameObject.SetActive(false);
        }
    }
}
