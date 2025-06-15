using UnityEngine.UIElements;
using Kobold.UI.Configuration;

namespace Kobold.UI
{
	/// <summary>
	/// Interface that all UI presenters must implement
	/// </summary>
	public interface IUIPresenter
	{
		/// <summary>
		/// Called once when the presenter is created
		/// Used to cache references and set up event bindings
		/// </summary>
		/// <param name="root">The root visual element of the window</param>
		/// <param name="config">The UI configuration for accessing shared settings</param>
		void Initialize(VisualElement root, KoboldUIConfiguration config);
        
		/// <summary>
		/// Called every time the window is shown
		/// Used to refresh data, set focus, etc.
		/// </summary>
		void OnShow();
        
		/// <summary>
		/// Called every time the window is hidden
		/// Used to save state, cleanup temporary data, etc.
		/// </summary>
		void OnHide();
        
		/// <summary>
		/// Called when the presenter is being destroyed
		/// Used to unregister events, dispose resources, etc.
		/// </summary>
		void Cleanup();
	}
}
