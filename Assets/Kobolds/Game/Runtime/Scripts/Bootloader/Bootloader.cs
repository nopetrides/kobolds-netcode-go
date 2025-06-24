using System;
using System.Collections;
using DG.Tweening;
using Febucci.UI;
using Kobolds.Runtime.Managers;
using UnityEngine;
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

		Sequence _sequence;
		
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

		public void Update()
		{
			if (Input.anyKeyDown)
			{
				_sequence.Kill();
				IntroComplete();
			}
		}
	}
}
