using System;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// Indicator shown when a pickup is in range.
    /// </summary>
    class PickUpIndicator : MonoBehaviour
    {
        [SerializeField]
        VisualTreeAsset m_PickupAsset;

        [SerializeField]
        Camera m_Camera;

        [SerializeField]
        float m_VerticalOffset = 1.5f;

        [SerializeField]
        UIDocument m_WorldspaceUI;

        VisualElement _mPickupUI;

        Transform _mCurrentPickup;

        Transform _mNextPickup;

        bool _mIsShown = false;

        bool IsShown
        {
            set
            {
                // if the value is the same, do nothing
                if (_mIsShown == value)
                    return;

                _mIsShown = value;

                // fade in the pickup UI
                if (_mIsShown)
                {
                    _mPickupUI.RemoveFromClassList(UIUtils.SInactiveUSSClass);
                    _mPickupUI.AddToClassList(UIUtils.SActiveUSSClass);
                    return;
                }

                // fade out the pickup UI
                _mPickupUI.RemoveFromClassList(UIUtils.SActiveUSSClass);
                _mPickupUI.AddToClassList(UIUtils.SInactiveUSSClass);
            }
        }

        void ShowPickup(Transform t)
        {
            _mNextPickup = t;
        }

        void ClearPickup()
        {
            _mNextPickup = null;
        }

        void OnEnable()
        {
            // pick first child to avoid adding the root element
            _mPickupUI = m_PickupAsset.CloneTree().GetFirstChild();
            _mPickupUI.AddToClassList(UIUtils.SInactiveUSSClass);
            m_WorldspaceUI.rootVisualElement.Q<VisualElement>("Pickup").Add(_mPickupUI);
            GameplayEventHandler.OnPickupStateChanged += OnPickupStateChanged;
        }

        void OnPickupStateChanged(PickupState state, Transform pickupTransform)
        {
            switch (state)
            {
                case PickupState.PickupInRange:
                    ShowPickup(pickupTransform);
                    break;
                case PickupState.Inactive or PickupState.Carry:
                    ClearPickup();
                    break;
            }
        }

        void Update()
        {
            if (_mCurrentPickup == _mNextPickup)
            {
                if (_mCurrentPickup != null)
                {
                    IsShown = true;
                    UpdatePickup();
                }

                return;
            }

            IsShown = false;
            if (_mPickupUI.resolvedStyle.opacity == 0)
            {
                _mCurrentPickup = _mNextPickup;
            }
        }

        void UpdatePickup()
        {
            UIUtils.TransformUIDocumentWorldspace(m_WorldspaceUI, m_Camera, _mCurrentPickup, m_VerticalOffset);
        }

        void OnDisable()
        {
            GameplayEventHandler.OnPickupStateChanged -= OnPickupStateChanged;
        }
    }
}
