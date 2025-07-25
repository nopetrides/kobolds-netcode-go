using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime.BuiltInScenarios;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.BossRoom.Utils
{
    public class NetworkSimulatorUIMediator : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField]
        NetworkSimulator m_NetworkSimulator;
#endif
        [SerializeField]
        CanvasGroup m_CanvasGroup;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField]
        TMP_Dropdown m_PresetsDropdown;

        [SerializeField]
        TMP_Dropdown m_ScenariosDropdown;

        [SerializeField]
        Button m_ScenariosButton;

        [SerializeField]
        TextMeshProUGUI m_ScenariosButtonText;

        [SerializeField]
        TMP_InputField m_LagSpikeDuration;

        [SerializeField]
        KeyCode m_OpenWindowKeyCode = KeyCode.Tilde;

        [SerializeField]
        List<ConnectionsCycle.Configuration> m_ConnectionsCycleConfigurations;

        [SerializeField]
        List<RandomConnectionsSwap.Configuration> m_RandomConnectionsSwapConfigurations;

        [SerializeField]
        int m_RandomConnectionsSwapChangeIntervalMilliseconds;

        const int KNbTouchesToOpenWindow = 5;

        Dictionary<string, INetworkSimulatorPreset> _mSimulatorPresets = new Dictionary<string, INetworkSimulatorPreset>();
#endif
        bool _mShown;

        const string KNone = "None";
        const string KConnectionCyclesScenarioName = "Connections Cycle";
        const string KRandomConnectionSwapScenarioName = "Random Connections Swap";
        const string KPauseString = "Pause";
        const string KResumeString = "Resume";

        void Awake()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            InitializeUI();
