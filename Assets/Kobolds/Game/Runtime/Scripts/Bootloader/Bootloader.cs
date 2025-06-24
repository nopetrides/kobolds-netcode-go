using System;
using System.Collections.Generic;
using DG.Tweening;
using Febucci.UI;
using Kobold;
using Kobolds.Runtime.Managers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Kobolds.Runtime
{
    public class Bootloader : MonoBehaviour
    {
		[FormerlySerializedAs("_gameLoader")] 
		[SerializeField] private GlobalsMgr GameLoader;

		[SerializeField] private float InDelay = 0.1f;
		[SerializeField] private float OutDelay = 0.1f;
		
		[SerializeField] private TypewriterByCharacter StartingTypewriter;
		
		[SerializeField] private List<string> AllowedSkipActions = new() {"Escape", "Submit", "Cancel", "Fire", "Click"};

		private Sequence _sequence;
		
		private readonly List<InputAction> _subscribedActions = new();
		
		// Start is called once before the first execution of Update after the MonoBehaviour is created
		private void Start()
		{
			foreach (var map in KoboldInputSystemManager.Instance.NewInputSystem.actions.actionMaps)
			{
				foreach (var action in map.actions)
				{
					if (!AllowedSkipActions.Contains(action.name)) continue;
					var a = action; // Capture local copy for closure
					a.performed += OnAnyInput;
					_subscribedActions.Add(a);
				}
			}
		}
		
		private void OnEnable()
		{
			StartSequence();
		}

		private void StartSequence()
		{
			_sequence = DOTween.Sequence();
			_sequence.SetDelay(InDelay);
			_sequence.OnComplete(ShowSplash);
			_sequence.Play();
		}

		private void ShowSplash()
		{
			StartingTypewriter.ShowText("P");
		}


		// Called by the splash after finishing
        public void LoadIntro()
		{
			EndSequence();
		}

		private void EndSequence()
		{
			_sequence = DOTween.Sequence();
			_sequence.SetDelay(OutDelay);
			_sequence.OnComplete(IntroComplete);
			_sequence.Play();
		}

		private void IntroComplete()
		{
			Instantiate(GameLoader);
		}
		
		
		private void OnDestroy()
		{
			// Clean up input listeners
			foreach (var action in _subscribedActions) action.performed -= OnAnyInput;

			_subscribedActions.Clear();
		}

		private void OnAnyInput(InputAction.CallbackContext ctx)
		{
			Interrupt();
		}

		private void Interrupt()
		{
			_sequence.Kill();
			IntroComplete();
		}
	}
}
