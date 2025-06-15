using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

/// <summary>
/// Utility menus to easily create our builds for our playtests. If you're just exploring this project, you shouldn't need those. They are mostly to make
/// multiplatform build creation easier and is meant for internal usage.
/// </summary>
internal static class BuildHelpers
{
    const string KMenuRoot = "Boss Room/Playtest Builds/";
    const string KBuild = KMenuRoot + "Build";
    const string KDeleteBuilds = KMenuRoot + "Delete All Builds (keeps cache)";
    const string KAllToggleName = KMenuRoot + "Toggle All";
    const string KMobileToggleName = KMenuRoot + "Toggle Mobile";
    const string KiosToggleName = KMenuRoot + "Toggle iOS";
    const string KAndroidToggleName = KMenuRoot + "Toggle Android";
    const string KDesktopToggleName = KMenuRoot + "Toggle Desktop";
    const string KMacOSToggleName = KMenuRoot + "Toggle MacOS";
    const string KWindowsToggleName = KMenuRoot + "Toggle Windows";
    const string KDisableProjectIDToggleName = KMenuRoot + "Skip Project ID Check"; // double negative in the name since menu is unchecked by default
    const string KSkipAutoDeleteToggleName = KMenuRoot + "Skip Auto Delete Builds";

    const int KMenuGroupingBuild = 0; // to add separator in menus
    const int KMenuGroupingPlatforms = 11;
    const int KMenuGroupingOtherToggles = 22;

    static BuildTarget _sCurrentEditorBuildTarget;
    static BuildTargetGroup _sCurrentEditorBuildTargetGroup;
    static int _sNbBuildsDone;

    static string BuildPathRootDirectory => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", "Playtest");
    static string BuildPathDirectory(string platformName) => Path.Combine(BuildPathRootDirectory, platformName);
    public static string BuildPath(string platformName) => Path.Combine(BuildPathDirectory(platformName), "BossRoomPlaytest");

    [MenuItem(KBuild, false, KMenuGroupingBuild)]
    static async void Build()
    {
        _sNbBuildsDone = 0;
        bool buildiOS = Menu.GetChecked(KiosToggleName);
        bool buildAndroid = Menu.GetChecked(KAndroidToggleName);
        bool buildMacOS = Menu.GetChecked(KMacOSToggleName);
        bool buildWindows = Menu.GetChecked(KWindowsToggleName);

        bool skipAutoDelete = Menu.GetChecked(KSkipAutoDeleteToggleName);

        Debug.Log($"Starting build: buildiOS?:{buildiOS} buildAndroid?:{buildAndroid} buildMacOS?:{buildMacOS} buildWindows?:{buildWindows}");
        if (string.IsNullOrEmpty(CloudProjectSettings.projectId) && !Menu.GetChecked(KDisableProjectIDToggleName))
        {
            string errorMessage = $"Project ID was supposed to be setup and wasn't, make sure to set it up or disable project ID check with the [{KDisableProjectIDToggleName}] menu";
            EditorUtility.DisplayDialog("Error Custom Build", errorMessage, "ok");
            throw new Exception(errorMessage);
        }

        SaveCurrentBuildTarget();

        try
        {
            // deleting so we don't end up testing on outdated builds if there's a build failure
            if (!skipAutoDelete) DeleteBuilds();

            if (buildiOS) await BuildPlayerUtilityAsync(BuildTarget.iOS, "", true);
            if (buildAndroid) await BuildPlayerUtilityAsync(BuildTarget.Android, ".apk", true); // there's the possibility of an error where it

            // complains about NDK missing. Building manually on android then trying again seems to work? Can't find anything on this.
            if (buildMacOS) await BuildPlayerUtilityAsync(BuildTarget.StandaloneOSX, ".app", true);
            if (buildWindows) await BuildPlayerUtilityAsync(BuildTarget.StandaloneWindows64, ".exe", true);
        }
        catch
        {
            EditorUtility.DisplayDialog("Exception while building", "See console for details", "ok");
            throw;
        }
        finally
        {
            Debug.Log($"Count builds done: {_sNbBuildsDone}");
            RestoreBuildTarget();
        }
    }

    [MenuItem(KBuild, true)]
    static bool CanBuild()
    {
        return Menu.GetChecked(KiosToggleName) ||
            Menu.GetChecked(KAndroidToggleName) ||
            Menu.GetChecked(KMacOSToggleName) ||
            Menu.GetChecked(KWindowsToggleName);
    }

    static void RestoreBuildTarget()
    {
        Debug.Log($"restoring editor to initial build target {_sCurrentEditorBuildTarget}");
        EditorUserBuildSettings.SwitchActiveBuildTarget(_sCurrentEditorBuildTargetGroup, _sCurrentEditorBuildTarget);
    }

