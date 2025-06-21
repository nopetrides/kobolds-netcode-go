using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI
{
	/// <summary>
	/// Main UI controller. Holds child views for sub views.
	/// </summary>
	[RequireComponent(typeof(UIDocument))]
	class KoboldMainMenuController : KoboldUIView
	{
		UIDocument _mUIDocument;
		private KoboldUIInputHandler _uiInputHandler;

		/// <summary>
		/// Home view: Main menu items
		/// </summary>
		[SerializeField]
		KoboldHomeScreenView m_HomeView;

		KoboldUIView _mCurrentView;

		void OnEnable()
		{
			_mUIDocument = GetComponent<UIDocument>();
			Initialize(_mUIDocument.rootVisualElement);

			// Setup UI input handler
			SetupUIInputHandler();

			m_HomeView.Initialize(MRoot.Q<VisualElement>("main-menu-window"));
			RegisterEvents();
			DisplayChildView(m_HomeView);
		}

		void OnDisable()
		{
			UnregisterEvents();
		}

		private void SetupUIInputHandler()
		{
			// Add UI input handler if not present
			_uiInputHandler = GetComponent<KoboldUIInputHandler>();
			if (_uiInputHandler == null)
			{
				_uiInputHandler = gameObject.AddComponent<KoboldUIInputHandler>();
				Debug.Log("[KoboldMainMenuController] Added KoboldUIInputHandler component");
			}
			
			_uiInputHandler.SetCurrentDocument(_mUIDocument);
			Debug.Log("[KoboldMainMenuController] UI input handler setup complete");
		}

		protected override void RegisterEvents()
		{
			// Register any additional events here if needed
		}

		protected override void UnregisterEvents()
		{
			// Unregister any additional events here if needed
		}
	}
}