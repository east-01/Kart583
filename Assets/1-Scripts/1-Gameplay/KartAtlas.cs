using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class KartAtlas : MonoBehaviour
{

    private KartSettings? _highestStats;
    /** Constructs an artificial KartSettings struct using the highest values for each statistic.
      * Used in KartSelectController stats menu. */
    public KartSettings HighestStats {
        get { 
            if(_highestStats == null) {
                _highestStats = new();
                foreach(KartName name in Enum.GetValues(typeof(KartName))) {
                    KartSettings settings = RetrieveData(name).settings;
                    foreach(FieldInfo fi in typeof(KartSettings).GetFields()) {
                        if(fi.FieldType != typeof(float)) {
                            Debug.Log("Field \"" + fi.Name + "\" in KartSettings struct isn't a float. Update KartAtlas#RetrieveHighestStats() to handle it.");
                            continue;
                        }
                        object boxedHS = _highestStats; // Dumb. See https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing
                        if((float)fi.GetValue(settings) > (float)fi.GetValue(_highestStats)) {
                            fi.SetValue(boxedHS, fi.GetValue(settings));
                        }
                        _highestStats = (KartSettings)boxedHS;
                    }
                }
            }
            return _highestStats.Value;
        }
    }
    
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
    public Sprite image;
    public KartSettings settings;
}
