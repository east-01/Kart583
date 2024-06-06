using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FishNet;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class DevSettings
{

    public static readonly string FILE_PATH = "./DeveloperSettings.json";

#region Game version fields
    public int major, minor, revision;
    public ReleaseType releaseType;
    public static string GetVersionString() { return $"{Settings.major}.{Settings.minor}.{Settings.revision}" + (Settings.releaseType == ReleaseType.DEVELOPMENT ? "dev" : ""); }
    public static bool IsDevelopment() { return Settings.releaseType == ReleaseType.DEVELOPMENT; }
#endregion

#region Developer settings fields
    /* 
    Editor fields 
    About organization: Fields are organized in the order which they appear in the DevSettingsWindow, starting with the
      private field and then the public accessor for it. The public accessor should take into consideration whether or
      not the master enable (or other parent booleans) are enabled.
    */
#region Master Enable
    private bool masterEnable;
    public bool Enable { 
        get { return masterEnable && releaseType == ReleaseType.DEVELOPMENT; } 
        set { masterEnable = value; }
    }
#endregion

#region Player limit
    private bool overridePlayerLimit;
    public bool OverridePlayerLimit { 
        get { return Enable && overridePlayerLimit; } 
        set { overridePlayerLimit = value;}
    }
    private int playerLimit;
    public int PlayerLimit {
        get { return playerLimit; }
        set { playerLimit = value;}
    }
#endregion

#region Load Mode
    private LoadMode loadMode;
    public LoadMode LoadMode { 
        get { return Enable ? loadMode : LoadMode.NONE; } 
        set { loadMode = value; }
    }
#endregion

#region Map pick
    private bool overrideMapPick;
    public bool OverrideMapPick { 
        get { return Enable && overrideMapPick; } 
        set { overrideMapPick = value;}
    }
    private KartLevel map;
    public KartLevel Map {  
        get { return map; }
        set { map = value; }
    }
#endregion

#region Server settings
    private bool haveStandalonePlayerRunAsServer;
    public bool HaveStandalonePlayerRunAsServer { 
        get { 
            bool commandLineStates = false;
            foreach(string arg in System.Environment.GetCommandLineArgs()) { 
                if(arg.Equals("-runAsServer")) {
                    commandLineStates = true;
                    break;
                }
            }
            return commandLineStates || (Enable && haveStandalonePlayerRunAsServer); 
        } 
        set { haveStandalonePlayerRunAsServer = value; }    
    }
    private bool manualLobbyPlayerWaitSwitch;
    public bool ManualLobbyPlayerWaitSwitch { 
        get { return Enable && manualLobbyPlayerWaitSwitch; } 
        set { manualLobbyPlayerWaitSwitch = value;}
    }
#endregion

#region Race progress
    private bool overrideRaceProgressAtStart;
    public bool OverrideRaceProgressAtStart { 
        get { return Enable && overrideRaceProgressAtStart; } 
        set { overrideRaceProgressAtStart = value; }
    }
    private float raceProgress;
    public float RaceProgress {
        get { return raceProgress; }
        set { raceProgress = value;}
    }
#endregion

#region Race settings
    private bool overrideLapCount;
    public bool OverrideLapCount { 
        get { return Enable && overrideLapCount; } 
        set { overrideLapCount = value; } 
    }
    private int lapCount;
    public int LapCount {
        get { return lapCount; }
        set { lapCount = value;}
    }
    private bool overrideBots;
    public bool OverrideBots { 
        get { return Enable && overrideBots; } 
        set { overrideBots = value;}
    }
    private bool bots;
    public bool Bots {
        get { return bots; }
        set { bots = value;}
    }
#endregion
#endregion

    /* Private fields */
    private KartLevel queuedMapLoad;
    public bool hasProcessedLoadMode = false;    

    private static DevSettings settings;
    public static DevSettings Settings { 
        get {
            if(settings == null)
                LoadSettings();
            if(settings == null)
                Debug.LogError("Failed to load DeveloperSettings");

            // Checks
            if(settings.OverridePlayerLimit && settings.PlayerLimit <= 0)
                Debug.LogWarning($"DevSettings: Max players is being overridden but the new value is <= 0, this is not recommended.");
            if(settings.OverrideLapCount && settings.LapCount <= 0)
                Debug.LogWarning($"DevSettings: Lap count is being overridden but the new value is <= 0, this is not recommended.");

            return settings;
        } 
        // set { settings = value; }
    }


    public static string Serialize() { return JsonConvert.SerializeObject(settings, Formatting.Indented, new JsonSerializerSettings{PreserveReferencesHandling = PreserveReferencesHandling.Objects}); }
    public static void SaveSettings(string serializedDeveloperSettings) { File.WriteAllText(FILE_PATH, serializedDeveloperSettings); }
    public static void LoadSettings() 
    {
        if(File.Exists(FILE_PATH))
            settings = JsonConvert.DeserializeObject<DevSettings>(File.ReadAllText(FILE_PATH));
        else
            settings = new() {
                major = 0,
                minor = 0,
                revision = 0,
                masterEnable = false,
                overridePlayerLimit = false,
                playerLimit = -1,
                loadMode = LoadMode.NONE,
                map = KartLevel.TEST_TRACK,
                haveStandalonePlayerRunAsServer = false,
                manualLobbyPlayerWaitSwitch = false,
                overrideRaceProgressAtStart = false,
                raceProgress = 0,
                overrideLapCount = false,
                lapCount = -1,
                overrideBots = false,
                bots = false
            };
    }

    public static List<string> SettingsPrintout { get {
        List<string> printout = new();

        string headerMessage = $"Dev settings are " + (Settings.Enable ? "enabled." : "disabled.");
        if(!DevSettings.IsDevelopment())
            headerMessage += " Not in a development version.";
        if(!Settings.Enable)
            headerMessage += " Master enable is turned off";

        printout.Add(headerMessage);
        
        if(Settings.Enable) {
            if(Settings.ManualLobbyPlayerWaitSwitch)
                printout.Add("Manual lobby player switch active. Press F4 in game lobby on server instance to pick map.");
            if(Settings.OverridePlayerLimit)
                printout.Add($"Overriding player limit. New player limit: {Settings.PlayerLimit}");
            if(Settings.OverrideMapPick)
                printout.Add($"Overriding map pick. New map pick: {Settings.Map}");
            if(Settings.OverrideRaceProgressAtStart)
                printout.Add($"Overriding race progress at start. Initial race progress: {Settings.RaceProgress}");
            if(Settings.OverrideLapCount)
                printout.Add($"Overriding lap count. New lap count: {Settings.LapCount}");
            if(Settings.OverrideBots)
                printout.Add($"Overriding bots. Are bots enabled: {Settings.Bots}");
        }

        return printout;
    } }


}

public enum LoadMode 
{
    NONE, LOAD_LOBBY, LOAD_MAP_LOCAL
}

public enum ReleaseType {
    RELEASE, DEVELOPMENT
}