#endif
            // Hide UI until ready
            Hide();
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.interactable = false;
            m_CanvasGroup.blocksRaycasts = false;
            _mShown = false;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void Start()
        {
            NetworkManager.Singleton.OnClientStarted += OnNetworkManagerStarted;
            NetworkManager.Singleton.OnServerStarted += OnNetworkManagerStarted;
        }

        void OnDestroy()
        {
            if (NetworkManager.Singleton is not null)
            {
                NetworkManager.Singleton.OnClientStarted -= OnNetworkManagerStarted;
                NetworkManager.Singleton.OnServerStarted -= OnNetworkManagerStarted;
            }
        }

        void OnNetworkManagerStarted()
        {
            if (m_NetworkSimulator.IsAvailable)
            {
                Show();
            }
        }

        void OnPresetChanged(int optionIndex)
        {
            m_NetworkSimulator.ChangeConnectionPreset(_mSimulatorPresets[m_PresetsDropdown.options[optionIndex].text]);
        }

        void OnScenarioChanged(int optionIndex)
        {
            var scenarioName = m_ScenariosDropdown.options[optionIndex].text;
            NetworkScenario scenario = null;
            switch (scenarioName)
            {
                case KNone:
                    m_PresetsDropdown.captionText.color = m_PresetsDropdown.colors.normalColor;
                    m_PresetsDropdown.interactable = true;
                    break;
                case KConnectionCyclesScenarioName:
                    var connectionsCyleScenario = new ConnectionsCycle();
                    connectionsCyleScenario.Configurations.Clear();
                    foreach (var configuration in m_ConnectionsCycleConfigurations)
                    {
                        connectionsCyleScenario.Configurations.Add(configuration);
                    }
                    m_PresetsDropdown.captionText.color = m_PresetsDropdown.colors.disabledColor;
                    m_PresetsDropdown.interactable = false;
                    scenario = connectionsCyleScenario;
                    break;
                case KRandomConnectionSwapScenarioName:
                    var randomConnectionsSwapScenario = new RandomConnectionsSwap();
                    randomConnectionsSwapScenario.Configurations.Clear();
                    foreach (var configuration in m_RandomConnectionsSwapConfigurations)
                    {
                        randomConnectionsSwapScenario.Configurations.Add(configuration);
                    }
                    m_PresetsDropdown.captionText.color = m_PresetsDropdown.colors.disabledColor;
                    m_PresetsDropdown.interactable = false;
                    scenario = randomConnectionsSwapScenario;
                    break;
                default:
                    Debug.LogError("Invalid Scenario selected.");
                    m_PresetsDropdown.captionText.color = m_PresetsDropdown.colors.normalColor;
                    m_PresetsDropdown.interactable = true;
                    break;
            }
            m_NetworkSimulator.Scenario = scenario;
            if (m_NetworkSimulator.Scenario != null)
            {
                m_NetworkSimulator.Scenario.Start(m_NetworkSimulator);
            }

            UpdateScenarioButton();
        }

        void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.interactable = true;
            m_CanvasGroup.blocksRaycasts = true;
            UpdateScenarioButton();
            _mShown = true;
        }

        void ToggleVisibility()
        {
            if (_mShown)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        void InitializeUI()
        {
            // Initialize connection presets dropdown
            var optionData = new List<TMP_Dropdown.OptionData>();
            // Adding all available presets
            foreach (var networkSimulatorPreset in NetworkSimulatorPresets.Values)
            {
                _mSimulatorPresets[networkSimulatorPreset.Name] = networkSimulatorPreset;
                optionData.Add(new TMP_Dropdown.OptionData(networkSimulatorPreset.Name));
            }
            m_PresetsDropdown.AddOptions(optionData);
            m_PresetsDropdown.onValueChanged.AddListener(OnPresetChanged);

            // Initialize scenario dropdown
            optionData = new List<TMP_Dropdown.OptionData>();

            // Adding empty scenario
            optionData.Add(new TMP_Dropdown.OptionData(KNone));

            // Adding ConnectionsCycle scenario
            optionData.Add(new TMP_Dropdown.OptionData(KConnectionCyclesScenarioName));

            // Adding RandomConnectionsSwap scenario
            optionData.Add(new TMP_Dropdown.OptionData(KRandomConnectionSwapScenarioName));

            m_ScenariosDropdown.AddOptions(optionData);
            m_ScenariosDropdown.onValueChanged.AddListener(OnScenarioChanged);
        }

        void Update()
        {
            if (m_NetworkSimulator.IsAvailable)
            {
                if (Input.touchCount == KNbTouchesToOpenWindow && AnyTouchDown() ||
                    m_OpenWindowKeyCode != KeyCode.None && Input.GetKeyDown(m_OpenWindowKeyCode))
                {
                    ToggleVisibility();
                }

                var selectedPreset = m_PresetsDropdown.options[m_PresetsDropdown.value].text;
                if (selectedPreset != m_NetworkSimulator.CurrentPreset.Name)
                {
                    for (var i = 0; i < m_PresetsDropdown.options.Count; i++)
                    {
                        if (m_PresetsDropdown.options[i].text == m_NetworkSimulator.CurrentPreset.Name)
                        {
                            m_PresetsDropdown.value = i;
                        }
                    }
                }

            }
            else
            {
                if (_mShown)
                {
                    Hide();
                }
            }
        }

        static bool AnyTouchDown()
        {
            foreach (var touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    return true;
                }
            }
            return false;
        }

        public void SimulateDisconnect()
        {
            m_NetworkSimulator.Disconnect();
        }

        public void TriggerLagSpike()
        {
            double.TryParse(m_LagSpikeDuration.text, out var duration);
            m_NetworkSimulator.TriggerLagSpike(TimeSpan.FromMilliseconds(duration));
        }

        public void SanitizeLagSpikeDurationInputField()
        {
            m_LagSpikeDuration.text = Regex.Replace(m_LagSpikeDuration.text, "[^0-9]", "");
        }

        public void TriggerScenario()
        {
            if (m_NetworkSimulator.Scenario != null)
            {
                m_NetworkSimulator.Scenario.IsPaused = !m_NetworkSimulator.Scenario.IsPaused;
                UpdateScenarioButton();
            }
        }

        void UpdateScenarioButton()
        {
            if (m_NetworkSimulator.Scenario != null)
            {
                m_ScenariosButtonText.text = m_NetworkSimulator.Scenario.IsPaused ? KResumeString : KPauseString;
                m_ScenariosButton.interactable = true;
                m_ScenariosButtonText.color = m_ScenariosButton.colors.normalColor;
            }
            else
            {
                m_ScenariosButtonText.text = "None";
                m_ScenariosButton.interactable = false;
                m_ScenariosButtonText.color = m_ScenariosButton.colors.disabledColor;
            }
        }
#endif
    }
}
