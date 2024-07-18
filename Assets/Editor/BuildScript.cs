using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    public static void PerformBuild(string[] args = null)
    {
        if(args == null) 
            args = System.Environment.GetCommandLineArgs();

        string targetLocation = null;
        for(int i = 0; i < args.Length; i++) {
            if(args[i] == "BuildScript.PerformBuild" && i < args.Length-1)
                targetLocation = args[i+1];
        }

        Debug.Log(targetLocation);
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

        VersionPacket vp = new() {
            build_succeeded = summary.result == BuildResult.Succeeded,
            build_message = summary.result.ToString(),
            version = DevSettings.GetVersionString(),
            changelog = "None"
        };

        string json = JsonConvert.SerializeObject(vp);
        string[] locationArr = targetLocation.Split("\\");
        string parentFolder = "";
        for(int i = 0; i < locationArr.Length-2; i++) { parentFolder += locationArr[i] + "\\"; }
        Debug.Log($"target loc: {targetLocation} parentFolder: {parentFolder}");
        File.WriteAllText(parentFolder + "versionpacket.json", json);

    }
}

public class BuildScriptWindow : EditorWindow
{
    [MenuItem("Window/Build Script")]
    public static void ShowWindow() 
    {
        GetWindow<BuildScriptWindow>("Build Script");
    }

    private void OnGUI() 
    {
        if(GUILayout.Button("Build")) {
            BuildScript.PerformBuild(new string[] {
                "C:\\Program Files\\Unity\\Hub\\Editor\\2021.3.17f1\\Editor\\Unity.exe",
                "-projectpath",
                "C:\\Users\\mulle\\Kart583",
                "-useHub",
                "-hubIPC",
                "-cloudEnvironment",
                "production",
                "-licensingIpc",
                "LicenseClient-mulle",
                "-hubSessionId",
                "e855f55c-2fa5-4c40-989e-b155c3331fbf",
                "-accessToken",
                "nFJ_KQINP9_TFIY61dmuL26ICVv5EZ0GNdx8AivtfeQ001f",
                "Files\"\\Unity\\Hub\\Editor\\2021.3.17f1\\Editor\\Unity.exe",
                "-projectPath",
                "C:\\Users\\mulle\\Kart583\\",
                "-logFile",
                "C:\\Users\\mulle\\Kart583\\Builds\\unity_build_log",
                "-executeMethod",
                "BuildScript.PerformBuild",
                "C:\\Users\\mulle\\Kart583\\Builds\\Latest\\DriftBrothers.exe",
                "-quit"
            });
        }
    }
}

public struct VersionPacket 
{
    public bool build_succeeded;
    public string build_message;
    public string version;
    public string changelog;
}
