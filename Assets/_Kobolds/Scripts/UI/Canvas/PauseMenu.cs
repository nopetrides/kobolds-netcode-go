using Kobold.GameManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Kobold
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _quitButton;

        private KoboldCanvasManager _canvasManager;

        public void Initialize(KoboldCanvasManager canvasManager)
        {
            _canvasManager = canvasManager;
            
            _resumeButton.onClick.AddListener(OnResume);
            _mainMenuButton.onClick.AddListener(OnMainMenu);
            _quitButton.onClick.AddListener(OnQuit);
        }

        private void OnDestroy()
        {
            _resumeButton.onClick.RemoveListener(OnResume);
            _mainMenuButton.onClick.RemoveListener(OnMainMenu);
            _quitButton.onClick.RemoveListener(OnQuit);
        }

        private void OnResume()
        {
            _canvasManager.OnPlayerUnpause();
        }

        private void OnMainMenu()
        {
            KoboldEventHandler.ReturnToMainMenuPressed();
        }

        private void OnQuit()
        {
            KoboldEventHandler.QuitGamePressed();
        }
    }
} 