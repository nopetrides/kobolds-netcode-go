using System;
using Unity.Multiplayer.Tools.NetStatsMonitor;
using UnityEngine;
using Unity.Multiplayer.Samples.SocialHub.Input;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI.Tools
{
    /// <summary>
    /// Adds a label to the RuntimeNetStatsMonitor UI to show how to toggle visibility.
    /// In future this functionality could be moved to the RuntimeNetStatsMonitor itself.
    /// </summary>
    [RequireComponent(typeof(RuntimeNetStatsMonitor))]
    class RuntimeStatsMonitorController : MonoBehaviour
    {
        RuntimeNetStatsMonitor _mRuntimeNetStatsMonitor;

        const string KVisibilityLabelName = "toggle-visibility-label";

        void Start()
        {
            _mRuntimeNetStatsMonitor = GetComponent<RuntimeNetStatsMonitor>();
            var uiDocuments = FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (_mRuntimeNetStatsMonitor.PanelSettingsOverride == null)
            {
                Debug.LogWarning("Assign PanelSettingsOverride to this MonoBehaviour!", this);
                return;
            }

            foreach (var uiDoc in uiDocuments)
            {
                if (uiDoc.panelSettings == _mRuntimeNetStatsMonitor.PanelSettingsOverride)
                {
                    var rsnm = uiDoc.runtimePanel.visualTree.Q<VisualElement>(className: "rnsm-monitor");
                    if (rsnm == null)
                    {
                        Debug.LogWarning("Could not find RuntimeNetworkStatsMonitor VisualElement, cannot attach UI.", this);
                        return;
                    }

                    if (rsnm.Q<VisualElement>(KVisibilityLabelName) != null)
                    {
                        // Label already exists, do not add another
                        break;
                    }

                    var inputText = InputSystemManager.IsMobile.Result ? "4-Finger Tap" : "M";
                    var label = new Label($"Toggle visibility with {inputText}")
                    {
                        name = KVisibilityLabelName,
                        style =
                        {
                            backgroundColor = new StyleColor(Color.black),
                            unityTextAlign = TextAnchor.MiddleCenter
                        }
                    };
                    rsnm.Add(label);
                }
            }

            GameInput.Actions.Player.ToggleNetworkStats.performed += OnToggleVisibility;
        }

        void OnDestroy()
        {
            GameInput.Actions.Player.ToggleNetworkStats.performed -= OnToggleVisibility;
        }

        void OnToggleVisibility(InputAction.CallbackContext obj)
        {
            _mRuntimeNetStatsMonitor.Visible = !_mRuntimeNetStatsMonitor.Visible;
        }
    }
}
