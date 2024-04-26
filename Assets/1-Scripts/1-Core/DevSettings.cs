using System.Collections;
using System.Collections.Generic;
using FishNet;
using UnityEngine;

public class DevSettings : MonoBehaviour
{

    [SerializeField] private bool masterEnable;

    [SerializeField] private bool overridePlayerLimit = false;
    public int playerLimit = -1;

    [SerializeField] public LoadMode loadMode = LoadMode.NONE;

    [SerializeField] private bool overrideMapPick = false;
    public KartLevel map = KartLevel.TEST_TRACK;

    [SerializeField] private bool haveStandalonePlayerRunAsServer = false;
    [SerializeField] private bool manualLobbyPlayerWaitSwitch = false;


    [SerializeField] private bool overrideRaceProgressAtStart = false;
    public float raceProgress = 0;

    [SerializeField] private bool overrideLapCount = false;
    public int lapCount = -1;
    [SerializeField] private bool overrideBots = false;
    public bool bots = false;

    /* Public accessors */
    public bool Enable { get { return masterEnable && GameVersion.IsDevelopment; } }
    public LoadMode LoadMode { get { return Enable ? loadMode : LoadMode.NONE; } }
    public bool HaveStandalonePlayerRunAsServer { get { return Enable && haveStandalonePlayerRunAsServer; } }
    public bool ManualLobbyPlayerWaitSwitch { get { return Enable && manualLobbyPlayerWaitSwitch; } }
    public bool OverridePlayerLimit { get { return Enable && overridePlayerLimit; } }
    public bool OverrideMapPick { get { return Enable && overrideMapPick; } }
    public bool OverrideRaceProgressAtStart { get { return Enable && overrideRaceProgressAtStart; } }
    public bool OverrideLapCount { get { return Enable && overrideLapCount; } }
    public bool OverrideBots { get { return Enable && overrideBots; } }

    private void Start() 
    {
        if(OverridePlayerLimit && playerLimit <= 0)
            Debug.LogWarning($"DevSettings: Max players is being overridden but the new value is <= 0, this is not recommended.");

        if(OverrideLapCount && lapCount <= 0)
            Debug.LogWarning($"DevSettings: Lap count is being overridden but the new value is <= 0, this is not recommended.");

        PrintDevSettings("#ffff99");

        if(HaveStandalonePlayerRunAsServer && !Application.isEditor) {
            NetworkStateManager nsm = InstanceFinder.NetworkManager.GetComponent<NetworkStateManager>();
            nsm.StartServer();
            return;
        }

        if(LoadMode != LoadMode.NONE)
            SimulateLoad();
    }

    /// <summary>
    /// When loadMode != LoadMode.NONE this will load the user into a map either in local play or multiplayer depending on load mode.
    /// For multiplayer load mode, there must be a server instance running to recieve the player.
    /// </summary>
    public void SimulateLoad() 
    {
        if(LoadMode == LoadMode.NONE) {
            Debug.LogError("Tried to simulate load but the LoadMode was set to NONE.");
            return;
        }

        CoreManager.Instance.isMultiplayer = LoadMode == LoadMode.LOAD_LOBBY;

        if(LoadMode == LoadMode.LOAD_LOBBY) {
            CoreManager.TransitionManager.LoadScene(SceneNames.MENU_LOBBY);
        } else if(LoadMode == LoadMode.LOAD_MAP_LOCAL) {
            KartLevel mapPick = OverrideMapPick ? map : GameLobby.PickKartLevel();
            if(!OverrideMapPick) 
                Debug.Log($"<color=green>SimulateLoad: loading into local play map but override map pick is off, picked {mapPick} randomly.</color>");
            CoreManager.Instance.LoadLocalMap(mapPick);
        }
    }

    public void PrintDevSettings(string color) 
    {
        string headerMessage = $"Dev settings are " + (Enable ? "enabled." : "disabled.");
        if(!GameVersion.IsDevelopment)
            headerMessage += " Not in a development version.";
        if(!masterEnable)
            headerMessage += " Master enable is turned off";

        Debug.Log($"<color={color}>{headerMessage}</color>");
        
        if(!Enable) return;

        List<string> devSettings = new();

        if(ManualLobbyPlayerWaitSwitch)
            devSettings.Add("Manual lobby player switch active. Press F4 in game lobby on server instance to pick map.");
        if(OverridePlayerLimit)
            devSettings.Add($"Overriding player limit. New player limit: {playerLimit}");
        if(OverrideMapPick)
            devSettings.Add($"Overriding map pick. New map pick: {map}");
        if(OverrideRaceProgressAtStart)
            devSettings.Add($"Overriding race progress at start. Initial race progress: {raceProgress}");
        if(OverrideLapCount)
            devSettings.Add($"Overriding lap count. New lap count: {lapCount}");
        if(OverrideBots)
            devSettings.Add($"Overriding bots. Are bots enabled: {bots}");

        devSettings.ForEach(s => Debug.Log($"<color={color}> - {s}</color>"));
    }

}

public enum LoadMode {
    NONE, LOAD_LOBBY, LOAD_MAP_LOCAL
}