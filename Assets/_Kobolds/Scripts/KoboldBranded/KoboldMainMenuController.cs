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

			m_HomeView.Initialize(MRoot.Q<VisualElement>("main-menu-window"));
			RegisterEvents();
			DisplayChildView(m_HomeView);
		}

		void OnDisable()
		{
			UnregisterEvents();
		}

		protected override void RegisterEvents()
		{
		}

		protected override void UnregisterEvents()
		{
		}
	}
}