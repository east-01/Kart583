using UnityEditor;
using UnityEngine;

public class BuildScript
{
    public static void PerformBuild()
    {
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, $"{Application.dataPath}/../Builds", BuildTarget.StandaloneWindows, BuildOptions.None);
    }
}
