using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi
{
    public enum TransitionUpdateMode
    {
        /// <summary>
        /// Changes the transition state instantly within one frame.</summary>
        UpdateInstantly,
        /// <summary>
        /// Tries to use the state transition time to apply the new value.</summary>
        UpdateWithTransition,
        /// <summary>
        /// Does not update to the new value. It will update when the state changes or when explicitly set via code.</summary>
        DoNotUpdate,
    }

    public static class Helper
    {
        /// <summary>
        /// Tries to find all transitions for the given state-type and applies a new value for the given state.
        /// </summary>
        /// <typeparam name="T">The type a state can have.</typeparam>
        /// <param name="transitions">The list of transitions.</param>
        /// <param name="stateName">The name of the state to change the value for.</param>
        /// <param name="newStateValue">The new value for the given state.</param>
        /// <param name="updateMode">If the current state is the state you are changing, define how the new value is going to be applied.</param>
        public static void SetTransitionStateValues<T>(this IList<Transitions> transitions, string stateName, T newStateValue,
            TransitionUpdateMode updateMode = TransitionUpdateMode.UpdateWithTransition)
        {
            foreach (var transition in transitions)
            {
                if (transition.TransitionStates is TransitionStateCollection<T>)
                {
                    SetTransitionStateValue(transition, stateName, newStateValue, updateMode);
                }
            }
        }

        /// <summary>
        /// Tries to apply a new value for the given state. The <c>transition.TransitionStates</c> must be of the correct type.
        /// </summary>
        /// <typeparam name="T">The type a state can have.</typeparam>
        /// <param name="transitions">The list of transitions.</param>
        /// <param name="stateName">The name of the state to change the value for.</param>
        /// <param name="newStateValue">The new value for the given state.</param>
        /// <param name="updateMode">If the current state is the state you are changing, define how the new value is going to be applied.</param>
        public static void SetTransitionStateValue<T>(this Transitions transition, string stateName, T newStateValue,
            TransitionUpdateMode updateMode = TransitionUpdateMode.UpdateWithTransition)
        {
            if (!(transition.TransitionStates is TransitionStateCollection<T> transitionStates))
            {
                Debug.LogError($"Cannot set transition state: The transition is for {transition.Mode}, not for {typeof(T).Name}");
                return;
            }

            var states = transitionStates.GetStates();
            var state= states.FirstOrDefault(s => s.Name == stateName);
            if (state == null)
            {
                Debug.LogError($"Cannot set tranistion state: No state found with name '{stateName}'.");
                return;
            }

            state.StateObject = newStateValue;

            // update state if it is the currently active one
            if (updateMode != TransitionUpdateMode.DoNotUpdate && transition.CurrentStateName == stateName)
            {
                bool instant = updateMode == TransitionUpdateMode.UpdateInstantly;
                transitionStates.Apply(stateName, instant);
            }
        }

    }
}
