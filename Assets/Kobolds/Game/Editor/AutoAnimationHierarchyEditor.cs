// (Auto) Animation Hierarchy Editor
// 20250307
// https://github.com/GapVR
// forked from https://github.com/s-m-k/Unity-Animation-Hierarchy-Editor

#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class AutoAnimationHierarchyEditor : EditorWindow {
	private static int _columnWidth = 200;
	
	private Animator _animatorObject;
	private List<AnimationClip> _animationClips;
	private ArrayList _pathsKeys;
	private Hashtable _paths;

	Dictionary<string, string> _tempPathOverrides;

	private Vector2 _scrollPos = Vector2.zero;

	[MenuItem("Tools/Auto Animation Hierarchy Editor")]
	static void ShowWindow() {
		GetWindow<AutoAnimationHierarchyEditor>();
	}


	public AutoAnimationHierarchyEditor(){
		_animationClips = new List<AnimationClip>();
		_tempPathOverrides = new Dictionary<string, string>();
	}
	
	void OnSelectionChange() {
		if (Selection.objects.Length > 1 )
		{
			Debug.Log ("Length? " + Selection.objects.Length);
			_animationClips.Clear();
			foreach ( Object o in Selection.objects )
			{
				if ( o is AnimationClip ) _animationClips.Add((AnimationClip)o);
			}
		}
		else if (Selection.activeObject is AnimationClip) {
			_animationClips.Clear();
			_animationClips.Add((AnimationClip)Selection.activeObject);
			FillModel();
		} else {
			_animationClips.Clear();
		}
		
		Repaint();
	}

	private string _sOriginalRoot = "Root";
	private string _sNewRoot = "SomeNewObject/Root";

