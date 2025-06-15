using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
    public class ThirdPartySupportPage : WizardPage
    {
        const string PACKAGE_PATH_TMP_OLD = "TheraBytes/BetterUI/packages/BetterUI_TextMeshPro_UiEditorPanel.unitypackage";
        const string PACKAGE_PATH_TMP_NEW = "TheraBytes/BetterUI/packages/BetterUI_TextMeshPro_EditorPanelUI.unitypackage";
        const string TMP_KEY = "TextMeshPro";

        public override string NameId { get { return "ThirdPartySupportPage"; } }

        public ThirdPartySupportPage(IWizard wizard)
            : base(wizard)
        {
        }

        protected override void OnInitialize()
        {
            Add(new InfoWizardPageElement("Additional Packages", InfoType.Header));
            InitTextMeshProSection();
            InitRewiredSection();
        }

        private void InitRewiredSection()
        {
            Add(new SeparatorWizardPageElement());
            Add(new InfoWizardPageElement("Rewired", InfoType.Header));
#if REWIRED
            Add(new InfoWizardPageElement(@"Rewired support is installed."));
            Add(new InfoWizardPageElement(@"If you want to uninstall it, remove the Scripting Define Symbol ""REWIRED"" in the Player Settings manually (Unity doesn't have an API to do this for you)."));

            Add(new InfoWizardPageElement(@"If you added new build targets, you need to add the ""REWIRED"" symbol there as well."));
            Add(new CustomWizardPageElement((o) =>
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(@"Open Player Settings"))
                    {
                        SettingsService.OpenProjectSettings("Project/Player");
                    }

                    if (GUILayout.Button(@"Add ""REWIRED"" symbol again"))
                    {
                        AddSymbol("REWIRED");
                    }

                    EditorGUILayout.EndHorizontal();
            }).MarkComplete());
#else
            bool rewiredDetected = AppDomain.CurrentDomain.GetAssemblies().Any(o => o.FullName.Contains("Rewired_Core"));
            if (rewiredDetected)
            {
                Add(new InfoWizardPageElement(@"Rewired is detected."));
                Add(new InfoWizardPageElement(@"You can add ""REWIRED"" symbol to enable rewired support in Better UI."));

                Add(new CustomWizardPageElement((o) =>
                    {
                        if (GUILayout.Button(@"Add ""REWIRED"" symbol"))
                        {
                            wizard.DoReloadOperation(this, () =>
                            {
                                AddSymbol("REWIRED");
                            });
                        }
                    }).MarkComplete());
            }
            else
            {
                Add(new InfoWizardPageElement(@"Rewired was not detected."));
                Add(new InfoWizardPageElement(@"If you are sure that Rewired is in the project, you can add the Scripting Define Symbol ""REWIRED"" in the Player Settings to enable rewired support in Better UI."));

                Add(new CustomWizardPageElement((o) =>
                {
                    if (GUILayout.Button(@"Open Player Settings"))
                    {
                        SettingsService.OpenProjectSettings("Project/Player");
                    }
                }).MarkComplete());
            }
