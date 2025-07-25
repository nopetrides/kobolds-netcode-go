using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheraBytes.BetterUi
{   
    [HelpURL("https://documentation.therabytes.de/better-ui/BetterButton.html")]
    [AddComponentMenu("Better UI/Controls/Better Button", 30)]
    public class BetterButton : Button, IBetterTransitionUiElement
    {
        public List<Transitions> BetterTransitions { get { return betterTransitions; } }

        [SerializeField, DefaultTransitionStates]
        List<Transitions> betterTransitions = new List<Transitions>();

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            if (!(base.gameObject.activeInHierarchy))
                return;

            foreach (var info in betterTransitions)
            {
                info.SetState(state.ToString(), instant);
            }
        }



        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Press();
            }
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
            Press();
        }

        private void Press()
        {
            if (IsActive() && IsInteractable())
            {
                GlobalApplier.Instance.NotifyButtonClick(this);
            }
        }
    }
}
