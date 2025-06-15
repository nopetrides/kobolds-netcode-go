using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI.Configuration
{
	/// <summary>
	///     Shared UI configuration settings
	///     Window definitions are now handled separately via UIWindowDefinition assets
	/// </summary>
	[CreateAssetMenu(fileName = "KoboldUIConfiguration", menuName = "Kobold/UI/Configuration")]
	public class KoboldUIConfiguration : ScriptableObject
	{
		[Header("Animation Settings")]
		[Tooltip("Default animation duration in seconds")]
		[Range(0.1f, 2.0f)]
		public float defaultAnimationDuration = 0.3f;

		[Tooltip("Stagger delay between child animations")]
		[Range(0.01f, 0.5f)]
		public float childAnimationStagger = 0.05f;

		[Header("UI Behavior")]
		[Tooltip("Should windows auto-animate when shown")]
		public bool autoAnimateWindows = true;

		[Tooltip("Enable UI sound effects")]
		public bool enableUISounds = true;

		[Header("Default Values")]
		[Tooltip("Default player name for input fields")]
		public string defaultPlayerName = "Kobold";

		[Tooltip("Default session name for multiplayer")]
		public string defaultSessionName = "KoboldHub";

		[Header("UI Settings")]
		[Tooltip("Time in seconds before UI input is accepted after window change")]
		[Range(0f, 1f)]
		public float inputDelayAfterTransition = 0.1f;

		[Tooltip("Enable gamepad/controller support for UI navigation")]
		public bool enableGamepadSupport = true;
		
		public PanelSettings defaultPanelSettings;

#if UNITY_EDITOR
		private void OnValidate()
		{
			// Clamp animation values
			defaultAnimationDuration = Mathf.Clamp(defaultAnimationDuration, 0.1f, 2.0f);
			childAnimationStagger = Mathf.Clamp(childAnimationStagger, 0.01f, 0.5f);
			inputDelayAfterTransition = Mathf.Clamp(inputDelayAfterTransition, 0f, 1f);
		}
#endif
	}
}
