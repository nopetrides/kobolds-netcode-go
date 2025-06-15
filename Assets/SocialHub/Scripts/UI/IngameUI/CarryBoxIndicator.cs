using System;
using System.Collections;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// Panel that shows interaction options when character carries something.
    /// </summary>
    class CarryBoxIndicator : MonoBehaviour
    {
        [SerializeField]
        VisualTreeAsset m_CarryBoxIndicatorAsset;

        [SerializeField]
        Camera m_Camera;

        [SerializeField]
        float m_VerticalOffset = 1.5f;

        [SerializeField]
        UIDocument m_ScreenspaceUI;

        [SerializeField]
        float m_PanelMaxSize = 1.5f;

        [SerializeField]
        float m_PanelMinSize = 0.7f;

        VisualElement _mCarryUI;

        Transform _mCarryTransform;

        bool _mIsShown;

        void OnEnable()
        {
            // Pick first child to avoid adding the root element
            _mCarryUI = m_CarryBoxIndicatorAsset.CloneTree().GetFirstChild();
            m_ScreenspaceUI.rootVisualElement.Q<VisualElement>("player-carry-container").Add(_mCarryUI);
            _mCarryUI.Q<Label>("call-to-action").text = "tap - drop \nhold - throw";
            _mCarryUI.AddToClassList("carrybox");
            _mCarryUI.AddToClassList(UIUtils.SInactiveUSSClass);

            GameplayEventHandler.OnPickupStateChanged += OnPickupStateChanged;
        }

        void OnPickupStateChanged(PickupState state, Transform pickupTransform)
        {
            if (state == PickupState.Carry)
            {
                ShowCarry(pickupTransform);
                return;
            }
            HideCarry();
        }

        void ShowCarry(Transform t)
        {
            if (_mIsShown)
                return;

            _mCarryTransform = t;
            _mCarryUI.RemoveFromClassList(UIUtils.SInactiveUSSClass);
            _mCarryUI.AddToClassList(UIUtils.SActiveUSSClass);
            StopCoroutine(HideAfterDelay(5f));
            StartCoroutine(HideAfterDelay(5f));
            _mIsShown = true;
        }

        void HideCarry()
        {
            if(_mIsShown == false)
                return;

            StopCoroutine(HideAfterDelay(5f));
            _mCarryUI.RemoveFromClassList(UIUtils.SActiveUSSClass);
            _mCarryUI.AddToClassList(UIUtils.SInactiveUSSClass);
            _mIsShown = false;
        }

        IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideCarry();
        }

        void Update()
        {
            if (_mCarryTransform == null)
                return;

            _mCarryUI.TranslateVeWorldToScreenspace(m_Camera, _mCarryTransform, m_VerticalOffset);
            var distance = Vector3.Distance(m_Camera.transform.position, _mCarryTransform.position);
            var mappedScale = Mathf.Lerp(m_PanelMaxSize, m_PanelMinSize, Mathf.InverseLerp(5, 20, distance));
            _mCarryUI.style.scale = new StyleScale(new Vector2(mappedScale, mappedScale));
        }

        void OnDisable()
        {
            GameplayEventHandler.OnPickupStateChanged -= OnPickupStateChanged;
        }
    }
}
