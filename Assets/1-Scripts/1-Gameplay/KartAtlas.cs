using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KartAtlas : MonoBehaviour
{

    [Header("IMPORTANT NOTE: Match enum index to list index")] public List<KartDataPackage> Karts;
    public KartDataPackage RetrieveData(KartName kartName) 
    {
        return Karts[(int)kartName];
    }

}

public enum KartName {
    STANDARD, SPEED
}

[Serializable]
public struct KartDataPackage 
{
    public string name;
    public KartModel model;
    public KartSettings settings;
}
