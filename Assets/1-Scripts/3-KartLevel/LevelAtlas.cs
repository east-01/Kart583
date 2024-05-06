using System;
using System.Collections.Generic;
using UnityEngine;

/** The Level atlas will hold all Level data for each Level enum. Properties are filled
  *   in the GameplayManager of the scene, where this LevelAtlas script should belong. */
public class LevelAtlas : MonoBehaviour
{
    /** It's very important that we match the enum index to the list index so that RetrieveData() is fast. */
    [Header("IMPORTANT NOTE: Match enum index to list index")] public List<LevelDataPackage> Levels;
    public LevelDataPackage RetrieveData(KartLevel Level) 
    {
        return Levels[(int)Level];
    }

    public KartLevel SearchEnumBySceneName(string sceneName) 
    {
        foreach(KartLevel level in Enum.GetValues(typeof(KartLevel))) {
            if(RetrieveData(level).sceneName == sceneName)
                return level;
        }
        Debug.LogError($"Failed to find KartLevel from scene name \"{sceneName}\"");
        return default;
    }

    public static KartLevel PickRandomLevel() 
    {
        Array values = Enum.GetValues(typeof(KartLevel));
        return (KartLevel)values.GetValue(new System.Random().Next(values.Length));
    }
}

[Serializable]
public struct LevelDataPackage
{
    public KartLevel Level;
    public String sceneName;
    public String levelString;
    public Sprite levelImage;
    public int lapCount;
}