#endif
        }

        private void AddSymbol(string symbol)
        {
            var targets = Enum.GetValues(typeof(BuildTarget)).Cast<BuildTarget>();
            var groups = Enum.GetValues(typeof(BuildTargetGroup)).Cast<BuildTargetGroup>();

            List<BuildTargetGroup> processedGroups = new List<BuildTargetGroup>();

            foreach (var target in targets)
            {
                var group = BuildPipeline.GetBuildTargetGroup(target);
                if (!BuildPipeline.IsBuildTargetSupported(group, target))
                    continue;

                if (processedGroups.Contains(group))
                    continue;
#if UNITY_2023_1_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group), symbol);
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, symbol);
#endif
                processedGroups.Add(group);
            }
        }

        void InitTextMeshProSection()
        {
            // TextMesh Pro
            Add(new SeparatorWizardPageElement());
            Add(new InfoWizardPageElement("TextMeth Pro", InfoType.Header));

            if (IsBetterTextMeshProInstalled(out var tmpAddOnPath))
            {
                Add(new InfoWizardPageElement("TextMesh Pro add-on is already installed."));

                Add(new ValueWizardPageElement<string>(TMP_KEY,
                    (o, v) =>
                    {
                        if(GUILayout.Button("Remove TextMesh Pro add-on"))
                        {
                            if(EditorUtility.DisplayDialog("Remove TextMesh Pro add-on?",
                                "Are you sure you want to remove Better UI support for TextMesh Pro? (some UIs could break)",
                                "Remove it!", "Cancel"))
                            {
                                wizard.DoReloadOperation(this, () =>
                                {
                                    try
                                    {
                                        System.IO.File.Delete(tmpAddOnPath + ".meta");
                                        System.IO.Directory.Delete(tmpAddOnPath, true);
                                    }
                                    catch
                                    {
                                        Debug.LogError("Could not properly delete " + tmpAddOnPath);
                                    }
                                    AssetDatabase.Refresh();
                                    v = null;
                                });
                            }
                        }

                        return v;
                    }).MarkComplete());
            }
            else
            {
                int v1, v2, v3;
                bool foundTmpVersion = TryGetTextMethProPackageVersion(out v1, out v2, out v3);
                if (foundTmpVersion)
                {
                    string versionString = string.Format("{0}.{1}.{2}", v1, v2, v3);
                    Add(new InfoWizardPageElement(string.Format("TextMesh pro version {0} or above detected.", versionString)));
                    if (v1 > 3 || (v1 == 3 && v2 > 0) || (v1 == 3 && v2 == 0 && v3 > 6))
                    {
                        Add(new InfoWizardPageElement("The latest tested TextMesh Pro version is 3.0.6.\n" +
                            "It is very likely that the Better UI add-on for TextMesh Pro will also work with version " + versionString +
                            " but it cannot be guaranteed. If it doesnt work with this version, please write a mail to info@therabytes.de.",
                            InfoType.WarningBox));
                    }

                    if (v1 < 1 || (v1 == 1 && v2 == 0 && v3 < 54))
                    {
                        Add(new InfoWizardPageElement("Your version of TextMesh Pro is too old and not supported. Please upgrade at least to version 1.0.54.",
                            InfoType.ErrorBox));
                    }
                    else
                    {
                        const string assertionText = "You have a TextMesh Pro package installed which is not supported in your version of Unity. You should upgrade TextMesh Pro.";
                        bool isNewBaseClass = 
#if UNITY_2020_1_OR_NEWER
                            (v1 >= 3); // should always be the case
                            Debug.Assert(v1 >= 3, assertionText);
#elif UNITY_2019_4_OR_NEWER
                            (v1 == 2 && v2 >= 1) || (v1 > 2);
                            Debug.Assert(v1 == 2, assertionText);
#else
                            (v1 == 1 && v2 >= 5) || (v1 > 1);
                            Debug.Assert(v1 == 1, assertionText);
#endif

                        string packageName = (isNewBaseClass)
                            ? PACKAGE_PATH_TMP_NEW
                            : PACKAGE_PATH_TMP_OLD;

                        Add(new ValueWizardPageElement<string>(TMP_KEY,
                            (o, v) =>
                            {
                                if (GUILayout.Button("Import TextMesh Pro Add-On"))
                                {
                                    wizard.DoReloadOperation(this, () =>
                                    {
                                        AssetDatabase.ImportPackage(System.IO.Path.Combine(Application.dataPath, packageName), false);
                                        v = packageName;
                                    });
                                }
                                return v;
                            }).MarkComplete());
                    }
                }
                else
                {
                    Add(new InfoWizardPageElement("TextMesh Pro could not be detected."));
                    Add(new InfoWizardPageElement("You may install the right Better UI add-on now " +
                        "but if you don't have TextMesh Pro installed in your project, " +
                        "you will face compile errors until TextMesh Pro is installed or the add on folder is deleted again.",
                        InfoType.WarningBox));


                    Add(new InfoWizardPageElement("Please select the add on for the text mesh pro version you have installed."));

#if UNITY_2020_1_OR_NEWER
                    string textNewVersion = "v3.0 or above";
                    string textOldVersion = "below v3.0";
#elif UNITY_2019_4_OR_NEWER
                    string textNewVersion = "v2.1 or above";
                    string textOldVersion = "below v2.1";
#else
                    string textNewVersion = "v1.5 or above";
                    string textOldVersion = "below v1.5 and above v1.0.54";
#endif
                    Add(new ValueWizardPageElement<string>(TMP_KEY,
                           (o, v) =>
                           {
                               if (GUILayout.Button("Import Add-On (TextMesh Pro " + textNewVersion + ")"))
                               {
                                   wizard.DoReloadOperation(this, () =>
                                    {
                                        AssetDatabase.ImportPackage(System.IO.Path.Combine(Application.dataPath, PACKAGE_PATH_TMP_NEW), false);
                                        v = PACKAGE_PATH_TMP_NEW;
                                    });
                               }

                               if (GUILayout.Button("Import Add-On (TextMesh Pro " + textOldVersion + ")"))
                               {
                                   wizard.DoReloadOperation(this, () =>
                                   {
                                       AssetDatabase.ImportPackage(System.IO.Path.Combine(Application.dataPath, PACKAGE_PATH_TMP_OLD), false);
                                       v = PACKAGE_PATH_TMP_OLD;
                                   });
                               }

                               return v;
                           }).MarkComplete());
                }
            }
        }

        private bool TryGetTextMethProPackageVersion(out int v1, out int v2, out int v3)
        {
            v1 = 0;
            v2 = 0;
            v3 = 0;

#if UNITY_6000_0_OR_NEWER // TextMesh Pro is deeply integrated in Unity 6
            v1 = 3;
            return true;
#endif

            string folder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "Packages");
            if (!System.IO.Directory.Exists(folder))
                return false;

            string file = System.IO.Path.Combine(folder, "manifest.json");
            if (!System.IO.File.Exists(file))
                return false;

            string json = System.IO.File.ReadAllText(file);
            var match = Regex.Match(json, @"""com.unity.textmeshpro"":\s?""(?<v1>\d *).(?<v2>\d *).(?<v3>\d *)""");
            if (!match.Success)
                return false;

            try
            {
                return int.TryParse(match.Groups["v1"].Value, out v1)
                    && int.TryParse(match.Groups["v2"].Value, out v2)
                    && int.TryParse(match.Groups["v3"].Value, out v3);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsBetterTextMeshProInstalled(out string path)
        {
            path = System.IO.Path.Combine(Application.dataPath, "TheraBytes/BetterUI_TextMeshPro");
            return (System.IO.Directory.Exists(path));
        }
    }
}
