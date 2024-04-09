using UnityEditor;
using UnityEngine;

public class BuildScript
{
    public static void PerformBuild(string target)
    {
        BuildTarget buildTarget;

        switch (target)
        {
            case "-windows":
                buildTarget = BuildTarget.StandaloneWindows;
                break;
            case "-macos":
                buildTarget = BuildTarget.StandaloneOSX;
                break;
            case "-linux":
                buildTarget = BuildTarget.StandaloneLinux64;
                break;
            case "-android":
                buildTarget = BuildTarget.Android;
                break;
            case "-ios":
                buildTarget = BuildTarget.iOS;
                break;
            case "-webgl":
                buildTarget = BuildTarget.WebGL;
                break;
            default:
                UnityEngine.Debug.LogError("Invalid build target specified.");
                return;
        }

        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, $"{Application.dataPath}/../Builds", buildTarget, BuildOptions.None);
    }
}
