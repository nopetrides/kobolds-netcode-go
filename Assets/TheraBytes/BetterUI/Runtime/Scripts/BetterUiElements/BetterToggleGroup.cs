using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{
    [DisallowMultipleComponent]
    [HelpURL("https://documentation.therabytes.de/better-ui/BetterToggleGroup.html")]
    public class BetterToggleGroup : ToggleGroup
    {
        public IReadOnlyList<Toggle> Toggles { get { return base.m_Toggles; } }

        protected override void Start()
        {
            BetterEnsureValidState();
        }

        protected override void OnEnable()
        {
            BetterEnsureValidState();
        }

        public void BetterEnsureValidState()
        {
            // set one on if there is not any but there should be any
            if (!allowSwitchOff && !this.AnyInteractableTogglesOn() && GetAllInteractableToggles().Any())
            {
                var toggle = GetAllInteractableToggles().First();
                toggle.SetIsOn(true);
                NotifyToggleOn(toggle);
            }

            IEnumerable<Toggle> activeToggles = this.ActiveInteractableToggles();

            // set all but the first off, if there are several on
            if (activeToggles.Any())
            {
                Toggle firstActive = activeToggles.First();

                foreach (Toggle toggle in activeToggles)
                {
                    if (toggle == firstActive)
                        continue;

                    toggle.SetIsOn(false);
                }
            }
        }

        public IEnumerable<Toggle> GetAllInteractableToggles()
        {
            return m_Toggles.Where(toggle => toggle.interactable);
        }
    }


    public static class ToggleGroupExtensions
    {
        /// <summary>
        /// Gets all toggles where isOn is true and that are interactable right now.
        /// </summary>
        public static IEnumerable<Toggle> ActiveInteractableToggles(this ToggleGroup self)
        {
            return self.ActiveToggles()
                .Where(toggle => toggle.interactable);
        }

        /// <summary>
        /// Gets the first toggle where isOn is true and that are interactable right now.
        /// </summary>
        public static Toggle GetFirstActiveInteractableToggle(this ToggleGroup self)
        {
            return self.ActiveToggles()
                .FirstOrDefault(toggle => toggle.interactable);
        }
        /// <summary>
        /// returns true if there is any toggle that isOn and interactable.
        /// </summary>
        public static bool AnyInteractableTogglesOn(this ToggleGroup self)
        {
            return self.GetFirstActiveInteractableToggle() != null;
        }
    }
}