    static void SaveCurrentBuildTarget()
    {
        _sCurrentEditorBuildTarget = EditorUserBuildSettings.activeBuildTarget;
        _sCurrentEditorBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
    }

    [MenuItem(KAllToggleName, false, KMenuGroupingPlatforms)]
    static void ToggleAll()
    {
        var newValue = ToggleMenu(KAllToggleName);
        ToggleMenu(KDesktopToggleName, newValue);
        ToggleMenu(KMacOSToggleName, newValue);
        ToggleMenu(KWindowsToggleName, newValue);
        ToggleMenu(KMobileToggleName, newValue);
        ToggleMenu(KiosToggleName, newValue);
        ToggleMenu(KAndroidToggleName, newValue);
    }

    [MenuItem(KMobileToggleName, false, KMenuGroupingPlatforms)]
    static void ToggleMobile()
    {
        var newValue = ToggleMenu(KMobileToggleName);
        ToggleMenu(KiosToggleName, newValue);
        ToggleMenu(KAndroidToggleName, newValue);
    }

    [MenuItem(KiosToggleName, false, KMenuGroupingPlatforms)]
    static void ToggleiOS()
    {
        ToggleMenu(KiosToggleName);
    }

    [MenuItem(KAndroidToggleName, false, KMenuGroupingPlatforms)]
    static void ToggleAndroid()
    {
        ToggleMenu(KAndroidToggleName);
    }

    [MenuItem(KDesktopToggleName, false, KMenuGroupingPlatforms)]
    static void ToggleDesktop()
    {
        var newValue = ToggleMenu(KDesktopToggleName);
        ToggleMenu(KMacOSToggleName, newValue);
        ToggleMenu(KWindowsToggleName, newValue);
    }

    [MenuItem(KMacOSToggleName, false, KMenuGroupingPlatforms)]
    static void ToggleMacOS()
    {
        ToggleMenu(KMacOSToggleName);
    }

    [MenuItem(KWindowsToggleName, false, KMenuGroupingPlatforms)]
    static void ToggleWindows()
    {
        ToggleMenu(KWindowsToggleName);
    }

    [MenuItem(KDisableProjectIDToggleName, false, KMenuGroupingOtherToggles)]
    static void ToggleProjectID()
    {
        ToggleMenu(KDisableProjectIDToggleName);
    }

    [MenuItem(KSkipAutoDeleteToggleName, false, KMenuGroupingOtherToggles)]
    static void ToggleAutoDelete()
    {
        ToggleMenu(KSkipAutoDeleteToggleName);
    }

    static bool ToggleMenu(string menuName, bool? valueToSet = null)
    {
        bool toSet = !Menu.GetChecked(menuName);
        if (valueToSet != null)
        {
            toSet = valueToSet.Value;
        }

        Menu.SetChecked(menuName, toSet);
        return toSet;
    }

    static async Task BuildPlayerUtilityAsync(BuildTarget buildTarget = BuildTarget.NoTarget, string buildPathExtension = null, bool buildDebug = false)
    {
        _sNbBuildsDone++;
        Debug.Log($"Starting build for {buildTarget.ToString()}");

        await Task.Delay(100); // skipping some time to make sure debug logs are flushed before we build

        var buildPathToUse = BuildPath(buildTarget.ToString());
        buildPathToUse += buildPathExtension;

        var buildPlayerOptions = new BuildPlayerOptions();

        List<string> scenesToInclude = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenesToInclude.Add(scene.path);
            }
        }

        buildPlayerOptions.scenes = scenesToInclude.ToArray();
        buildPlayerOptions.locationPathName = buildPathToUse;
        buildPlayerOptions.target = buildTarget;
        var buildOptions = BuildOptions.None;
        if (buildDebug)
        {
            buildOptions |= BuildOptions.Development;
        }

        buildOptions |= BuildOptions.StrictMode;
        buildPlayerOptions.options = buildOptions;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {summary.totalSize} bytes at {summary.outputPath}");
        }
        else
        {
            string debugString = buildDebug ? "debug" : "release";
            throw new Exception($"Build failed for {debugString}:{buildTarget}! {report.summary.totalErrors} errors");
        }
    }

    [MenuItem(KDeleteBuilds, false, KMenuGroupingBuild)]
    public static void DeleteBuilds()
    {
        if (Directory.Exists(BuildPathRootDirectory))
        {
            Directory.Delete(BuildPathRootDirectory, recursive: true);
            Debug.Log($"deleted {BuildPathRootDirectory}");
        }
        else
        {
            Debug.Log($"Build directory does not exist ({BuildPathRootDirectory}). No cleanup to do");
        }
    }
}
