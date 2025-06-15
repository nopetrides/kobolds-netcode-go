using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    /// <summary>
    /// Template class for all view controllers.
    /// </summary>
    abstract class UIView : MonoBehaviour
    {
        /// <summary>
        /// Root of the visual element that this view controls
        /// </summary>
        protected VisualElement MRoot;

        // Child reference to child view if any.
        protected UIView MChildView;

        /// <summary>
        /// Determines if the view is modal: Allowing parent windows to remain visable.
        /// </summary>
        [SerializeField]
        public bool IsModal;

        /// <summary>
        /// Displays a targeted child view.
        /// </summary>
        /// <param name="targetUI">Child ui view to show.</param>
        protected void DisplayChildView(UIView targetUI)
        {
            if (!targetUI.IsModal)
            {
                MChildView?.Hide();
            }

            MChildView?.UnregisterEvents();
            MChildView = targetUI;
            MChildView.Show();
            MChildView.RegisterEvents();
        }

        public virtual void Initialize(VisualElement root)
        {
            MRoot = root;
        }

        void Show()
        {
            MRoot.style.display = DisplayStyle.Flex;
            HandleOnShown();
        }

        void Hide()
        {
            MRoot.style.display = DisplayStyle.None;
            HandleOnHidden();
        }

        protected abstract void RegisterEvents();

        protected abstract void UnregisterEvents();

        protected virtual void HandleOnShown() { }
        protected virtual void HandleOnHidden() { }
    }
}
