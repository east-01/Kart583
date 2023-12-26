using System;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.SceneManagement;

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