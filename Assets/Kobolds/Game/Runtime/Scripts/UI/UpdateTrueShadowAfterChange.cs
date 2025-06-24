using LeTai.TrueShadow;
using UnityEngine;

public class UpdateTrueShadowAfterChange : MonoBehaviour
{
	private TrueShadow _ts;
	
	public void UpdateTMPSubmeshesShadow()
	{
		// Trying to work around updating after typewriter
		if (_ts != null)
			DestroyImmediate(_ts);
		_ts = gameObject.AddComponent<TrueShadow>();
		_ts.Size = 3;
		_ts.Spread = 0.9f;
		_ts.OffsetAngle = 0;
		_ts.OffsetDistance = 0;
		_ts.Color = Color.white;
		_ts.IgnoreCasterColor = true;
	}
}
