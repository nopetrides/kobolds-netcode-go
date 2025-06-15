using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI.Components
{
	/// <summary>
	///     Animated text field with focus states
	/// </summary>
	public class KoboldLabel : KoboldVisualElement
	{
		private readonly Label _label;

		public KoboldLabel() : this(string.Empty)
		{
		}

		public KoboldLabel(string text)
		{
			AddToClassList("kobold-label");

			// Container for the field
			var container = new VisualElement();
			Add(container);

			// Floating label
			_label = new Label(text);
			_label.AddToClassList("floating-label");
			container.Add(_label);
		}

		public string Text
		{
			get => _label?.text ?? string.Empty;
			set
			{
				if (_label != null)
					_label.text = value;
			}
		}

		protected override void PrepareForAnimation()
		{
			base.PrepareForAnimation();

			// Text fields slide in from bottom
			style.translate = new StyleTranslate(new Translate(0, 30, 0));
		}
	}

	// UXML Support with modern attributes
	[UxmlElement]
	public partial class KoboldLabelElement : VisualElement
	{
		private KoboldLabel _label;

		public KoboldLabelElement()
		{
			RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
		}

		[UxmlAttribute]
		private string Text { get; set; } = "Text";

		[UxmlAttribute]
		public float AnimationDuration { get; set; } = 0.3f;

		[UxmlAttribute]
		public float AnimationDelay { get; set; } = 0f;

		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			if (_label == null)
			{
				_label = new KoboldLabel(Text);
				_label.AnimationDuration = AnimationDuration;
				_label.AnimationDelay = AnimationDelay;
				Add(_label);
				Debug.Log($"[KoboldButtonElement] Attached: {name}, added: {_label != null}");
			}
		}
	}
}
