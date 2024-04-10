using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    public static void PerformBuild()
    {
        BuildPlayerOptions options = new();
        options.target = BuildTarget.StandaloneWindows;
        options.locationPathName = "C:/Users/mulle/Desktop/Servers/Webserver/DriftBrothers/DriftBrothers.exe";
        options.options = BuildOptions.None;
        string[] scenes = new string[EditorBuildSettings.scenes.Length];
        for(int i = 0; i < scenes.Length; i++) {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }
        options.scenes = scenes;
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if(summary.result != BuildResult.Succeeded)
            Debug.LogError("Build didn't succeed. Result: " + summary.result);
    }
}
