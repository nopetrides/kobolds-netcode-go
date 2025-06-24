using Kobolds.UI;
using UnityEngine;

[CreateAssetMenu(fileName = "MenusList", menuName = "Scriptable Objects/MenusList")]
public class MenusList : ScriptableObject
{
	[SerializeField] private LanguageSelectMenu _languageSelectMenuPrefab;
}
