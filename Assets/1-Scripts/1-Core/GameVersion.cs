using UnityEditor;
using UnityEngine;

/// <summary>
/// Provides tools for interpreting version
/// </summary>
public class GameVersion
{

    public static int major = 0;
    public static int minor = 4;
    public static int revision = 0;
    public static ReleaseType releaseType = ReleaseType.DEVELOPMENT;

    public static string Version { get {
        return $"{major}.{minor}.{revision}" + (releaseType == ReleaseType.DEVELOPMENT ? "dev" : "");
    } }

    public static bool IsDevelopment { get {
        return releaseType == ReleaseType.DEVELOPMENT;
    } }
}

public enum ReleaseType {
    RELEASE, DEVELOPMENT
}