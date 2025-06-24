using System.Collections.Generic;
using Kobold;
using P3T.Scripts.Managers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class NextSceneOnTimelineComplete : MonoBehaviour
{
	[SerializeField] private PlayableDirector Pd;
	[SerializeField] private List<string> AllowedSkipActions = new() {"Escape", "Submit", "Cancel", "Fire", "Click"};

	private readonly List<InputAction> _subscribedActions = new();

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	private void Start()
	{
		Pd.stopped += OnTimelineStop;
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

	private void OnDestroy()
	{
		// Clean up input listeners
		foreach (var action in _subscribedActions) action.performed -= OnAnyInput;

		_subscribedActions.Clear();
	}

	private void OnAnyInput(InputAction.CallbackContext ctx)
	{
		Pd?.Stop();
	}

	private void OnTimelineStop(PlayableDirector obj)
	{
		Debug.Log("PlayableDirector Stopped");
		
		if (Application.isPlaying)
			SceneMgr.Instance?.LoadScene(nameof(SceneNames.KoboldMainMenu), null);
	}
}