private Vector2 _horizScrollPos;

	void OnGUI() {
		if (Event.current.type == EventType.ValidateCommand) {
			switch (Event.current.commandName) {
			case "UndoRedoPerformed":
				FillModel();
				break;
			}
		}
		
		if (_animationClips.Count > 0 ) {
			_scrollPos = GUILayout.BeginScrollView(_scrollPos, GUIStyle.none);
			
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Referenced Animator (Root):", GUILayout.Width(_columnWidth));

			_animatorObject = ((Animator)EditorGUILayout.ObjectField(
				_animatorObject,
				typeof(Animator),
				true,
				GUILayout.Width(_columnWidth))
							  );
			

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Animation Clip:", GUILayout.Width(_columnWidth));

			if ( _animationClips.Count == 1 )
			{
				_animationClips[0] = ((AnimationClip)EditorGUILayout.ObjectField(
					_animationClips[0],
					typeof(AnimationClip),
					true,
					GUILayout.Width(_columnWidth))
								  );
			}		   
			else
			{
				GUILayout.Label("Multiple Anim Clips: " + _animationClips.Count, GUILayout.Width(_columnWidth));
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(20);

			EditorGUILayout.BeginHorizontal();

			_sOriginalRoot = EditorGUILayout.TextField(_sOriginalRoot, GUILayout.Width(_columnWidth));
			_sNewRoot = EditorGUILayout.TextField(_sNewRoot, GUILayout.Width(_columnWidth));
			if (GUILayout.Button("Replace Root")) {
				Debug.Log("O: "+_sOriginalRoot+ " N: "+_sNewRoot);
				ReplaceRoot(_sOriginalRoot, _sNewRoot);
			}

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Reference path:", GUILayout.Width(_columnWidth));
			GUILayout.Label("Animated properties:", GUILayout.Width(_columnWidth*0.5f));
			GUILayout.Label("(Count)", GUILayout.Width(30));
			GUILayout.Label("Object:", GUILayout.Width(_columnWidth));
			EditorGUILayout.EndHorizontal();
			
			if (_paths != null) 
			{

_horizScrollPos = EditorGUILayout.BeginScrollView(_horizScrollPos, GUIStyle.none);

				foreach (string path in _pathsKeys) 
				{
					GUICreatePathItem(path);
				}

EditorGUILayout.EndScrollView();

			}
			
			GUILayout.Space(40);
			GUILayout.EndScrollView();
		} else {
			GUILayout.Label("Please select an Animation Clip");
		}
	}


// object matching: start

// change value to return max n best matches
private List<GameObject> _gameObjectReferences = new List<GameObject>();

// object matching: end


	void GUICreatePathItem(string path) {
		string newPath = path;
		GameObject obj = FindObjectInRoot(path);
		GameObject newObj;
		ArrayList properties = (ArrayList)_paths[path];



		string pathOverride = path;

		if ( _tempPathOverrides.ContainsKey(path) ) pathOverride = _tempPathOverrides[path];
		
		EditorGUILayout.BeginHorizontal();
		
		pathOverride = EditorGUILayout.TextField(pathOverride, GUILayout.Width(_columnWidth));
		if ( pathOverride != path ) _tempPathOverrides[path] = pathOverride;

		if (GUILayout.Button("Change", GUILayout.Width(60))) {
			newPath = pathOverride;
			_tempPathOverrides.Remove(path);
		}
		
		EditorGUILayout.LabelField(
			properties != null ? properties.Count.ToString() : "0",
			GUILayout.Width(30)
			);
		
		Color standardColor = GUI.color;
		
		if (obj != null) {
			GUI.color = Color.green;
		} else {
			GUI.color = Color.red;
		}
		
		newObj = (GameObject)EditorGUILayout.ObjectField(
			obj,
			typeof(GameObject),
			true,
			GUILayout.Width(_columnWidth)
			);
		
		GUI.color = standardColor;
		

// object matching: start

// create suggestions
if (obj == null)
{
		_gameObjectReferences.Clear();

		string[] pathParts = path.Split('/');
		if (pathParts.Length != 0)
		{
			string objname = pathParts[pathParts.Length - 1];

			GameObject[] allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

			foreach (GameObject gg in allGameObjects)
			{
				if (gg.name == objname)
				{
					_gameObjectReferences.Add(gg);
				}
			}

			for (int i = 0; i<_gameObjectReferences.Count; i++)
			{
				if (_gameObjectReferences[i] != null)
				{
					_gameObjectReferences[i] = (GameObject)EditorGUILayout.ObjectField(_gameObjectReferences[i], typeof(GameObject), true, GUILayout.Width(_columnWidth/2f));
					if (GUILayout.Button("Change", GUILayout.Width(60)))
					{
						newObj = _gameObjectReferences[i];
					}
				}
			}
		}
}
// object matching: end

		EditorGUILayout.EndHorizontal();
		
		try {
			if (obj != newObj) {
				UpdatePath(path, ChildPath(newObj));
			}
			
			if (newPath != path) {
				UpdatePath(path, newPath);
			}
		} catch (UnityException ex) {
			Debug.LogError(ex.Message);
		}
	}
	
	void OnInspectorUpdate() {
		Repaint();
	}
	
	void FillModel() {
		_paths = new Hashtable();
		_pathsKeys = new ArrayList();

		foreach ( AnimationClip animationClip in _animationClips )
		{
			FillModelWithCurves(AnimationUtility.GetCurveBindings(animationClip));
			FillModelWithCurves(AnimationUtility.GetObjectReferenceCurveBindings(animationClip));
		}
	}
	
	private void FillModelWithCurves(EditorCurveBinding[] curves) {
		foreach (EditorCurveBinding curveData in curves) {
			string key = curveData.path;
			
			if (_paths.ContainsKey(key)) {
				((ArrayList)_paths[key]).Add(curveData);
			} else {
				ArrayList newProperties = new ArrayList();
				newProperties.Add(curveData);
				_paths.Add(key, newProperties);
				_pathsKeys.Add(key);
			}
		}
	}

	string _sReplacementOldRoot;
	string _sReplacementNewRoot;


	void ReplaceRoot(string oldRoot, string newRoot)
	{
		float fProgress;
		_sReplacementOldRoot = oldRoot;
		_sReplacementNewRoot = newRoot;

		AssetDatabase.StartAssetEditing();
		
		for ( int iCurrentClip = 0; iCurrentClip < _animationClips.Count; iCurrentClip++ )
		{
			AnimationClip animationClip =  _animationClips[iCurrentClip];
			Undo.RecordObject(animationClip, "Animation Hierarchy Root Change");
			
			for ( int iCurrentPath = 0; iCurrentPath < _pathsKeys.Count; iCurrentPath ++)
			{
				string path = _pathsKeys[iCurrentPath] as string;
				if (path != null)
				{
					ArrayList curves = (ArrayList)_paths[path];

					for (int i = 0; i < curves.Count; i++) 
					{
						EditorCurveBinding binding = (EditorCurveBinding)curves[i];

						if ( path.Contains(_sReplacementOldRoot) )
						{
							if ( !path.Contains(_sReplacementNewRoot) )
							{
								string sNewPath = Regex.Replace(path, "^"+_sReplacementOldRoot, _sReplacementNewRoot );												

								AnimationCurve curve = AnimationUtility.GetEditorCurve(animationClip, binding);
								if ( curve != null )
								{
									AnimationUtility.SetEditorCurve(animationClip, binding, null);				
									binding.path = sNewPath;
									AnimationUtility.SetEditorCurve(animationClip, binding, curve);
								}
								else
								{
									ObjectReferenceKeyframe[] objectReferenceCurve = AnimationUtility.GetObjectReferenceCurve(animationClip, binding);
									AnimationUtility.SetObjectReferenceCurve(animationClip, binding, null);
									binding.path = sNewPath;
									AnimationUtility.SetObjectReferenceCurve(animationClip, binding, objectReferenceCurve);
								}
							}
						}
					}
				}

				// Update the progress meter
				float fChunk = 1f / _animationClips.Count;
				fProgress = (iCurrentClip * fChunk) + fChunk * (iCurrentPath / (float) _pathsKeys.Count);
				
				EditorUtility.DisplayProgressBar(
					"Animation Hierarchy Progress", 
					"How far along the animation editing has progressed.",
					fProgress);
			}

		}
		AssetDatabase.StopAssetEditing();
		EditorUtility.ClearProgressBar();
		
		FillModel();
		Repaint();
	}
	
	void UpdatePath(string oldPath, string newPath) 
	{
		if (_paths[newPath] != null) {
			throw new UnityException("Path " + newPath + " already exists in that animation!");
		}
		AssetDatabase.StartAssetEditing();
		for ( int iCurrentClip = 0; iCurrentClip < _animationClips.Count; iCurrentClip++ )
		{
			AnimationClip animationClip =  _animationClips[iCurrentClip];
			Undo.RecordObject(animationClip, "Animation Hierarchy Change");
			
			//recreating all curves one by one
			//to maintain proper order in the editor - 
			//slower than just removing old curve
			//and adding a corrected one, but it's more
			//user-friendly
			for ( int iCurrentPath = 0; iCurrentPath < _pathsKeys.Count; iCurrentPath ++)
			{
				string path = _pathsKeys[iCurrentPath] as string;
				ArrayList curves = (ArrayList)_paths[path];
				
				for (int i = 0; i < curves.Count; i++) {
					EditorCurveBinding binding = (EditorCurveBinding)curves[i];
					AnimationCurve curve = AnimationUtility.GetEditorCurve(animationClip, binding);
					ObjectReferenceKeyframe[] objectReferenceCurve = AnimationUtility.GetObjectReferenceCurve(animationClip, binding);


						if ( curve != null )
							AnimationUtility.SetEditorCurve(animationClip, binding, null);
						else
							AnimationUtility.SetObjectReferenceCurve(animationClip, binding, null);

						if (path == oldPath) 
							binding.path = newPath;

						if ( curve != null )
							AnimationUtility.SetEditorCurve(animationClip, binding, curve);
						else
							AnimationUtility.SetObjectReferenceCurve(animationClip, binding, objectReferenceCurve);

					float fChunk = 1f / _animationClips.Count;
					float fProgress = (iCurrentClip * fChunk) + fChunk * (iCurrentPath / (float) _pathsKeys.Count);				
					
					EditorUtility.DisplayProgressBar(
						"Animation Hierarchy Progress", 
						"How far along the animation editing has progressed.",
						fProgress);
				}
			}
		}
		AssetDatabase.StopAssetEditing();
		EditorUtility.ClearProgressBar();
		FillModel();
		Repaint();
	}
	
	GameObject FindObjectInRoot(string path) {
		if (_animatorObject == null) {
			return null;
		}
		
		Transform child = _animatorObject.transform.Find(path);
		
		if (child != null) {
			return child.gameObject;
		}

		return null;
	}
	
	string ChildPath(GameObject obj, bool sep = false) {
		if (_animatorObject == null) {
			throw new UnityException("Please assign Referenced Animator (Root) first!");
		}
		
		if (obj == _animatorObject.gameObject) {
			return "";
		}

		if (obj.transform.parent == null) {
			throw new UnityException("Object must belong to " + _animatorObject + "!");
		}

		return ChildPath(obj.transform.parent.gameObject, true) + obj.name + (sep ? "/" : "");
	}
}

#endif
