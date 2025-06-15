using System;
using System.Collections;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Infrastructure;
using Unity.BossRoom.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// Class designed to only run on a client. Add this to a world-space prefab to display health or name on UI.
    /// </summary>
    /// <remarks>
    /// Execution order is explicitly set such that it this class executes its LateUpdate after any Cinemachine
    /// LateUpdate calls, which may alter the final position of the game camera.
    /// </remarks>
    [DefaultExecutionOrder(300)]
    public class UIStateDisplayHandler : NetworkBehaviour
    {
        [SerializeField]
        bool m_DisplayHealth;

        [SerializeField]
        bool m_DisplayName;

        [SerializeField]
        UIStateDisplay m_UIStatePrefab;

        // spawned in world (only one instance of this)
        UIStateDisplay _mUIState;

        RectTransform _mUIStateRectTransform;

        bool _mUIStateActive;

        [SerializeField]
        NetworkHealthState m_NetworkHealthState;

        [SerializeField]
        NetworkNameState m_NetworkNameState;

        ServerCharacter _mServerCharacter;

        ClientAvatarGuidHandler _mClientAvatarGuidHandler;

        NetworkAvatarGuidState _mNetworkAvatarGuidState;

        [SerializeField]
        IntVariable m_BaseHP;

        [Tooltip("UI object(s) will appear positioned at this transforms position.")]
        [SerializeField]
        Transform m_TransformToTrack;

        Camera _mCamera;

        Transform _mCanvasTransform;

        // as soon as any HP goes to 0, we wait this long before removing health bar UI object
        const float KDurationSeconds = 2f;

        [Tooltip("World space vertical offset for positioning.")]
        [SerializeField]
        float m_VerticalWorldOffset;

        [Tooltip("Screen space vertical offset for positioning.")]
        [SerializeField]
        float m_VerticalScreenOffset;

        Vector3 _mVerticalOffset;

        // used to compute world position based on target and offsets
        Vector3 _mWorldPos;

        void Awake()
        {
            _mServerCharacter = GetComponent<ServerCharacter>();
        }

        public override void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsClient)
            {
                enabled = false;
                return;
            }

            var cameraGameObject = GameObject.FindWithTag("MainCamera");
            if (cameraGameObject)
            {
                _mCamera = cameraGameObject.GetComponent<Camera>();
            }
            Assert.IsNotNull(_mCamera);

            var canvasGameObject = GameObject.FindWithTag("GameCanvas");
            if (canvasGameObject)
            {
                _mCanvasTransform = canvasGameObject.transform;
            }
            Assert.IsNotNull(_mCanvasTransform);

            Assert.IsTrue(m_DisplayHealth || m_DisplayName, "Neither display fields are toggled on!");
            if (m_DisplayHealth)
            {
                Assert.IsNotNull(m_NetworkHealthState, "A NetworkHealthState component needs to be attached!");
            }

            _mVerticalOffset = new Vector3(0f, m_VerticalScreenOffset, 0f);

            // if PC, find our graphics transform and update health through callbacks, if displayed
            if (TryGetComponent(out _mClientAvatarGuidHandler) && TryGetComponent(out _mNetworkAvatarGuidState))
            {
                m_BaseHP = _mNetworkAvatarGuidState.RegisteredAvatar.CharacterClass.BaseHP;

                if (_mServerCharacter.ClientCharacter)
                {
                    TrackGraphicsTransform(_mServerCharacter.ClientCharacter.gameObject);
                }
                else
                {
                    _mClientAvatarGuidHandler.AvatarGraphicsSpawned += TrackGraphicsTransform;
                }

                if (m_DisplayHealth)
                {
                    m_NetworkHealthState.HitPointsReplenished += DisplayUIHealth;
                    m_NetworkHealthState.HitPointsDepleted += RemoveUIHealth;
                }
            }

            if (m_DisplayName)
            {
                DisplayUIName();
            }

            if (m_DisplayHealth)
            {
                DisplayUIHealth();
            }
        }

        void OnDisable()
        {
            if (!m_DisplayHealth)
            {
                return;
            }

            if (m_NetworkHealthState != null)
            {
                m_NetworkHealthState.HitPointsReplenished -= DisplayUIHealth;
                m_NetworkHealthState.HitPointsDepleted -= RemoveUIHealth;
            }

            if (_mClientAvatarGuidHandler)
            {
                _mClientAvatarGuidHandler.AvatarGraphicsSpawned -= TrackGraphicsTransform;
            }
        }

        void DisplayUIName()
        {
            if (m_NetworkNameState == null)
            {
                return;
            }

            if (_mUIState == null)
            {
                SpawnUIState();
            }

            _mUIState.DisplayName(m_NetworkNameState.Name);
            _mUIStateActive = true;
        }

        void DisplayUIHealth()
        {
            if (m_NetworkHealthState == null)
            {
                return;
            }

            if (_mUIState == null)
            {
                SpawnUIState();
            }

            _mUIState.DisplayHealth(m_NetworkHealthState.HitPoints, m_BaseHP.Value);
            _mUIStateActive = true;
        }

        void SpawnUIState()
        {
            _mUIState = Instantiate(m_UIStatePrefab, _mCanvasTransform);
            // make in world UI state draw under other UI elements
            _mUIState.transform.SetAsFirstSibling();
            _mUIStateRectTransform = _mUIState.GetComponent<RectTransform>();
        }

        void RemoveUIHealth()
        {
            StartCoroutine(WaitToHideHealthBar());
        }

        IEnumerator WaitToHideHealthBar()
        {
            yield return new WaitForSeconds(KDurationSeconds);

            _mUIState.HideHealth();
        }

        void TrackGraphicsTransform(GameObject graphicsGameObject)
        {
            m_TransformToTrack = graphicsGameObject.transform;
        }

        /// <remarks>
        /// Moving UI objects on LateUpdate ensures that the game camera is at its final position pre-render.
        /// </remarks>
        void LateUpdate()
        {
            if (_mUIStateActive && m_TransformToTrack)
            {
                // set world position with world offset added
                _mWorldPos.Set(m_TransformToTrack.position.x,
                    m_TransformToTrack.position.y + m_VerticalWorldOffset,
                    m_TransformToTrack.position.z);

                _mUIStateRectTransform.position = _mCamera.WorldToScreenPoint(_mWorldPos) + _mVerticalOffset;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (_mUIState != null)
            {
                Destroy(_mUIState.gameObject);
            }
        }
    }
}
