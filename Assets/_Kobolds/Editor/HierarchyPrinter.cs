using System.Text;
using UnityEditor;
using UnityEngine;

public class HierarchyPrinter : EditorWindow
{
    [MenuItem("Tools/Print Hierarchy")]
    public static void ShowWindow()
    {
        GetWindow<HierarchyPrinter>("Hierarchy Printer");
    }

    private GameObject _targetObject;

	private void OnGUI()
	{
		GUILayout.Label("Hierarchy Printer", EditorStyles.boldLabel);

		_targetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", _targetObject, typeof(GameObject), true);

		if (GUILayout.Button("Print Hierarchy"))
		{
			if (_targetObject != null)
			{
				StringBuilder hierarchyOutput = new StringBuilder();
				hierarchyOutput.AppendLine($"Hierarchy of {_targetObject.name}:");
				BuildHierarchyString(_targetObject.transform, 0, hierarchyOutput);
				Debug.Log(hierarchyOutput.ToString());
			}
			else
			{
				Debug.LogWarning("Please assign a target GameObject.");
			}
		}
	}

	private void BuildHierarchyString(Transform parent, int depth, StringBuilder sb)
	{
		// Indent based on depth
		string indent = new string(' ', depth * 2);
		sb.AppendLine($"{indent}- {parent.name}");

		// Recursively build hierarchy for children
		foreach (Transform child in parent)
		{
			BuildHierarchyString(child, depth + 1, sb);
		}
	}
}
