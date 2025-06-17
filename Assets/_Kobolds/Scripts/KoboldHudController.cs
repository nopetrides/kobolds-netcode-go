using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI
{
	[RequireComponent(typeof(UIDocument))]
	public class KoboldHUDController : KoboldUIView
	{
		[SerializeField] private KoboldHUDView _hudView;

		private UIDocument _document;

		private void OnEnable()
		{
			_document = GetComponent<UIDocument>();
			Initialize(_document.rootVisualElement);

			_hudView.Initialize(MRoot.Q<VisualElement>("hud-window"));
			RegisterEvents();
			DisplayChildView(_hudView);
		}

		private void OnDisable()
		{
			UnregisterEvents();
		}

		protected override void RegisterEvents() { }

		protected override void UnregisterEvents() { }
	}
}
