using Unity.Multiplayer.Samples.SocialHub.GameManagement;

using Unity.Services.Vivox;
using Unity.Multiplayer.Samples.SocialHub.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// Ingame Menu to show options like exit, go to main menu etc.
    /// </summary>
    class IngameMenu : MonoBehaviour
    {
        [SerializeField]
        UIDocument m_UIDocument;

        [SerializeField]
        VisualTreeAsset m_IngameMenuAsset;

        VisualElement _mRoot;
        VisualElement _mMenu;
        VisualElement _mScreenOverlay;

        Button _mBurgerButton;
        Button _mExitButton;
        Button _mGotoMainButton;
        Button _mCloseMenuButton;

        Toggle _mMuteToggle;

        Slider _mInputVolumeSlider;
        Slider _mOutputVolumeSlider;

        DropdownField _mInputDevicesDropdown;
        DropdownField _mOutputDevicesDropdown;

        void OnEnable()
        {
            _mRoot = m_UIDocument.rootVisualElement.Q<VisualElement>("ingame-menu-container");
            _mRoot.Add(m_IngameMenuAsset.CloneTree().GetFirstChild());

            _mScreenOverlay = _mRoot.Q<VisualElement>("screen-overlay");

            _mBurgerButton = _mRoot.Q<Button>("burger-button");
            _mBurgerButton.clicked += ShowMenu;

            _mMenu = _mRoot.Q<VisualElement>("menu");
            _mMenu.AddToClassList(UIUtils.SInactiveUSSClass);

            _mExitButton = _mMenu.Q<Button>("btn-exit");
            _mExitButton.clicked += QuitGame;

            _mGotoMainButton = _mMenu.Q<Button>("btn-goto-main");
            _mGotoMainButton.clicked += GoToMainMenuScene;

            _mCloseMenuButton = _mMenu.Q<Button>("btn-close-menu");
            _mCloseMenuButton.clicked += HideMenu;

            GameInput.Actions.Player.TogglePauseMenu.performed += OnTogglePauseMenu;

            // Audio settings

            // Input Selection
            _mInputDevicesDropdown = _mMenu.Q<DropdownField>("audio-input");
            PopulateAudioInputDevices();
            _mInputDevicesDropdown.RegisterValueChangedCallback(evt => OnInputDeviceDropDownChanged(evt));

            // Output Selection
            _mOutputDevicesDropdown = _mMenu.Q<DropdownField>("audio-output");
            PopulateAudioOutputDevices();
            _mOutputDevicesDropdown.value = VivoxService.Instance.ActiveOutputDevice.DeviceName;
            _mOutputDevicesDropdown.RegisterValueChangedCallback(evt => OnOutputDeviceDropdownChanged(evt));

            // Input Volume
            _mInputVolumeSlider = _mMenu.Q<Slider>("input-volume");
            _mInputVolumeSlider.value = VivoxService.Instance.InputDeviceVolume + 50;
            _mInputVolumeSlider.RegisterValueChangedCallback(evt => OnInputVolumeChanged(evt));

            // Output Volume
            _mOutputVolumeSlider = _mMenu.Q<Slider>("output-volume");
            _mOutputVolumeSlider.value = VivoxService.Instance.OutputDeviceVolume + 50;
            _mOutputVolumeSlider.RegisterValueChangedCallback(evt => OnOutputVolumeChanged(evt));

            // Mute Button
            _mMuteToggle = _mMenu.Q<Toggle>("mute-checkbox");
            _mMuteToggle.SetValueWithoutNotify(VivoxService.Instance.IsInputDeviceMuted);
            _mMuteToggle.RegisterValueChangedCallback(evt => OnMuteCheckboxChanged(evt));

            VivoxService.Instance.AvailableInputDevicesChanged += PopulateAudioInputDevices;
            VivoxService.Instance.AvailableOutputDevicesChanged += PopulateAudioOutputDevices;
            HideMenu();
        }

        void OnOutputVolumeChanged(ChangeEvent<float> evt)
        {
            // Vivox Volume is from  -50 to 50
            var vol = evt.newValue - 50;
            VivoxService.Instance.SetOutputDeviceVolume((int)vol);
        }

        void OnInputVolumeChanged(ChangeEvent<float> evt)
        {
            // Vivox Volume is from  -50 to 50
            var vol = evt.newValue - 50;
            VivoxService.Instance.SetInputDeviceVolume((int)vol);
        }

        void OnTogglePauseMenu(InputAction.CallbackContext _)
        {
            ShowMenu();
        }

        void ShowMenu()
        {
            InputSystemManager.Instance.EnableUIInputs();
            _mMenu.RemoveFromClassList(UIUtils.SInactiveUSSClass);
            _mMenu.AddToClassList(UIUtils.SActiveUSSClass);
            _mScreenOverlay.style.display = DisplayStyle.Flex;
            _mMenu.SetEnabled(true);
        }

        void HideMenu()
        {
            InputSystemManager.Instance.EnableGameplayInputs();
            _mScreenOverlay.style.display = DisplayStyle.None;
            _mMenu.RemoveFromClassList(UIUtils.SActiveUSSClass);
            _mMenu.AddToClassList(UIUtils.SInactiveUSSClass);
            _mMenu.SetEnabled(false);
        }

        void PopulateAudioInputDevices()
        {
            _mInputDevicesDropdown.choices.Clear();
            foreach (var inputDevice in VivoxService.Instance.AvailableInputDevices)
            {
                _mInputDevicesDropdown.choices.Add(inputDevice.DeviceName);
            }

            _mInputDevicesDropdown.SetValueWithoutNotify(VivoxService.Instance.ActiveInputDevice.DeviceName);
        }

        void PopulateAudioOutputDevices()
        {
            _mOutputDevicesDropdown.choices.Clear();
            foreach (var outputDevice in VivoxService.Instance.AvailableOutputDevices)
            {
                _mOutputDevicesDropdown.choices.Add(outputDevice.DeviceName);
            }

            _mOutputDevicesDropdown.SetValueWithoutNotify(VivoxService.Instance.ActiveOutputDevice.DeviceName);
        }

        void OnMuteCheckboxChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                VivoxService.Instance.MuteInputDevice();
            else
                VivoxService.Instance.UnmuteInputDevice();
        }

        async void OnOutputDeviceDropdownChanged(ChangeEvent<string> evt)
        {
            // Capture the values because we need them if something goes wrong.
            var newValue = evt.newValue;
            DropdownField dropdown = (DropdownField)evt.target;

            foreach (var outputDevice in VivoxService.Instance.AvailableOutputDevices)
            {
                if (outputDevice.DeviceName == newValue)
                {
                    await VivoxService.Instance.SetActiveOutputDeviceAsync(outputDevice);
                    break;
                }
            }

            if (VivoxService.Instance.ActiveOutputDevice.DeviceName != newValue)
            {
                Debug.LogWarning("Could not set Audio Output Device " + newValue);
                dropdown.value = VivoxService.Instance.ActiveOutputDevice.DeviceName;
            }
        }

        async void OnInputDeviceDropDownChanged(ChangeEvent<string> evt)
        {
            // Capture the values because we need them if something goes wrong.
            var newValue = evt.newValue;
            DropdownField dropdown = (DropdownField)evt.target;

            foreach (var inputDevice in VivoxService.Instance.AvailableInputDevices)
            {
                if (inputDevice.DeviceName == newValue)
                {
                    await VivoxService.Instance.SetActiveInputDeviceAsync(inputDevice);
                    break;
                }
            }

            if (VivoxService.Instance.ActiveInputDevice.DeviceName != newValue)
            {
                Debug.LogWarning("Could not set Audio Input Device " + newValue);
                dropdown.value = VivoxService.Instance.ActiveOutputDevice.DeviceName;
            }
        }

        void OnDisable()
        {
            _mBurgerButton.clicked -= ShowMenu;
            _mExitButton.clicked -= QuitGame;
            _mGotoMainButton.clicked -= GoToMainMenuScene;
            _mCloseMenuButton.clicked -= HideMenu;

            _mInputDevicesDropdown.UnregisterValueChangedCallback(evt => OnInputDeviceDropDownChanged(evt));
            _mOutputDevicesDropdown.UnregisterValueChangedCallback(evt => OnOutputDeviceDropdownChanged(evt));

            _mInputVolumeSlider.UnregisterValueChangedCallback(evt => OnInputVolumeChanged(evt));
            _mOutputVolumeSlider.UnregisterValueChangedCallback(evt => OnOutputVolumeChanged(evt));

            VivoxService.Instance.AvailableInputDevicesChanged -= PopulateAudioInputDevices;
            VivoxService.Instance.AvailableOutputDevicesChanged -= PopulateAudioOutputDevices;

            GameInput.Actions.Player.TogglePauseMenu.performed -= OnTogglePauseMenu;
        }

        static void GoToMainMenuScene()
        {
            GameplayEventHandler.ReturnToMainMenuPressed();
        }

        static void QuitGame()
        {
            GameplayEventHandler.QuitGamePressed();
        }
    }
}
