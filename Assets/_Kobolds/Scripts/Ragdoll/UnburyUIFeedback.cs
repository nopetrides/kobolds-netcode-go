using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Kobold
{
	/// <summary>
	/// Handles UI feedback for unbury actions in the Kobolds namespace.
	/// </summary>
	public class UnburyUIFeedback : MonoBehaviour
	{
		/// <summary>
		/// Reference to the <c>UnburyController</c>, responsible for controlling the unburying process and tracking struggle progress.
		/// </summary>
		[Header("References")]
		[SerializeField] private UnburyController Unbury;

		/// Represents a group of UI elements and controls their visibility, interactability, and transparency.
		[SerializeField] private CanvasGroup CanvasGroup;

		/// <summary>
		/// TextMeshProUGUI component used to display and animate mash input prompts or feedback.
		/// </summary>
		[SerializeField] private TextMeshProUGUI MashText;

		/// <summary>
		/// Represents the image UI component used to visually indicate progress.
		/// </summary>
		[SerializeField] private Image FillImage;

		/// <summary>
		/// Determines the speed at which the mash text pulsing effect oscillates.
		/// </summary>
		[Header("Pulsing")]
		[SerializeField] private float PulseSpeed = 2f;

		/// <summary>
		/// Defines the minimum scale for the pulsing effect applied to UI elements.
		/// </summary>
		[SerializeField] private float PulseScaleMin = 0.9f;

		/// <summary>
		/// The maximum scale value during the pulsing animation of the UI element.
		/// Represents the upper limit for the scale oscillation effect.
		/// </summary>
		[SerializeField] private float PulseScaleMax = 1.1f;

		/// <summary>
		/// A RectTransform used for applying a shaking effect to the text.
		/// </summary>
		[Header("Shaking")]
		[SerializeField] private RectTransform TextShakeTransform;

		/// <summary>
		/// Represents the maximum intensity of shaking applied to UI elements
		/// based on the progress percentage during the unbury interaction.
		/// </summary>
		[SerializeField] private float MaxShakeAmount = 12f;

		/// <summary>
		/// Represents a gradient used to determine the fill color of the UI element based on progression percentage.
		/// </summary>
		[Header("Color")]
		[SerializeField] private Gradient FillColorGradient;

		/// <summary>
		/// Stores the base position of the text used for resetting its position after applying shake effects.
		/// </summary>
		private Vector3 _baseTextPosition;

		/// <summary>
		/// Initializes the component when the script instance is being loaded.
		/// If a CanvasGroup is assigned, sets its alpha value to fully visible (1f).
		/// Stores the initial anchored position of the RectTransform used for text shaking.
		/// </summary>
		private void Start()
		{
			if (CanvasGroup) CanvasGroup.alpha = 1f;
			_baseTextPosition = TextShakeTransform.anchoredPosition;
		}

		/// <summary>
		/// Updates the UI feedback for the unbury mechanic.
		/// Checks the validity of the `Unbury` reference and exits early if null or disabled.
		/// Calculates the progress of the unbury effort using `Unbury.StrugglePercentComplete`.
		/// Hides the UI when the unbury process is complete by setting `CanvasGroup.alpha` to 0.
		/// Applies a pulsing effect to the mash text using sine wave interpolation and scaling.
		/// Updates the fill amount and gradient color of the UI image based on progress.
		/// Adds shake effects to the UI text based on progress, using random offsets.
		/// </summary>
		private void Update()
		{
			if (!Unbury || !Unbury.enabled) return;

			float progress = Unbury.StrugglePercentComplete;

			// Hide when complete
			if (progress >= 1f)
			{
				if (CanvasGroup) CanvasGroup.alpha = 0f;
				return;
			}

			// Pulsing Text
			float pulse = Mathf.Lerp(PulseScaleMin, PulseScaleMax, (Mathf.Sin(Time.time * PulseSpeed) + 1f) / 2f);
			MashText.transform.localScale = Vector3.one * pulse;

			// Fill color & amount
			FillImage.fillAmount = progress;
			FillImage.color = FillColorGradient.Evaluate(progress);

			// Shake intensity
			float shakeAmount = Mathf.Lerp(0f, MaxShakeAmount, progress);
			Vector2 randomOffset = Random.insideUnitCircle * shakeAmount;
			TextShakeTransform.anchoredPosition = _baseTextPosition + (Vector3) randomOffset;
		}
	}
}