using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    public static void PerformBuild()
    {

        string[] args = System.Environment.GetCommandLineArgs();
        string targetLocation = null;
        for(int i = 0; i < args.Length; i++) {
            if(args[i] == "BuildScript.PerformBuild" && i < args.Length-1)
                targetLocation = args[i+1];
        }

        if(targetLocation == null) {
            Debug.LogError("Failed to find target location from command line arguments.");
            return;
        }

        BuildPlayerOptions options = new();
        options.target = BuildTarget.StandaloneWindows;
        options.locationPathName = targetLocation;
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
