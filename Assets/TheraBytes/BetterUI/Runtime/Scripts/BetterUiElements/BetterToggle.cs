using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{
    [HelpURL("https://documentation.therabytes.de/better-ui/BetterToggle.html")]
    [AddComponentMenu("Better UI/Controls/Better Toggle", 30)]
    public class BetterToggle : Toggle, IBetterTransitionUiElement
    {
        public static List<BetterToggle> togglesThatTurnedOn = new List<BetterToggle>();
        public static List<BetterToggle> togglesThatTurnedOff = new List<BetterToggle>();
        public List<Transitions> BetterTransitions { get { return betterTransitions; } }
        public List<Transitions> BetterTransitionsWhenOn { get { return betterTransitionsWhenOn; } }
        public List<Transitions> BetterTransitionsWhenOff { get { return betterTransitionsWhenOff; } }
        public List<Transitions> BetterToggleTransitions { get { return betterToggleTransitions; } }

        [SerializeField, DefaultTransitionStates]
        List<Transitions> betterTransitions = new List<Transitions>();

        [SerializeField, TransitionStates("On", "Off")]
        List<Transitions> betterToggleTransitions = new List<Transitions>();
        [SerializeField, DefaultTransitionStates]
        List<Transitions> betterTransitionsWhenOn = new List<Transitions>();
        [SerializeField, DefaultTransitionStates]
        List<Transitions> betterTransitionsWhenOff = new List<Transitions>();

        public new bool isOn { get { return base.isOn; } set { Set(value); } }

        bool wasOn;

        protected override void OnEnable()
        {
            base.OnEnable();
            ValueChanged(base.isOn, true);
            DoStateTransition(currentSelectionState, true);
        }

        void Update()
        {
            if (wasOn && !isOn)
            {
                togglesThatTurnedOff.Add(this);
            }
            else if (!wasOn && isOn)
            {
                togglesThatTurnedOn.Add(this);
            }
        }

        void LateUpdate()
        {
            foreach (var tgl in togglesThatTurnedOff)
            {
                if (tgl != null)
                {
                    tgl.ValueChanged(false);
                    GlobalApplier.Instance.NotifyToggleChanged(this, false);
                }
            }

            togglesThatTurnedOff.Clear();

            foreach (var tgl in togglesThatTurnedOn)
            {
                if (tgl != null)
                {
                    tgl.ValueChanged(true);
                    GlobalApplier.Instance.NotifyToggleChanged(this, true);
                }
            }

            togglesThatTurnedOn.Clear();
        }

        public void Set(bool isOn, bool sendCallback = true)
        {
            if (base.isOn == isOn && wasOn == isOn)
                return;

            bool prevAllowSwitchOff = false;

            if (isOn && group != null)
            {
                prevAllowSwitchOff = group.allowSwitchOff;
                group.allowSwitchOff = true;

                // set all other toggles of the group off here already,
                // so that any contents of "tabs" and their navigation groups are disabled
                // before the potential tab content of this toggle becomes enabled.
                if (group is BetterToggleGroup betterGroup)
                {
                    foreach (Toggle tgl in betterGroup.Toggles)
                    {
                        if (tgl == this)
                            continue;

                        tgl.SetIsOn(false, sendCallback);
                    }
                }
                else
                {
                    Debug.LogWarning($@"""Better Toggle's"" group should be a ""Better Toggle Group"" or null. Otherwise, some internal logic might break.");
                }
            }

            // apply Better UI Transitions and updated "wasOn"
            this.ValueChanged(isOn);

            if (sendCallback)
            {
                base.isOn = isOn;
                GlobalApplier.Instance.NotifyToggleChanged(this, isOn);
            }
            else
            {
                base.SetIsOnWithoutNotify(isOn);
            }

            if (isOn && group != null)
            {
                group.allowSwitchOff = prevAllowSwitchOff;
            }
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            if (!(base.gameObject.activeInHierarchy))
                return;

            var stateTransitions = (isOn)
                ? betterTransitionsWhenOn
                : betterTransitionsWhenOff;

            foreach (var info in stateTransitions)
            {
                info.SetState(state.ToString(), instant);
            }

            foreach (var info in betterTransitions)
            {
                if (state != SelectionState.Disabled && isOn)
                {
                    var tglTr = betterToggleTransitions.FirstOrDefault(
                        (o) => o.TransitionStates != null && info.TransitionStates != null
                            && o.TransitionStates.Target == info.TransitionStates.Target
                            && o.Mode == info.Mode);

                    if (tglTr != null)
                    {
                        continue;
                    }
                }

                info.SetState(state.ToString(), instant);
            }
        }

        private void ValueChanged(bool on)
        {
            ValueChanged(on, false);
        }

        private void ValueChanged(bool on, bool immediate)
        {
            wasOn = on;
            foreach (var state in betterToggleTransitions)
            {
                state.SetState((on) ? "On" : "Off", immediate);
            }

            var stateTransitions = (on)
                ? betterTransitionsWhenOn
                : betterTransitionsWhenOff;

            foreach (var state in stateTransitions)
            {
                state.SetState(currentSelectionState.ToString(), immediate);
            }
        }

    }
}

namespace UnityEngine.UI
{
    using TheraBytes.BetterUi;

    public static class ToggleExtensions
    {
        /// <summary>
        /// This method is sets "isOn" to the given value. If the toggle is in fact a BetterToggle, it also directly applies Better UI transitions. If you use <see cref="Toggle.isOn"/> instead, the BetterUI transitions will be applied in the next <c>LateUpdate</c> call.
        /// </summary>
        /// <param name="self">The toggle or better toggle to set on or off.</param>
        /// <param name="isOn">the new value for the <see cref="Toggle.isOn"/> property.</param>
        /// <param name="sendCallback">If true, callbacks will be invoked. Otherwise <see cref="Toggle.SetIsOnWithoutNotify"/> will be used.</param>
        public static void SetIsOn(this Toggle self, bool isOn, bool sendCallback = true)
        {
            if (self is BetterToggle betterToggle)
            {
                betterToggle.Set(isOn, sendCallback);
            }
            else
            {
                if (sendCallback)
                {
                    self.isOn = isOn;
                }
                else
                {
                    self.SetIsOnWithoutNotify(isOn);
                }
            }
        }

    }
}
