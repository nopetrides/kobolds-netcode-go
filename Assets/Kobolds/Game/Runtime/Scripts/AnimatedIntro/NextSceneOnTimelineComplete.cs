using Kobolds.Runtime;
using P3T.Scripts.Managers;
using UnityEngine;
using UnityEngine.Playables;

public class NextSceneOnTimelineComplete : MonoBehaviour
{
    [SerializeField] private PlayableDirector Pd;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Pd.stopped += OnTimelineStop;
    }

    private void OnTimelineStop(PlayableDirector obj)
    {
        Debug.Log($"PlayableDirector Stopped");
		if (Application.isPlaying)
			SceneMgr.Instance?.LoadScene(GameScenes.CharactersScene.ToString(), null);
    }
}
