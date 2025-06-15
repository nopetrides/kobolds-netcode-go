using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    namespace UIToolkitSamples
    {
        /// <summary>
        /// Main UI controller. Holds child views for sub views.
        /// </summary>
        [RequireComponent(typeof(UIDocument))]
        class MainUIController : UIView
        {
            UIDocument _mUIDocument;

            /// <summary>
            /// Home view: Main menu items
            /// </summary>
            [SerializeField]
            HomeScreenView m_HomeView;

            UIView _mCurrentView;

            void OnEnable()
            {
                _mUIDocument = GetComponent<UIDocument>();
                Initialize(_mUIDocument.rootVisualElement);

                m_HomeView.Initialize(MRoot.Q<VisualElement>("HomeScreen"));
                RegisterEvents();
                DisplayChildView(m_HomeView);
            }

            void OnDisable()
            {
                UnregisterEvents();
            }

            protected override void RegisterEvents() { }

            protected override void UnregisterEvents() { }
        }
    }
}
